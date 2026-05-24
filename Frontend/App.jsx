import { useState, useEffect } from "react";

const API = "http://localhost:5000/api";

const STATUS_CONFIG = {
  IN_STOCK:     { color: "#22c55e", bg: "#dcfce7", label: "In Stock" },
  LOW_STOCK:    { color: "#f59e0b", bg: "#fef3c7", label: "Low Stock" },
  OUT_OF_STOCK: { color: "#ef4444", bg: "#fee2e2", label: "Out of Stock" },
};

function Badge({ status }) {
  const cfg = STATUS_CONFIG[status] || STATUS_CONFIG.IN_STOCK;
  return (
    <span style={{
      background: cfg.bg, color: cfg.color,
      padding: "2px 10px", borderRadius: 99,
      fontSize: 11, fontWeight: 700, letterSpacing: 0.5
    }}>{cfg.label}</span>
  );
}

function StatCard({ label, value, sub, accent }) {
  return (
    <div style={{
      background: "#fff", borderRadius: 14,
      padding: "20px 24px", boxShadow: "0 1px 4px #0001",
      borderLeft: `4px solid ${accent}`, minWidth: 160
    }}>
      <div style={{ fontSize: 28, fontWeight: 800, color: accent }}>{value}</div>
      <div style={{ fontWeight: 600, color: "#1e293b", marginTop: 2 }}>{label}</div>
      {sub && <div style={{ fontSize: 12, color: "#94a3b8", marginTop: 2 }}>{sub}</div>}
    </div>
  );
}

function Modal({ title, onClose, children }) {
  return (
    <div style={{
      position: "fixed", inset: 0, background: "#0007",
      display: "flex", alignItems: "center", justifyContent: "center", zIndex: 100
    }} onClick={onClose}>
      <div style={{
        background: "#fff", borderRadius: 16, padding: 32,
        minWidth: 420, maxWidth: 520, width: "90%", boxShadow: "0 20px 60px #0003"
      }} onClick={e => e.stopPropagation()}>
        <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 20 }}>
          <h2 style={{ margin: 0, fontSize: 20, fontWeight: 800 }}>{title}</h2>
          <button onClick={onClose} style={{
            background: "none", border: "none", fontSize: 22,
            cursor: "pointer", color: "#64748b"
          }}>×</button>
        </div>
        {children}
      </div>
    </div>
  );
}

function Input({ label, ...props }) {
  return (
    <div style={{ marginBottom: 14 }}>
      <label style={{ display: "block", fontSize: 13, fontWeight: 600, color: "#475569", marginBottom: 5 }}>
        {label}
      </label>
      <input style={{
        width: "100%", padding: "9px 12px", border: "1.5px solid #e2e8f0",
        borderRadius: 8, fontSize: 14, outline: "none", boxSizing: "border-box",
        transition: "border-color 0.15s"
      }} {...props}
        onFocus={e => e.target.style.borderColor = "#6366f1"}
        onBlur={e => e.target.style.borderColor = "#e2e8f0"}
      />
    </div>
  );
}

