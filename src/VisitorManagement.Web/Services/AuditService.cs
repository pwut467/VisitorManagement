using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;

namespace VisitorManagement.Web.Services;

public interface IAuditService
{
    Task WriteAsync(string? actorUserId, string action, string? entityType, string? entityId, string? detail, string? ip);
}

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task WriteAsync(string? actorUserId, string action, string? entityType, string? entityId, string? detail, string? ip)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Detail = detail,
            IpAddress = ip,
            CreatedAt = TimeHelper.Now
        });
        await _db.SaveChangesAsync();
    }
}
