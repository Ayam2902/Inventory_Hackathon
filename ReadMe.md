# 📦 Inventory Management System — POC for Indian SMEs

A full-stack inventory management system built for small and medium enterprises (SMEs) in India. It combines a real-time web dashboard, a REST API with GST-aware business logic, and a WhatsApp bot that lets shop owners manage stock directly from their phones — no app installation required.

---

## 🧩 What This Does

Most Indian SMEs track inventory in physical registers or basic spreadsheets. This system replaces that with:

- A **live web dashboard** to view, filter, and update stock
- A **.NET REST API** for all CRUD operations, with GST rate tracking per product
- A **WhatsApp chatbot** — shop owners can check stock, record sales, and get low-stock alerts by just sending a WhatsApp message in plain English or Hindi
- A **MySQL database** with a full audit trail of every stock movement

---

## 🗂️ Project Structure

```
hackathon/
├── SqlScripts/
│   ├── 01_schema.sql          ← Tables, views, indexes
│   └── 02_seed_data.sql       ← Sample products & suppliers for testing
│
├── Backend/                   ← ASP.NET Core 9 Web API
│   ├── Program.cs             ← App setup, DI registration
│   ├── appsettings.json       ← DB connection string + WhatsApp config
│   ├── InventoryApi.csproj
│   ├── Models.cs              ← Domain models (Product, InventoryItem, etc.)
│   ├── Repositories.cs        ← All DB queries via Dapper
│   ├── Controllers.cs         ← /api/products, /api/inventory endpoints
│   ├── WhatsAppController.cs  ← Webhook handler (Twilio & Meta supported)
│   └── WhatsAppIntentService.cs ← Rule-based NLP intent parser
│
└── Frontend/
    └── App.jsx                ← React dashboard (Vite)
```

---

## 🗄️ Database Schema

### Tables

| Table | Purpose |
|---|---|
| `products` | Product catalog — SKU, name, GST rate, reorder level, pricing |
| `inventory` | Current stock quantity per product per warehouse |
| `stock_transactions` | Immutable audit log of every IN / OUT / ADJUSTMENT / RETURN |
| `categories` | Product groupings |
| `suppliers` | Vendor details with GSTIN |
| `whatsapp_sessions` | Conversation state for multi-turn WhatsApp flows |

### Views

| View | Returns |
|---|---|
| `vw_inventory_status` | Joined product + stock + computed status (IN_STOCK / LOW_STOCK / OUT_OF_STOCK) |
| `vw_low_stock_alerts` | Only items at or below their reorder level |

---

## 🔌 API Endpoints

### Products — `/api/products`

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/products` | List all active products |
| GET | `/api/products/{id}` | Get product by ID |
| GET | `/api/products/sku/{sku}` | Get product by SKU |
| POST | `/api/products` | Create product with initial stock |
| PUT | `/api/products/{id}` | Update product details |
| DELETE | `/api/products/{id}` | Soft delete product |

### Inventory — `/api/inventory`

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/inventory` | Full inventory status (uses `vw_inventory_status`) |
| GET | `/api/inventory/{productId}` | Stock for a single product |
| GET | `/api/inventory/alerts/low-stock` | All low / out-of-stock items |
| PATCH | `/api/inventory/{productId}/stock` | Add / remove / adjust stock |
| GET | `/api/inventory/{productId}/history` | Last 50 transactions for a product |

### WhatsApp — `/api/whatsapp`

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/whatsapp/webhook` | Meta webhook verification handshake |
| POST | `/api/whatsapp/webhook` | Receive messages (Twilio or Meta format) |

---

## 📱 WhatsApp Integration

### How It Works

```
Your WhatsApp
     │
     ▼
Twilio / Meta Cloud API (free tier)
     │  POST /api/whatsapp/webhook
     ▼
WhatsAppController  ──►  WhatsAppIntentService (rule-based NLP)
     │                              │
     │                    Detected intent:
     │                    CHECK_STOCK / ADD_STOCK
     │                    REMOVE_STOCK / LOW_STOCK_ALERT
     ▼
Inventory Repository (MySQL)
     │
     ▼
Reply sent back via Twilio / Meta API
     │
     ▼
