using System.Text.RegularExpressions;

namespace InventoryApi.Services;

/// <summary>
/// FREE Intent Parser — no external AI API needed.
/// Uses rule-based pattern matching to classify WhatsApp messages.
/// 
/// Supported intents:
///   CHECK_STOCK   → "check usb cable", "stock of pen", "how many A4 paper"
///   ADD_STOCK     → "add 50 usb cable", "received 100 pens", "stock in 200 boxes"
///   REMOVE_STOCK  → "sold 10 usb", "remove 5 mouse", "dispatch 20 pens"
///   LOW_STOCK_ALERT → "low stock", "alerts", "what needs reorder"
///   HELP          → "help", "commands", "?"
/// </summary>
public interface IWhatsAppIntentService
{
    ParsedIntent ParseIntent(string message);
}

public record ParsedIntent(
    string Action,
    string? ProductQuery,
    int Quantity,
    string? Phone = null
);

public class WhatsAppIntentService : IWhatsAppIntentService
{
    // ── Keyword groups ────────────────────────────────────────────────────
    private static readonly string[] CheckKeywords =
        ["check", "stock of", "how many", "quantity of", "count", "status of",
         "kitna", "stock check", "available"];

    private static readonly string[] AddKeywords =
        ["add", "received", "stock in", "purchase", "bought", "incoming",
         "mila", "aaya", "in stock", "restock", "added"];

    private static readonly string[] RemoveKeywords =
        ["sold", "remove", "dispatch", "out", "issued", "used",
         "becha", "gaya", "sale", "delivery", "shipped"];

    private static readonly string[] LowStockKeywords =
        ["low stock", "alerts", "reorder", "running low", "shortage",
         "kya khatam", "low", "empty", "out of stock"];

    private static readonly string[] HelpKeywords =
        ["help", "commands", "menu", "?", "hi", "hello", "namaste", "start"];

    // ── Quantity extraction pattern ────────────────────────────────────────
    // Matches: "50", "50 pcs", "50 units" etc.
    private static readonly Regex QuantityRegex =
        new(@"\b(\d+)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // ── Product extraction: remove known verbs/quantities ─────────────────
    private static readonly Regex CleanupRegex =
        new(@"\b(add|check|sold|remove|dispatch|received|stock|of|in|out|the|a|an|\d+|pcs|units?|kg|box|litre)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ParsedIntent ParseIntent(string message)
    {
        var msg = message.Trim().ToLower();

        // 1. Detect action
        var action = DetectAction(msg);

        // 2. Extract quantity
        var quantityMatch = QuantityRegex.Match(msg);
        var quantity = quantityMatch.Success ? int.Parse(quantityMatch.Value) : 0;

        // 3. Extract product name (clean up stopwords from the message)
        var productQuery = ExtractProduct(msg);

        return new ParsedIntent(action, productQuery, quantity);
    }

    private static string DetectAction(string msg)
    {
        if (LowStockKeywords.Any(k => msg.Contains(k)))   return "LOW_STOCK_ALERT";
        if (HelpKeywords.Any(k => msg.Equals(k) || msg.StartsWith(k))) return "HELP";
        if (AddKeywords.Any(k => msg.Contains(k)))        return "ADD_STOCK";
        if (RemoveKeywords.Any(k => msg.Contains(k)))     return "REMOVE_STOCK";
        if (CheckKeywords.Any(k => msg.Contains(k)))      return "CHECK_STOCK";

        // Fallback: if message contains a number, assume CHECK
        if (QuantityRegex.IsMatch(msg)) return "ADD_STOCK";

        return "UNKNOWN";
    }

    private static string? ExtractProduct(string msg)
    {
        // Remove common command words and quantities, leaving the product name
        var cleaned = CleanupRegex.Replace(msg, " ").Trim();
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }
}
