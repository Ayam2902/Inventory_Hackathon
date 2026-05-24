using Microsoft.AspNetCore.Mvc;
using InventoryApi.Models;
using InventoryApi.Repositories;
using InventoryApi.Services;
using System.Text.Json;

namespace InventoryApi.Controllers;

/// <summary>
/// WhatsApp Webhook via Meta Cloud API (free tier)
/// Flow: WhatsApp → Meta Webhook → This endpoint → Parse Intent → Hit inventory API → Reply
/// 
/// FREE SETUP:
///   1. Create Meta Developer account → facebook.com/developers
///   2. Create an App → Add WhatsApp product
///   3. Use the free test number (5 contacts free, no business account needed for testing)
///   4. Register this endpoint as the webhook URL
///   5. Set WEBHOOK_VERIFY_TOKEN in appsettings.json
/// </summary>
[ApiController]
[Route("api/whatsapp")]
public class WhatsAppController : ControllerBase
{
    private readonly IWhatsAppIntentService _intentService;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IProductRepository _productRepo;
    private readonly IConfiguration _config;

    public WhatsAppController(
        IWhatsAppIntentService intentService,
        IInventoryRepository inventoryRepo,
        IProductRepository productRepo,
        IConfiguration config)
    {
        _intentService = intentService;
        _inventoryRepo = inventoryRepo;
        _productRepo = productRepo;
        _config = config;
    }

