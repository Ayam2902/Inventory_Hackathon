-- ============================================================
-- INVENTORY MANAGEMENT SYSTEM - MySQL Schema
-- For Small & Medium Enterprises (India)
-- ============================================================

CREATE DATABASE IF NOT EXISTS inventory_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE inventory_db;

-- ------------------------------------------------------------
-- CATEGORIES TABLE
-- ------------------------------------------------------------
CREATE TABLE categories (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    name        VARCHAR(100) NOT NULL,
    description VARCHAR(255),
    created_at  DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at  DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_category_name (name)
);

-- ------------------------------------------------------------
-- SUPPLIERS TABLE
-- ------------------------------------------------------------
CREATE TABLE suppliers (
    id           INT AUTO_INCREMENT PRIMARY KEY,
    name         VARCHAR(150) NOT NULL,
    contact_name VARCHAR(100),
    phone        VARCHAR(15),
    email        VARCHAR(100),
    address      TEXT,
    gstin        VARCHAR(20),          -- GST Identification Number (India)
    created_at   DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at   DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- ------------------------------------------------------------
-- PRODUCTS TABLE
-- ------------------------------------------------------------
CREATE TABLE products (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    sku             VARCHAR(50) NOT NULL,         -- Stock Keeping Unit
    name            VARCHAR(200) NOT NULL,
    description     TEXT,
    category_id     INT,
    supplier_id     INT,
    unit            VARCHAR(20) NOT NULL DEFAULT 'pcs',  -- pcs, kg, litre, box, etc.
    purchase_price  DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    selling_price   DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    gst_percent     DECIMAL(5,2) NOT NULL DEFAULT 18.00,  -- GST rate (India)
    reorder_level   INT NOT NULL DEFAULT 10,      -- Alert when stock hits this level
    is_active       BOOLEAN DEFAULT TRUE,
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_sku (sku),
    FOREIGN KEY (category_id) REFERENCES categories(id) ON DELETE SET NULL,
    FOREIGN KEY (supplier_id) REFERENCES suppliers(id) ON DELETE SET NULL
);

-- ------------------------------------------------------------
-- INVENTORY TABLE (current stock levels)
-- ------------------------------------------------------------
CREATE TABLE inventory (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    product_id      INT NOT NULL,
    quantity        INT NOT NULL DEFAULT 0,
    warehouse       VARCHAR(100) DEFAULT 'Main Warehouse',
    last_updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_product_warehouse (product_id, warehouse),
    FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE
);

-- ------------------------------------------------------------
-- STOCK TRANSACTIONS TABLE (audit trail for every movement)
-- ------------------------------------------------------------
CREATE TABLE stock_transactions (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    product_id      INT NOT NULL,
    transaction_type ENUM('IN','OUT','ADJUSTMENT','RETURN') NOT NULL,
    quantity        INT NOT NULL,           -- positive = stock added, negative = stock removed
    reference_no    VARCHAR(100),           -- PO number, invoice number, etc.
    notes           TEXT,
    source          ENUM('WEB','WHATSAPP','API') DEFAULT 'WEB',
    created_by      VARCHAR(100),           -- user or whatsapp number
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (product_id) REFERENCES products(id)
);

-- ------------------------------------------------------------
-- WHATSAPP SESSIONS TABLE (track conversation state)
-- ------------------------------------------------------------
CREATE TABLE whatsapp_sessions (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    phone_number    VARCHAR(20) NOT NULL,
    state           VARCHAR(50) DEFAULT 'IDLE',   -- conversation state machine
    context_data    JSON,                          -- store partial conversation data
    last_message    TEXT,
    updated_at      DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_phone (phone_number)
);

-- ============================================================
-- VIEWS for quick reporting
-- ============================================================

CREATE VIEW vw_inventory_status AS
SELECT
    p.id,
    p.sku,
    p.name AS product_name,
    c.name AS category,
    p.unit,
    p.selling_price,
    p.gst_percent,
    i.quantity AS current_stock,
    i.warehouse,
    p.reorder_level,
    CASE
        WHEN i.quantity = 0          THEN 'OUT_OF_STOCK'
        WHEN i.quantity <= p.reorder_level THEN 'LOW_STOCK'
        ELSE 'IN_STOCK'
    END AS stock_status,
    s.name AS supplier_name,
    s.phone AS supplier_phone
FROM products p
LEFT JOIN categories c    ON c.id = p.category_id
LEFT JOIN inventory i     ON i.product_id = p.id
LEFT JOIN suppliers s     ON s.id = p.supplier_id
WHERE p.is_active = TRUE;

CREATE VIEW vw_low_stock_alerts AS
SELECT * FROM vw_inventory_status
WHERE stock_status IN ('OUT_OF_STOCK', 'LOW_STOCK')
ORDER BY current_stock ASC;