// ── Inventory Table Tab ────────────────────────────────────────────────────
function InventoryTab({ inventory, onStockUpdate, categories }) {
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState("ALL");
  const [stockModal, setStockModal] = useState(null);
  const [stockForm, setStockForm] = useState({ type: "IN", qty: "", ref: "", notes: "" });

  const filtered = inventory.filter(p => {
    const matchSearch = p.productName.toLowerCase().includes(search.toLowerCase()) ||
                        p.sku.toLowerCase().includes(search.toLowerCase());
    const matchFilter = filter === "ALL" || p.stockStatus === filter;
    return matchSearch && matchFilter;
  });

  const handleStockSubmit = async () => {
    await onStockUpdate(stockModal.productId, {
      transactionType: stockForm.type,
      quantity: parseInt(stockForm.qty),
      referenceNo: stockForm.ref,
      notes: stockForm.notes,
      source: "WEB", createdBy: "admin"
    });
    setStockModal(null);
    setStockForm({ type: "IN", qty: "", ref: "", notes: "" });
  };

  return (
    <div>
      {/* Filters */}
      <div style={{ display: "flex", gap: 12, marginBottom: 20, flexWrap: "wrap" }}>
        <input
          placeholder="🔍  Search products or SKU..."
          value={search} onChange={e => setSearch(e.target.value)}
          style={{
            flex: 1, minWidth: 200, padding: "9px 14px",
            border: "1.5px solid #e2e8f0", borderRadius: 8, fontSize: 14, outline: "none"
          }}
        />
        {["ALL", "IN_STOCK", "LOW_STOCK", "OUT_OF_STOCK"].map(f => (
          <button key={f} onClick={() => setFilter(f)} style={{
            padding: "8px 16px", borderRadius: 8, border: "none",
            background: filter === f ? "#6366f1" : "#f1f5f9",
            color: filter === f ? "#fff" : "#475569",
            fontWeight: 600, cursor: "pointer", fontSize: 13
          }}>{f.replace(/_/g, " ")}</button>
        ))}
      </div>

      {/* Table */}
      <div style={{ overflowX: "auto", borderRadius: 12, border: "1.5px solid #e2e8f0" }}>
        <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 14 }}>
          <thead>
            <tr style={{ background: "#f8fafc", borderBottom: "1.5px solid #e2e8f0" }}>
              {["SKU", "Product", "Category", "Stock", "Unit", "Price (₹)", "Status", "Actions"].map(h => (
                <th key={h} style={{ padding: "12px 16px", textAlign: "left",
                  fontWeight: 700, color: "#475569", fontSize: 12, letterSpacing: 0.5 }}>
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {filtered.map((item, i) => (
              <tr key={item.productId} style={{
                borderBottom: "1px solid #f1f5f9",
                background: i % 2 === 0 ? "#fff" : "#fafafa"
              }}>
                <td style={{ padding: "12px 16px", fontFamily: "monospace",
                  color: "#6366f1", fontWeight: 700, fontSize: 12 }}>
                  {item.sku}
                </td>
                <td style={{ padding: "12px 16px", fontWeight: 600, color: "#1e293b" }}>
                  {item.productName}
                </td>
                <td style={{ padding: "12px 16px", color: "#64748b" }}>{item.categoryName}</td>
                <td style={{ padding: "12px 16px" }}>
                  <span style={{
                    fontWeight: 800, fontSize: 16,
                    color: item.currentStock === 0 ? "#ef4444" :
                           item.currentStock <= item.reorderLevel ? "#f59e0b" : "#1e293b"
                  }}>
                    {item.currentStock}
                  </span>
                  <span style={{ color: "#94a3b8", fontSize: 12, marginLeft: 4 }}>
                    / min {item.reorderLevel}
                  </span>
                </td>
                <td style={{ padding: "12px 16px", color: "#64748b" }}>{item.unit}</td>
                <td style={{ padding: "12px 16px", fontWeight: 600 }}>
                  ₹{item.sellingPrice?.toFixed(2)}
                  <span style={{ color: "#94a3b8", fontSize: 11, marginLeft: 4 }}>
                    +{item.gstPercent}% GST
                  </span>
                </td>
                <td style={{ padding: "12px 16px" }}>
                  <Badge status={item.stockStatus} />
                </td>
                <td style={{ padding: "12px 16px" }}>
                  <button
                    onClick={() => setStockModal(item)}
                    style={{
                      padding: "5px 12px", background: "#6366f1", color: "#fff",
                      border: "none", borderRadius: 6, cursor: "pointer",
                      fontSize: 12, fontWeight: 600
                    }}>
                    Update Stock
                  </button>
                </td>
              </tr>
            ))}
            {filtered.length === 0 && (
              <tr>
                <td colSpan={8} style={{ textAlign: "center", padding: 40, color: "#94a3b8" }}>
                  No products found
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {/* Stock Update Modal */}
      {stockModal && (
        <Modal title={`Update Stock — ${stockModal.productName}`} onClose={() => setStockModal(null)}>
          <div style={{ marginBottom: 14 }}>
            <label style={{ fontSize: 13, fontWeight: 600, color: "#475569", display: "block", marginBottom: 5 }}>
              Transaction Type
            </label>
            <div style={{ display: "flex", gap: 8 }}>
              {["IN", "OUT", "ADJUSTMENT"].map(t => (
                <button key={t} onClick={() => setStockForm(f => ({ ...f, type: t }))}
                  style={{
                    flex: 1, padding: "8px", border: "none", borderRadius: 8,
                    background: stockForm.type === t ? "#6366f1" : "#f1f5f9",
                    color: stockForm.type === t ? "#fff" : "#475569",
                    fontWeight: 700, cursor: "pointer"
                  }}>{t}</button>
              ))}
            </div>
          </div>
          <Input label="Quantity" type="number" value={stockForm.qty}
            onChange={e => setStockForm(f => ({ ...f, qty: e.target.value }))} />
          <Input label="Reference No (optional)" value={stockForm.ref}
            onChange={e => setStockForm(f => ({ ...f, ref: e.target.value }))} />
          <Input label="Notes (optional)" value={stockForm.notes}
            onChange={e => setStockForm(f => ({ ...f, notes: e.target.value }))} />
          <button onClick={handleStockSubmit} style={{
            width: "100%", padding: "12px", background: "#6366f1",
            color: "#fff", border: "none", borderRadius: 8,
            fontWeight: 700, fontSize: 15, cursor: "pointer", marginTop: 8
          }}>
            Save Transaction
          </button>
        </Modal>
      )}
    </div>
  );
}

// ── Add Product Tab ────────────────────────────────────────────────────────
function AddProductTab({ categories, onAdd }) {
  const [form, setForm] = useState({
    sku: "", name: "", description: "",
    categoryId: "", supplierId: "", unit: "pcs",
    purchasePrice: "", sellingPrice: "", gstPercent: 18,
    reorderLevel: 10, initialStock: 0
  });
  const [status, setStatus] = useState("");

  const set = (k, v) => setForm(f => ({ ...f, [k]: v }));

  const handleSubmit = async () => {
    try {
      const body = {
        ...form,
        categoryId: form.categoryId ? parseInt(form.categoryId) : null,
        supplierId: form.supplierId ? parseInt(form.supplierId) : null,
        purchasePrice: parseFloat(form.purchasePrice),
        sellingPrice: parseFloat(form.sellingPrice),
        gstPercent: parseFloat(form.gstPercent),
        reorderLevel: parseInt(form.reorderLevel),
        initialStock: parseInt(form.initialStock),
      };
      await onAdd(body);
      setStatus("✅ Product added successfully!");
      setForm({ sku: "", name: "", description: "", categoryId: "", supplierId: "",
        unit: "pcs", purchasePrice: "", sellingPrice: "", gstPercent: 18,
        reorderLevel: 10, initialStock: 0 });
    } catch {
      setStatus("❌ Error adding product.");
    }
  };

  return (
    <div style={{ maxWidth: 600 }}>
      <h2 style={{ fontSize: 18, fontWeight: 800, marginBottom: 20 }}>Add New Product</h2>
      {status && (
        <div style={{
          padding: "10px 16px", borderRadius: 8,
          background: status.startsWith("✅") ? "#dcfce7" : "#fee2e2",
          color: status.startsWith("✅") ? "#15803d" : "#dc2626",
          marginBottom: 16, fontWeight: 600
        }}>{status}</div>
      )}
      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0 16px" }}>
        <Input label="SKU *" value={form.sku} onChange={e => set("sku", e.target.value)} />
        <Input label="Product Name *" value={form.name} onChange={e => set("name", e.target.value)} />
        <div style={{ marginBottom: 14 }}>
          <label style={{ display: "block", fontSize: 13, fontWeight: 600, color: "#475569", marginBottom: 5 }}>
            Category
          </label>
          <select value={form.categoryId} onChange={e => set("categoryId", e.target.value)}
            style={{ width: "100%", padding: "9px 12px", border: "1.5px solid #e2e8f0",
              borderRadius: 8, fontSize: 14, background: "#fff" }}>
            <option value="">-- Select Category --</option>
            {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </div>
        <div style={{ marginBottom: 14 }}>
          <label style={{ display: "block", fontSize: 13, fontWeight: 600, color: "#475569", marginBottom: 5 }}>
            Unit
          </label>
          <select value={form.unit} onChange={e => set("unit", e.target.value)}
            style={{ width: "100%", padding: "9px 12px", border: "1.5px solid #e2e8f0",
              borderRadius: 8, fontSize: 14, background: "#fff" }}>
            {["pcs", "kg", "litre", "box", "ream", "roll", "meter"].map(u =>
              <option key={u}>{u}</option>
            )}
          </select>
        </div>
        <Input label="Purchase Price (₹)" type="number" value={form.purchasePrice}
          onChange={e => set("purchasePrice", e.target.value)} />
        <Input label="Selling Price (₹)" type="number" value={form.sellingPrice}
          onChange={e => set("sellingPrice", e.target.value)} />
        <Input label="GST %" type="number" value={form.gstPercent}
          onChange={e => set("gstPercent", e.target.value)} />
        <Input label="Reorder Level" type="number" value={form.reorderLevel}
          onChange={e => set("reorderLevel", e.target.value)} />
        <Input label="Initial Stock" type="number" value={form.initialStock}
          onChange={e => set("initialStock", e.target.value)} />
      </div>
      <Input label="Description (optional)" value={form.description}
        onChange={e => set("description", e.target.value)} />
      <button onClick={handleSubmit} style={{
        padding: "12px 32px", background: "#6366f1", color: "#fff",
        border: "none", borderRadius: 10, fontWeight: 700,
        fontSize: 15, cursor: "pointer"
      }}>
        Add Product
      </button>
    </div>
  );
}

// ── WhatsApp Guide Tab ────────────────────────────────────────────────────
function WhatsAppGuideTab() {
  const cmds = [
    { cmd: "CHECK USB cable", desc: "Check stock of any product by name or SKU" },
    { cmd: "ADD 50 wireless mouse", desc: "Add 50 units to stock" },
    { cmd: "SOLD 10 A4 paper", desc: "Record a sale (removes from stock)" },
    { cmd: "LOW STOCK", desc: "Get list of all low/out-of-stock items" },
    { cmd: "HELP", desc: "Show all available commands" },
  ];

  return (
    <div style={{ maxWidth: 640 }}>
      <h2 style={{ fontSize: 18, fontWeight: 800, marginBottom: 6 }}>
        📱 WhatsApp Integration Guide
      </h2>
      <p style={{ color: "#64748b", marginBottom: 24 }}>
        Connect your inventory to WhatsApp using the free Meta Cloud API.
        No business account needed for testing — just a Meta Developer account.
      </p>

      {/* Setup Steps */}
      <div style={{ marginBottom: 28 }}>
        <h3 style={{ fontSize: 15, fontWeight: 700, marginBottom: 14 }}>Setup Steps (Free)</h3>
        {[
          ["1", "Create Meta Developer Account", "Go to developers.facebook.com and sign up"],
          ["2", "Create a Meta App", "Choose Business type → Add WhatsApp product"],
          ["3", "Get Test Credentials", "Copy Access Token + Phone Number ID from the dashboard"],
          ["4", "Set Webhook URL", "Point to your server: https://yourserver.com/api/whatsapp/webhook"],
          ["5", "Add to appsettings.json", "Paste your token, phone ID, and a verify token"],
          ["6", "Expose locally (for dev)", "Use ngrok: ngrok http 5000 → use the HTTPS URL as webhook"],
        ].map(([num, title, desc]) => (
          <div key={num} style={{
            display: "flex", gap: 14, marginBottom: 14,
            background: "#f8fafc", borderRadius: 10, padding: 14
          }}>
            <div style={{
              width: 28, height: 28, background: "#6366f1", borderRadius: 99,
              color: "#fff", fontWeight: 800, display: "flex",
              alignItems: "center", justifyContent: "center", flexShrink: 0, fontSize: 13
            }}>{num}</div>
            <div>
              <div style={{ fontWeight: 700, fontSize: 14 }}>{title}</div>
              <div style={{ color: "#64748b", fontSize: 13 }}>{desc}</div>
            </div>
          </div>
        ))}
      </div>

      {/* Commands Reference */}
      <div>
        <h3 style={{ fontSize: 15, fontWeight: 700, marginBottom: 14 }}>Supported Commands</h3>
        <div style={{ borderRadius: 10, overflow: "hidden", border: "1.5px solid #e2e8f0" }}>
          {cmds.map((c, i) => (
            <div key={i} style={{
              display: "flex", gap: 20, padding: "12px 16px",
              borderBottom: i < cmds.length - 1 ? "1px solid #f1f5f9" : "none",
              background: i % 2 === 0 ? "#fff" : "#fafafa"
            }}>
              <code style={{
                background: "#ede9fe", color: "#7c3aed", padding: "3px 10px",
                borderRadius: 6, fontSize: 13, fontWeight: 700, flexShrink: 0
              }}>{c.cmd}</code>
              <span style={{ color: "#475569", fontSize: 13, alignSelf: "center" }}>{c.desc}</span>
            </div>
          ))}
        </div>
      </div>

      <div style={{
        marginTop: 24, padding: 16, background: "#fef3c7", borderRadius: 10,
        fontSize: 13, color: "#92400e"
      }}>
        💡 <strong>Free Tier Limits:</strong> Meta allows up to 5 recipient phone numbers
        and 1,000 free conversations/month on the test setup. For production,
        upgrade to a verified business number.
      </div>
    </div>
  );
}

// ── Main App ───────────────────────────────────────────────────────────────
export default function App() {
  const [tab, setTab] = useState("inventory");
  const [inventory, setInventory] = useState([]);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    setLoading(true);
    try {
      const [inv, cats] = await Promise.all([
        fetch(`${API}/inventory`).then(r => r.json()),
        fetch(`${API}/categories`).then(r => r.json()),
      ]);
      setInventory(inv.data || []);
      setCategories(cats.data || []);
    } catch {
      // Use mock data for demo
      setInventory([
        { productId:1, sku:"ELEC-001", productName:"USB-C Cable 1m", categoryName:"Electronics",
          unit:"pcs", sellingPrice:120, gstPercent:18, currentStock:120, reorderLevel:50, stockStatus:"IN_STOCK" },
        { productId:2, sku:"ELEC-002", productName:"Wireless Mouse", categoryName:"Electronics",
          unit:"pcs", sellingPrice:599, gstPercent:18, currentStock:35, reorderLevel:20, stockStatus:"IN_STOCK" },
        { productId:3, sku:"STAT-001", productName:"A4 Paper Ream", categoryName:"Stationery",
          unit:"ream", sellingPrice:150, gstPercent:5, currentStock:80, reorderLevel:30, stockStatus:"IN_STOCK" },
        { productId:6, sku:"PACK-002", productName:"Bubble Wrap Roll 50m", categoryName:"Packaging",
          unit:"roll", sellingPrice:700, gstPercent:18, currentStock:8, reorderLevel:10, stockStatus:"LOW_STOCK" },
        { productId:8, sku:"FG-001", productName:"Customised Pen Drive 32GB", categoryName:"Finished Goods",
          unit:"pcs", sellingPrice:450, gstPercent:18, currentStock:0, reorderLevel:15, stockStatus:"OUT_OF_STOCK" },
      ]);
      setCategories([
        { id:1, name:"Electronics" }, { id:2, name:"Stationery" },
        { id:3, name:"Packaging" }, { id:4, name:"Raw Materials" }, { id:5, name:"Finished Goods" },
      ]);
    }
    setLoading(false);
  };

  useEffect(() => { load(); }, []);

  const handleStockUpdate = async (productId, dto) => {
    try {
      await fetch(`${API}/inventory/${productId}/stock`, {
        method: "PATCH", headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dto)
      });
      await load();
    } catch { await load(); }
  };

  const handleAddProduct = async (dto) => {
    await fetch(`${API}/products`, {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify(dto)
    });
    await load();
  };

  // Stats
  const inStock = inventory.filter(p => p.stockStatus === "IN_STOCK").length;
  const lowStock = inventory.filter(p => p.stockStatus === "LOW_STOCK").length;
  const outStock = inventory.filter(p => p.stockStatus === "OUT_OF_STOCK").length;

  const TABS = [
    { id: "inventory", label: "📦 Inventory" },
    { id: "add", label: "➕ Add Product" },
    { id: "whatsapp", label: "📱 WhatsApp Setup" },
  ];

  return (
    <div style={{ minHeight: "100vh", background: "#f1f5f9", fontFamily: "'Segoe UI', sans-serif" }}>
      {/* Header */}
      <div style={{ background: "#fff", borderBottom: "1.5px solid #e2e8f0", padding: "0 32px" }}>
        <div style={{ maxWidth: 1200, margin: "0 auto", display: "flex", alignItems: "center", justifyContent: "space-between", height: 60 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
            <span style={{ fontSize: 24 }}>📦</span>
            <span style={{ fontWeight: 900, fontSize: 18, color: "#1e293b" }}>StockSetu</span>
            <span style={{
              background: "#ede9fe", color: "#7c3aed",
              fontSize: 10, fontWeight: 700, padding: "2px 8px",
              borderRadius: 99, letterSpacing: 0.5
            }}>INDIA SME</span>
          </div>
          <div style={{ fontSize: 13, color: "#64748b" }}>
            Inventory Management • WhatsApp Integrated
          </div>
        </div>
      </div>

      <div style={{ maxWidth: 1200, margin: "0 auto", padding: "28px 32px" }}>
        {/* Stats */}
        <div style={{ display: "flex", gap: 16, marginBottom: 28, flexWrap: "wrap" }}>
          <StatCard label="Total Products" value={inventory.length} sub="Active items" accent="#6366f1" />
          <StatCard label="In Stock" value={inStock} sub="Healthy" accent="#22c55e" />
          <StatCard label="Low Stock" value={lowStock} sub="Need reorder" accent="#f59e0b" />
          <StatCard label="Out of Stock" value={outStock} sub="Urgent" accent="#ef4444" />
        </div>

        {/* Tabs */}
        <div style={{
          background: "#fff", borderRadius: 14,
          boxShadow: "0 1px 4px #0001", overflow: "hidden"
        }}>
          <div style={{ display: "flex", borderBottom: "1.5px solid #f1f5f9", padding: "0 24px" }}>
            {TABS.map(t => (
              <button key={t.id} onClick={() => setTab(t.id)} style={{
                padding: "14px 20px", background: "none", border: "none",
                fontWeight: 700, fontSize: 14, cursor: "pointer",
                color: tab === t.id ? "#6366f1" : "#94a3b8",
                borderBottom: tab === t.id ? "2.5px solid #6366f1" : "2.5px solid transparent",
                marginBottom: -1.5
              }}>{t.label}</button>
            ))}
          </div>

          <div style={{ padding: 28 }}>
            {loading ? (
              <div style={{ textAlign: "center", padding: 60, color: "#94a3b8" }}>
                Loading inventory...
              </div>
            ) : tab === "inventory" ? (
              <InventoryTab inventory={inventory} onStockUpdate={handleStockUpdate} categories={categories} />
            ) : tab === "add" ? (
              <AddProductTab categories={categories} onAdd={handleAddProduct} />
            ) : (
              <WhatsAppGuideTab />
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
