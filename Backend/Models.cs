namespace InventoryApi.Models;

public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string Unit { get; set; } = "pcs";
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal GstPercent { get; set; } = 18;
    public int ReorderLevel { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class InventoryItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Unit { get; set; } = "pcs";
    public decimal SellingPrice { get; set; }
    public decimal GstPercent { get; set; }
    public int CurrentStock { get; set; }
    public string Warehouse { get; set; } = "Main Warehouse";
    public int ReorderLevel { get; set; }
    public string StockStatus { get; set; } = string.Empty;
    public string? SupplierName { get; set; }
    public string? SupplierPhone { get; set; }
}

public class StockTransaction
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string TransactionType { get; set; } = string.Empty; // IN, OUT, ADJUSTMENT
    public int Quantity { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
    public string Source { get; set; } = "WEB";
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// DTOs
public record CreateProductDto(
    string Sku,
    string Name,
    string? Description,
    int? CategoryId,
    int? SupplierId,
    string Unit,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal GstPercent,
    int ReorderLevel,
    int InitialStock
);

public record UpdateStockDto(
    string TransactionType,   // IN, OUT, ADJUSTMENT
    int Quantity,
    string? ReferenceNo,
    string? Notes,
    string Source = "WEB",
    string? CreatedBy = "admin"
);

public record ApiResponse<T>(bool Success, string Message, T? Data);
