using Dapper;
using InventoryApi.Models;

namespace InventoryApi.Repositories;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<Product?> GetBySkuAsync(string sku);
    Task<int> CreateAsync(CreateProductDto dto);
    Task<bool> UpdateAsync(int id, CreateProductDto dto);
    Task<bool> DeleteAsync(int id);
}

public class ProductRepository : IProductRepository
{
    private readonly IDbConnectionFactory _db;

    public ProductRepository(IDbConnectionFactory db) => _db = db;

    // Shared SELECT columns — maps snake_case DB columns to PascalCase model props
    private const string SelectColumns = @"
        p.id           AS Id,
        p.sku          AS Sku,
        p.name         AS Name,
        p.description  AS Description,
        p.category_id  AS CategoryId,
        c.name         AS CategoryName,
        p.supplier_id  AS SupplierId,
        s.name         AS SupplierName,
        p.unit         AS Unit,
        p.purchase_price AS PurchasePrice,
        p.selling_price  AS SellingPrice,
        p.gst_percent    AS GstPercent,
        p.reorder_level  AS ReorderLevel,
        p.is_active      AS IsActive,
        p.created_at     AS CreatedAt";

    private const string FromClause = @"
        FROM products p
        LEFT JOIN categories c ON c.id = p.category_id
        LEFT JOIN suppliers  s ON s.id = p.supplier_id";

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Product>(
            $"SELECT {SelectColumns} {FromClause} WHERE p.is_active = TRUE ORDER BY p.name");
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Product>(
            $"SELECT {SelectColumns} {FromClause} WHERE p.id = @Id AND p.is_active = TRUE",
            new { Id = id });
    }

    public async Task<Product?> GetBySkuAsync(string sku)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Product>(
            $"SELECT {SelectColumns} {FromClause} WHERE p.sku = @Sku AND p.is_active = TRUE",
            new { Sku = sku });
    }

    public async Task<int> CreateAsync(CreateProductDto dto)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        using var txn = conn.BeginTransaction();
        try
        {
            // 1. Insert product row
            var productId = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO products
                    (sku, name, description, category_id, supplier_id, unit,
                     purchase_price, selling_price, gst_percent, reorder_level)
                VALUES
                    (@Sku, @Name, @Description, @CategoryId, @SupplierId, @Unit,
                     @PurchasePrice, @SellingPrice, @GstPercent, @ReorderLevel);
                SELECT LAST_INSERT_ID();", dto, txn);

            // 2. Create inventory row with initial stock
            await conn.ExecuteAsync(@"
                INSERT INTO inventory (product_id, quantity)
                VALUES (@ProductId, @Quantity)
                ON DUPLICATE KEY UPDATE quantity = @Quantity",
                new { ProductId = productId, Quantity = dto.InitialStock }, txn);

            // 3. Record opening stock transaction (if any)
            if (dto.InitialStock > 0)
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO stock_transactions
                        (product_id, transaction_type, quantity, notes, source, created_by)
                    VALUES (@ProductId, 'IN', @Quantity, 'Opening stock', 'WEB', 'admin')",
                    new { ProductId = productId, Quantity = dto.InitialStock }, txn);
            }

            txn.Commit();
            return productId;
        }
        catch
        {
            txn.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int id, CreateProductDto dto)
    {
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(@"
            UPDATE products SET
                sku            = @Sku,
                name           = @Name,
                description    = @Description,
                category_id    = @CategoryId,
                supplier_id    = @SupplierId,
                unit           = @Unit,
                purchase_price = @PurchasePrice,
                selling_price  = @SellingPrice,
                gst_percent    = @GstPercent,
                reorder_level  = @ReorderLevel
            WHERE id = @Id AND is_active = TRUE",
            new
            {
                Id = id,
                dto.Sku, dto.Name, dto.Description, dto.CategoryId, dto.SupplierId,
                dto.Unit, dto.PurchasePrice, dto.SellingPrice, dto.GstPercent, dto.ReorderLevel
            });
        return affected > 0;
    }

    /// <summary>Soft-delete: marks the product inactive instead of removing it.</summary>
    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(
            "UPDATE products SET is_active = FALSE WHERE id = @Id AND is_active = TRUE",
            new { Id = id });
        return affected > 0;
    }
}
