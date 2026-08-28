namespace VisitorManagement.Web.Services;

/// <summary>
/// Holds the active local SQL connection string. Bootstrap may switch to a TCP
/// variant when <c>.\SQLEXPRESS</c> named-pipes fail even though Express is installed.
/// </summary>
public sealed class SqlConnectionResolver
{
    public const string DefaultSqlServer =
        @"Server=localhost\SQLEXPRESS;Database=VisitorManagment;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

    private string _connectionString;

    public SqlConnectionResolver(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("SqlServer") ?? DefaultSqlServer;
    }

    public string ConnectionString => _connectionString;

    public void Use(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }
}
