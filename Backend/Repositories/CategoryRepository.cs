using Dapper;
using InventoryApi.Models;

namespace InventoryApi.Repositories;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<int> CreateAsync(Category category);
}

public class CategoryRepository : ICategoryRepository
{
    private readonly IDbConnectionFactory _db;

    public CategoryRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Category>(
            "SELECT id AS Id, name AS Name, description AS Description FROM categories ORDER BY name");
    }

    public async Task<int> CreateAsync(Category category)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO categories (name, description)
            VALUES (@Name, @Description);
            SELECT LAST_INSERT_ID();",
            category);
    }
}