Your WhatsApp ✅
```

### Supported Commands

| Message | Intent | Action |
|---|---|---|
| `check USB cable` | CHECK_STOCK | Returns current stock + status |
| `how many pens` | CHECK_STOCK | Same — keyword variations work |
| `add 50 mouse` | ADD_STOCK | Inserts IN transaction |
| `received 100 A4 paper` | ADD_STOCK | Same |
| `sold 10 cables` | REMOVE_STOCK | Inserts OUT transaction |
| `dispatch 5 boxes` | REMOVE_STOCK | Same |
| `low stock` | LOW_STOCK_ALERT | Lists all items at/below reorder level |
| `alerts` | LOW_STOCK_ALERT | Same |
| `help` / `hi` | HELP | Returns command menu |

Hindi-ish phrases also work: `kitna`, `aaya`, `becha`, `khatam`

### Providers Supported

The controller supports both **Twilio** and **Meta Cloud API** — switch via `appsettings.json`:

```json
"WhatsApp": {
  "Provider": "Twilio",
  "AccountSid": "ACxxxxxxxx",
  "AuthToken": "xxxxxxxx",
  "SandboxNumber": "whatsapp:+14155238886"
}
```

---

## 🚀 How to Run

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [MySQL 8+](https://dev.mysql.com/downloads/)
- [Node.js 18+](https://nodejs.org/) (for the React frontend)
- [Twilio account](https://www.twilio.com/try-twilio) (free) **or** Meta Developer account
- `localtunnel` or `ngrok` to expose localhost to the internet

---

### Step 1 — Set Up the Database

```bash
mysql -u root -p < SqlScripts/01_schema.sql
mysql -u root -p < SqlScripts/02_seed_data.sql
```

---

### Step 2 — Configure the Backend

Edit `Backend/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "InventoryDb": "Server=localhost;Database=inventory_db;Uid=root;Pwd=YOUR_MYSQL_PASSWORD;"
  },
  "WhatsApp": {
    "Provider": "Twilio",
    "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "AuthToken":  "your_auth_token",
    "SandboxNumber": "whatsapp:+14155238886"
  }
}
```

---

### Step 3 — Run the Backend API

```bash
cd Backend
dotnet restore
dotnet run
# API available at: http://localhost:5000
# Swagger UI at:    http://localhost:5000/swagger
```

---

### Step 4 — Run the Frontend

```bash
cd Frontend
npm create vite@latest . -- --template react
# Replace src/App.jsx with the provided App.jsx
npm install
npm run dev
# Dashboard at: http://localhost:5173
```

---

### Step 5 — Expose API for WhatsApp Webhook

**Option A — localtunnel (no signup needed):**
```bash
npx localtunnel --port 5000 --subdomain inventory-poc
# Public URL: https://inventory-poc.loca.lt
```

**Option B — ngrok (free account required):**
```bash
ngrok config add-authtoken YOUR_TOKEN
ngrok http 5000
```

---

### Step 6 — Connect WhatsApp (Twilio Sandbox)

1. Go to **[Twilio Console](https://console.twilio.com) → Messaging → Try it out → Send a WhatsApp message**
2. Send the join phrase shown (e.g. `join plenty-tiger`) to `+14155238886` from your WhatsApp
3. Under **Sandbox Settings**, set:
   - **When a message comes in**: `https://inventory-poc.loca.lt/api/whatsapp/webhook`
   - Method: **HTTP POST** → Save
4. Send `help` to the Twilio sandbox number — you'll get a live reply ✅

---

## 🏗️ Tech Stack

| Layer | Technology | Cost |
|---|---|---|
| Database | MySQL 8 | Free |
| API | ASP.NET Core 9 + Dapper | Free |
| Frontend | React + Vite | Free |
| WhatsApp | Twilio Sandbox / Meta Cloud API | Free tier |
| Local Tunnel | localtunnel / ngrok | Free |
| Intent NLP | Rule-based (built-in) | Free |

---

## 🔭 Future Scope

### 1. AI-Powered Inventory Intelligence

Integrate Claude / OpenAI to analyze the `stock_transactions` audit log and surface actionable insights:

- **Seasonal demand forecasting** — detect patterns like stationery spikes before school season, or festive demand for packaging materials — and auto-set reorder levels per season
- **Store-level prioritization** — for multi-warehouse setups, recommend which products each warehouse should stock more of based on its local sales velocity
- **Supplier optimization** — flag slow-moving SKUs and recommend switching suppliers or discontinuing based on margin + turnover data
- **Anomaly detection** — alert when a product's sales velocity suddenly spikes or drops, flagging possible theft or data entry errors

```
stock_transactions (audit log)
        │
        ▼
  Claude / GPT API
  ├── Seasonal trend analysis
  ├── Store-level demand patterns
  ├── Reorder quantity suggestions
  └── Supplier performance scoring
        │
        ▼
  Recommendations pushed via WhatsApp
```

### 2. Automated WhatsApp Reports

Schedule and deliver business reports directly to the owner's WhatsApp — no app, no login:

- **Daily summary** — total sales, top 5 moving products, value of stock consumed
- **Weekly low-stock digest** — items needing reorder grouped by supplier, with suggested order quantities
- **Monthly P&L snapshot** — revenue from stock sold (selling price × qty) vs. cost of goods (purchase price × qty), GST summary
- **On-demand reports** — owner sends `report today` or `report last week` on WhatsApp and gets an instant reply

### 3. Voice & Image Input via WhatsApp

- Accept **product images** on WhatsApp — use OCR / vision AI to extract SKU barcodes and auto-update stock
- Accept **voice notes** — transcribe with Whisper and run through the intent engine, so owners can update stock hands-free while on the shop floor

### 4. Purchase Order Automation

When stock hits the reorder level, automatically:
- Generate a draft purchase order
- Send it to the supplier via WhatsApp or email
- Track PO status in a new `purchase_orders` table
- Reconcile against received stock when the goods arrive

### 5. Multi-Store Expansion

The `inventory.warehouse` column already supports multiple locations. Future work:
- Inter-warehouse stock transfers
- Per-store dashboards with role-based access
- Central vs. store-level inventory views
- Aggregate reporting across all locations

---

## 📌 Known Limitations (POC)

- WhatsApp Twilio sandbox is limited to opted-in numbers; production requires business verification
- Intent parsing is rule-based — works well for short commands, not for conversational queries
- No authentication on the API — add JWT middleware before any public deployment
- localtunnel URL changes on every restart — use a paid tunnel or deploy to a server for stable webhook URLs
