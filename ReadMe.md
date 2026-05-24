# 📦 StockSetu — Inventory Management POC for Indian SMEs

A full-stack inventory management system with:
- **MySQL** database (GST-aware, Indian business logic)
- **.NET 8 Web API** with CRUD endpoints (using Dapper)
- **React** frontend dashboard
- **WhatsApp integration** via free Meta Cloud API

---

## 🗂️ Project Structure

```
inventory-poc/
├── sql/
│   ├── 01_schema.sql       ← Table definitions, views
│   └── 02_seed_data.sql    ← Sample products for testing
│
├── dotnet-api/
│   ├── Program.cs                          ← App setup, DI
│   ├── appsettings.json                    ← DB + WhatsApp config
│   ├── Models/Models.cs                    ← Domain models + DTOs
│   ├── Repositories/Repositories.cs       ← CRUD via Dapper
│   ├── Controllers/
│   │   ├── Controllers.cs                  ← /api/products, /api/inventory
│   │   └── WhatsAppController.cs          ← /api/whatsapp/webhook
│   └── Services/
│       ├── DbConnectionFactory.cs
│       └── WhatsAppIntentService.cs       ← NLP intent parser
│
└── react-app/
    └── App.jsx                             ← Full React dashboard
```

---

## 🗄️ MySQL Schema

### Key Tables

| Table | Purpose |
|---|---|
| `products` | Master product catalog with GST rates, SKUs |
| `inventory` | Real-time stock levels per warehouse |
| `stock_transactions` | Audit trail of every stock movement (IN/OUT/ADJUSTMENT) |
| `categories` | Product groupings |
| `suppliers` | Vendor info with GSTIN |
| `whatsapp_sessions` | Tracks multi-turn WhatsApp conversation state |

### Key Views

| View | Returns |
|---|---|
| `vw_inventory_status` | Joined product + stock + status (IN_STOCK / LOW_STOCK / OUT_OF_STOCK) |
| `vw_low_stock_alerts` | Only items at or below reorder level |

### Run the scripts

```bash
mysql -u root -p < sql/01_schema.sql
mysql -u root -p < sql/02_seed_data.sql
```

---

## 🔌 .NET API Endpoints

### Products

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/products` | List all active products |
| GET | `/api/products/{id}` | Get by ID |
| GET | `/api/products/sku/{sku}` | Get by SKU |
| POST | `/api/products` | Create product + initial stock |
| PUT | `/api/products/{id}` | Update product |
| DELETE | `/api/products/{id}` | Soft delete |

### Inventory

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/inventory` | Full inventory status view |
| GET | `/api/inventory/{productId}` | Single product stock |
| GET | `/api/inventory/alerts/low-stock` | Low/out-of-stock items |
| PATCH | `/api/inventory/{productId}/stock` | Add/remove/adjust stock |
| GET | `/api/inventory/{productId}/history` | Last 50 transactions |

### WhatsApp

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/whatsapp/webhook` | Meta verification handshake |
| POST | `/api/whatsapp/webhook` | Receive & process messages |

### Run the API

```bash
cd dotnet-api
dotnet restore
dotnet run
# Swagger UI: http://localhost:5000/swagger
```

---

## 📱 WhatsApp Integration (FREE)

### Architecture

```
User's WhatsApp
      │
      ▼
Meta Cloud API (free)
      │  POST webhook
      ▼
.NET WhatsAppController
      │
      ▼
WhatsAppIntentService ── rule-based NLP ──► Detected Intent
      │                                         │
      ▼                                         ▼
Inventory Repository ◄──────────── CHECK_STOCK / ADD_STOCK
      │                             REMOVE_STOCK / LOW_STOCK_ALERT
      ▼
WhatsApp Reply via Meta Graph API
      │
      ▼
User's WhatsApp ✅
```

### Intent Detection (Free, No AI API Needed)

The `WhatsAppIntentService` uses keyword pattern matching:

| Message Examples | Detected Intent | API Called |
|---|---|---|
| "check USB cable" | `CHECK_STOCK` | GET /api/inventory |
| "how many A4 paper" | `CHECK_STOCK` | GET /api/inventory |
| "add 50 mouse" | `ADD_STOCK` | PATCH /api/inventory/{id}/stock |
| "received 100 pens" | `ADD_STOCK` | PATCH /api/inventory/{id}/stock |
| "sold 10 cables" | `REMOVE_STOCK` | PATCH /api/inventory/{id}/stock |
| "low stock" | `LOW_STOCK_ALERT` | GET /api/inventory/alerts/low-stock |
| "help" / "hi" | `HELP` | — |

Also works with Hindi-ish phrases: `kitna`, `aaya`, `becha`, `khatam`

### WhatsApp Setup (Step by Step)

1. **Create Meta Developer account** → [developers.facebook.com](https://developers.facebook.com)
2. **New App** → Business type → Add WhatsApp product
3. **Get credentials** from the WhatsApp > Getting Started page:
   - `Access Token` (temporary, refresh every 24h for dev)
   - `Phone Number ID`
4. **Set Webhook**:
   - For local dev: `ngrok http 5000` → use HTTPS URL
   - Callback URL: `https://xxxx.ngrok.io/api/whatsapp/webhook`
   - Verify Token: any string you choose (same in `appsettings.json`)
5. **Subscribe** to the `messages` webhook field
6. **Add test number**: WhatsApp > Getting Started → add your own number

### Update `appsettings.json`

```json
{
  "WhatsApp": {
    "VerifyToken": "my-secret-token-123",
    "AccessToken": "EAAxxxxxxxx...",
    "PhoneNumberId": "12345678901234"
  }
}
```

---

## ⚛️ React Frontend

### Features
- Dashboard with live stats (total, in-stock, low-stock, out-of-stock counts)
- Searchable inventory table with status filters
- In-table stock update modal (IN / OUT / ADJUSTMENT)
- Add Product form with GST rate, reorder level, initial stock
- WhatsApp setup guide tab

### Run locally (using Vite)

```bash
cd react-app
npm create vite@latest . -- --template react
# Replace src/App.jsx with the provided App.jsx
npm install
npm run dev
```

---

## 💡 Scaling This POC

| Feature | Approach |
|---|---|
| Multi-user auth | Add ASP.NET Identity or JWT middleware |
| Better NLP | Upgrade `WhatsAppIntentService` to call Claude/OpenAI API |
| Purchase orders | Add `purchase_orders` + `po_items` tables |
| Barcode scanning | Add barcode column to products, use a React barcode scanner |
| WhatsApp images | Accept product images via WhatsApp media message handler |
| Reporting | Add a `/api/reports/summary` endpoint + chart in React |
| Multi-warehouse | Already supported — `inventory.warehouse` column exists |

---

## 🏗️ Tech Stack Summary

| Layer | Technology | Cost |
|---|---|---|
| Database | MySQL 8 | Free |
| API | .NET 8 Web API + Dapper | Free |
| Frontend | React + Vite | Free |
| WhatsApp | Meta Cloud API | Free (1k conv/month) |
| Local tunnel | ngrok | Free tier |
| Intent NLP | Rule-based (built-in) | Free |
