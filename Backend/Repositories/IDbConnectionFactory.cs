using MySql.Data.MySqlClient;
using System.Data;

namespace InventoryApi.Repositories;

/// <summary>
/// Abstracts database connection creation so repositories stay testable
/// and the connection string is configured once in Program.cs.
/// </summary>
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class MySqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public MySqlConnectionFactory(string connectionString) =>
        _connectionString = connectionString;

    public IDbConnection CreateConnection() =>
        new MySqlConnection(_connectionString);
}
