-- ============================================================
-- SEED DATA - Sample inventory for an Indian SME
-- ============================================================
USE inventory_db;

-- Categories
INSERT INTO categories (name, description) VALUES
('Electronics',      'Electronic components and devices'),
('Stationery',       'Office and school stationery'),
('Packaging',        'Boxes, tapes, and packaging materials'),
('Raw Materials',    'Manufacturing inputs'),
('Finished Goods',   'Ready-to-sell products');

-- Suppliers
INSERT INTO suppliers (name, contact_name, phone, email, gstin) VALUES
('Sharma Traders',       'Rajesh Sharma',  '9876543210', 'rajesh@sharmatraders.in', '22AAAAA0000A1Z5'),
('Mumbai Electronics',   'Priya Mehta',   '9012345678', 'priya@mbielec.com',       '27BBBBB1111B2Z6'),
('Delhi Supplies Pvt Ltd','Amit Kumar',   '9123456789', 'amit@delhisupplies.in',   '07CCCCC2222C3Z7');

-- Products
INSERT INTO products (sku, name, category_id, supplier_id, unit, purchase_price, selling_price, gst_percent, reorder_level) VALUES
('ELEC-001', 'USB-C Cable 1m',           1, 2, 'pcs',  45.00,  120.00, 18, 50),
('ELEC-002', 'Wireless Mouse',           1, 2, 'pcs', 250.00,  599.00, 18, 20),
('STAT-001', 'A4 Paper Ream 500 sheets', 2, 1, 'ream',  85.00,  150.00,  5, 30),
('STAT-002', 'Ballpoint Pen (Box of 10)',2, 1, 'box',   22.00,   60.00, 18, 25),
('PACK-001', 'Corrugated Box 12x10x8',   3, 3, 'pcs',   18.00,   45.00, 18, 100),
('PACK-002', 'Bubble Wrap Roll 50m',     3, 3, 'roll', 320.00,  700.00, 18, 10),
('RAW-001',  'Aluminium Sheet 1mm',      4, 3, 'kg',   190.00,  280.00, 18, 200),
('FG-001',   'Customised Pen Drive 32GB',5, 2, 'pcs',  180.00,  450.00, 18, 15);

-- Inventory (initial stock)
INSERT INTO inventory (product_id, quantity, warehouse) VALUES
(1, 120, 'Main Warehouse'),
(2,  35, 'Main Warehouse'),
(3,  80, 'Main Warehouse'),
(4,  60, 'Main Warehouse'),
(5, 250, 'Main Warehouse'),
(6,   8, 'Main Warehouse'),   -- LOW STOCK (reorder=10)
(7, 500, 'Main Warehouse'),
(8,   0, 'Main Warehouse');   -- OUT OF STOCK

-- Some sample stock transactions
INSERT INTO stock_transactions (product_id, transaction_type, quantity, reference_no, notes, source, created_by) VALUES
(1, 'IN',   200, 'PO-2024-001', 'Initial stock purchase', 'WEB', 'admin'),
(1, 'OUT',   80, 'INV-2024-042', 'Sold to customer', 'WEB', 'admin'),
(6, 'IN',   50,  'PO-2024-002', 'Restock bubble wrap', 'WEB', 'admin'),
(6, 'OUT',  42,  'INV-2024-038', 'Dispatched orders', 'WEB', 'admin'),
(8, 'OUT',  15,  'INV-2024-040', 'Sold all stock', 'WEB', 'admin');
