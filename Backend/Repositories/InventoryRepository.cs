using Dapper;
using InventoryApi.Models;

namespace InventoryApi.Repositories;

public interface IInventoryRepository
{
    Task<IEnumerable<InventoryItem>> GetAllAsync();
    Task<InventoryItem?> GetByProductIdAsync(int productId);
    Task<IEnumerable<InventoryItem>> GetLowStockAsync();
    Task UpdateStockAsync(int productId, UpdateStockDto dto);
    Task<IEnumerable<StockTransaction>> GetTransactionHistoryAsync(int productId);
}

public class InventoryRepository : IInventoryRepository
{
    private readonly IDbConnectionFactory _db;

    public InventoryRepository(IDbConnectionFactory db) => _db = db;

    /// <summary>
    /// Inner query that resolves stock status per product.
    /// Wrapped in a subquery by the low-stock helper so MySQL can filter on the alias.
    /// </summary>
    private const string BaseQuery = @"
        SELECT
            i.id                                                   AS Id,
            p.id                                                   AS ProductId,
            p.sku                                                  AS Sku,
            p.name                                                 AS ProductName,
            COALESCE(c.name, '')                                   AS CategoryName,
            p.unit                                                 AS Unit,
            p.selling_price                                        AS SellingPrice,
            p.gst_percent                                          AS GstPercent,
            COALESCE(i.quantity, 0)                                AS CurrentStock,
            COALESCE(i.warehouse, 'Main Warehouse')                AS Warehouse,
            p.reorder_level                                        AS ReorderLevel,
            CASE
                WHEN COALESCE(i.quantity, 0) = 0                        THEN 'OUT_OF_STOCK'
                WHEN COALESCE(i.quantity, 0) <= p.reorder_level         THEN 'LOW_STOCK'
                ELSE 'IN_STOCK'
            END                                                    AS StockStatus,
            s.name                                                 AS SupplierName,
            s.phone                                                AS SupplierPhone
        FROM products p
        LEFT JOIN categories c ON c.id = p.category_id
        LEFT JOIN inventory   i ON i.product_id = p.id
        LEFT JOIN suppliers   s ON s.id = p.supplier_id
        WHERE p.is_active = TRUE";

    public async Task<IEnumerable<InventoryItem>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<InventoryItem>($"{BaseQuery} ORDER BY p.name");
    }

    public async Task<InventoryItem?> GetByProductIdAsync(int productId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<InventoryItem>(
            $"{BaseQuery} AND p.id = @ProductId",
            new { ProductId = productId });
    }

    public async Task<IEnumerable<InventoryItem>> GetLowStockAsync()
    {
        // Wrap in a derived table so WHERE can filter on the computed StockStatus alias
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<InventoryItem>($@"
            SELECT * FROM (
                {BaseQuery}
            ) AS inv_status
            WHERE StockStatus IN ('OUT_OF_STOCK', 'LOW_STOCK')
            ORDER BY CurrentStock ASC");
    }

    public async Task UpdateStockAsync(int productId, UpdateStockDto dto)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        using var txn = conn.BeginTransaction();
        try
        {
            // Adjust the current stock quantity
            var updateSql = dto.TransactionType switch
            {
                "IN"  => "UPDATE inventory SET quantity = quantity + @Quantity WHERE product_id = @ProductId",
                "OUT" => "UPDATE inventory SET quantity = GREATEST(0, quantity - @Quantity) WHERE product_id = @ProductId",
                _     => "UPDATE inventory SET quantity = @Quantity WHERE product_id = @ProductId"  // ADJUSTMENT
            };

            await conn.ExecuteAsync(updateSql,
                new { dto.Quantity, ProductId = productId }, txn);

            // Audit trail
            await conn.ExecuteAsync(@"
                INSERT INTO stock_transactions
                    (product_id, transaction_type, quantity, reference_no, notes, source, created_by)
                VALUES (@ProductId, @TransactionType, @Quantity, @ReferenceNo, @Notes, @Source, @CreatedBy)",
                new
                {
                    ProductId       = productId,
                    dto.TransactionType,
                    dto.Quantity,
                    dto.ReferenceNo,
                    dto.Notes,
                    dto.Source,
                    dto.CreatedBy
                }, txn);

            txn.Commit();
        }
        catch
        {
            txn.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<StockTransaction>> GetTransactionHistoryAsync(int productId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<StockTransaction>(@"
            SELECT
                id               AS Id,
                product_id       AS ProductId,
                transaction_type AS TransactionType,
                quantity         AS Quantity,
                reference_no     AS ReferenceNo,
                notes            AS Notes,
                source           AS Source,
                created_by       AS CreatedBy,
                created_at       AS CreatedAt
            FROM stock_transactions
            WHERE product_id = @ProductId
            ORDER BY created_at DESC
            LIMIT 100",
            new { ProductId = productId });
    }
}
