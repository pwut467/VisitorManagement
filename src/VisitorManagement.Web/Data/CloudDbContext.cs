using Microsoft.EntityFrameworkCore;

namespace VisitorManagement.Web.Data;

/// <summary>
/// Same schema as the local <see cref="AppDbContext"/>, pointed at the cloud SQL Server.
/// Local remains the source of truth for UI reads; this context is for push/sync only.
/// </summary>
public class CloudDbContext : AppDbContext
{
    public CloudDbContext(DbContextOptions<CloudDbContext> options) : base(options)
    {
    }
}