    // ── Meta webhook verification (GET) ──────────────────────────────────
    [HttpGet("webhook")]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.verify_token")] string token,
        [FromQuery(Name = "hub.challenge")] string challenge)
    {
        var myToken = _config["WhatsApp:VerifyToken"];
        if (mode == "subscribe" && token == myToken)
            return Ok(challenge);
        return Unauthorized();
    }

    // ── Incoming message handler (POST) ──────────────────────────────────
    // Twilio sends form-encoded data; Meta sends JSON — both handled here
    [HttpPost("webhook")]
    [Consumes("application/x-www-form-urlencoded", "application/json")]
    public async Task<IActionResult> HandleMessage()
    {
        try
        {
            string from, text;
            var provider = _config["WhatsApp:Provider"] ?? "Meta";

            if (provider == "Twilio")
            {
                // Twilio sends form-encoded body: From=whatsapp:+91xxx&Body=hello
                from = Request.Form["From"].ToString().Replace("whatsapp:", "");
                text = Request.Form["Body"].ToString();
            }
            else
            {
                // Meta sends JSON body
                using var reader = new System.IO.StreamReader(Request.Body);
                var rawBody = await reader.ReadToEndAsync();
                var body = System.Text.Json.JsonDocument.Parse(rawBody).RootElement;

                var entry = body.GetProperty("entry")[0];
                var change = entry.GetProperty("changes")[0];
                var value = change.GetProperty("value");

                if (!value.TryGetProperty("messages", out var messages))
                    return Ok();

                var message = messages[0];
                from = message.GetProperty("from").GetString()!;
                text = message.GetProperty("text").GetProperty("body").GetString()!;
            }

            if (string.IsNullOrWhiteSpace(text)) return Ok();

            var replyText = await ProcessMessageAsync(from, text);
            await SendWhatsAppReplyAsync(from, replyText);

            // Twilio expects empty 200 TwiML response
            return Content("<Response></Response>", "text/xml");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WhatsApp webhook error: {ex.Message}");
            return Content("<Response></Response>", "text/xml");
        }
    }

    // ── Core message processor ────────────────────────────────────────────
    private async Task<string> ProcessMessageAsync(string phone, string text)
    {
        // Stamp the caller's phone onto the intent so handlers can log it
        var intent = _intentService.ParseIntent(text) with { Phone = phone };

        return intent.Action switch
        {
            "CHECK_STOCK"     => await HandleCheckStock(intent),
            "ADD_STOCK"       => await HandleAddStock(intent),
            "REMOVE_STOCK"    => await HandleRemoveStock(intent),
            "LOW_STOCK_ALERT" => await HandleLowStockAlert(),
            "HELP"            => GetHelpMessage(),
            _                 => "❓ Sorry, I didn't understand that.\n\nType *HELP* to see what I can do!"
        };
    }

    private async Task<string> HandleCheckStock(ParsedIntent intent)
    {
        if (string.IsNullOrEmpty(intent.ProductQuery))
            return "Please tell me which product to check. E.g., *CHECK USB cable*";

        var products = await _inventoryRepo.GetAllAsync();
        var matches = products.Where(p =>
            p.ProductName.Contains(intent.ProductQuery, StringComparison.OrdinalIgnoreCase) ||
            p.Sku.Contains(intent.ProductQuery, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!matches.Any())
            return $"❌ No product found matching *{intent.ProductQuery}*";

        var lines = matches.Select(p =>
        {
            var emoji = p.StockStatus == "IN_STOCK" ? "✅" :
                        p.StockStatus == "LOW_STOCK" ? "⚠️" : "❌";
            return $"{emoji} *{p.ProductName}* (SKU: {p.Sku})\n   Stock: {p.CurrentStock} {p.Unit} | Status: {p.StockStatus}";
        });

        return $"📦 *Stock Status:*\n\n{string.Join("\n\n", lines)}";
    }

    private async Task<string> HandleAddStock(ParsedIntent intent)
    {
        if (string.IsNullOrEmpty(intent.ProductQuery) || intent.Quantity <= 0)
            return "Please specify product and quantity. E.g., *ADD 50 USB cable*";

        var products = await _inventoryRepo.GetAllAsync();
        var match = products.FirstOrDefault(p =>
            p.ProductName.Contains(intent.ProductQuery, StringComparison.OrdinalIgnoreCase) ||
            p.Sku.Equals(intent.ProductQuery, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            return $"❌ Product *{intent.ProductQuery}* not found.";

        await _inventoryRepo.UpdateStockAsync(match.ProductId, new UpdateStockDto(
            TransactionType: "IN",
            Quantity: intent.Quantity,
            ReferenceNo: null,
            Notes: "Updated via WhatsApp",
            Source: "WHATSAPP",
            CreatedBy: $"WA:{intent.Phone}"
        ));

        return $"✅ Added *{intent.Quantity} {match.Unit}* of *{match.ProductName}*\n" +
               $"New stock: {match.CurrentStock + intent.Quantity} {match.Unit}";
    }

    private async Task<string> HandleRemoveStock(ParsedIntent intent)
    {
        if (string.IsNullOrEmpty(intent.ProductQuery) || intent.Quantity <= 0)
            return "Please specify product and quantity. E.g., *SOLD 10 USB cable*";

        var products = await _inventoryRepo.GetAllAsync();
        var match = products.FirstOrDefault(p =>
            p.ProductName.Contains(intent.ProductQuery, StringComparison.OrdinalIgnoreCase) ||
            p.Sku.Equals(intent.ProductQuery, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            return $"❌ Product *{intent.ProductQuery}* not found.";

        if (match.CurrentStock < intent.Quantity)
            return $"⚠️ Not enough stock! Available: *{match.CurrentStock} {match.Unit}*";

        await _inventoryRepo.UpdateStockAsync(match.ProductId, new UpdateStockDto(
            TransactionType: "OUT",
            Quantity: intent.Quantity,
            ReferenceNo: null,
            Notes: "Updated via WhatsApp",
            Source: "WHATSAPP",
            CreatedBy: $"WA:{intent.Phone}"
        ));

        return $"✅ Removed *{intent.Quantity} {match.Unit}* of *{match.ProductName}*\n" +
               $"Remaining: {match.CurrentStock - intent.Quantity} {match.Unit}";
    }

    private async Task<string> HandleLowStockAlert()
    {
        var alerts = (await _inventoryRepo.GetLowStockAsync()).ToList();
        if (!alerts.Any())
            return "✅ All products are sufficiently stocked!";

        var lines = alerts.Select(p =>
        {
            var emoji = p.StockStatus == "OUT_OF_STOCK" ? "❌" : "⚠️";
            return $"{emoji} *{p.ProductName}*: {p.CurrentStock} {p.Unit} remaining (reorder at {p.ReorderLevel})";
        });

        return $"🚨 *Low Stock Alert ({alerts.Count} items):*\n\n{string.Join("\n", lines)}";
    }

    private static string GetHelpMessage() => @"📦 *Inventory Bot Commands:*

🔍 *Check stock:*
  CHECK [product name or SKU]
  e.g., CHECK USB cable

➕ *Add stock:*
  ADD [qty] [product]
  e.g., ADD 50 USB cable

➖ *Record sale/removal:*
  SOLD [qty] [product]
  REMOVE [qty] [product]

⚠️ *Low stock alerts:*
  LOW STOCK
  ALERTS

❓ *Help:*
  HELP";

    // ── Send reply — supports both Twilio and Meta ────────────────────────
    private async Task SendWhatsAppReplyAsync(string to, string message)
    {
        var provider = _config["WhatsApp:Provider"] ?? "Meta";

        if (provider == "Twilio")
        {
            // Twilio REST API — Basic Auth with AccountSid:AuthToken
            var accountSid  = _config["WhatsApp:AccountSid"]!;
            var authToken   = _config["WhatsApp:AuthToken"]!;
            var fromNumber  = _config["WhatsApp:SandboxNumber"]!;  // whatsapp:+14155238886
            var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";

            using var http = new HttpClient();
            var credentials = Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
            http.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");

            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("From", fromNumber),
                new KeyValuePair<string, string>("To",   $"whatsapp:{to}"),
                new KeyValuePair<string, string>("Body", message)
            });

            await http.PostAsync(url, formData);
        }
        else
        {
            // Meta Cloud API — Bearer token
            var token         = _config["WhatsApp:AccessToken"]!;
            var phoneNumberId = _config["WhatsApp:PhoneNumberId"]!;
            var url = $"https://graph.facebook.com/v19.0/{phoneNumberId}/messages";

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var payload = new
            {
                messaging_product = "whatsapp",
                to,
                type = "text",
                text = new { body = message }
            };

            await http.PostAsJsonAsync(url, payload);
        }
    }
}
