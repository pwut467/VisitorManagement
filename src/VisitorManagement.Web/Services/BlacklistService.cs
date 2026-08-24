using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;

namespace VisitorManagement.Web.Services;

public interface IBlacklistService
{
    Task<BlacklistEntry?> FindActiveAsync(string? nationalId, string? fullName, CancellationToken cancellationToken = default);
}

public class BlacklistService : IBlacklistService
{
    private readonly AppDbContext _db;

    public BlacklistService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<BlacklistEntry?> FindActiveAsync(string? nationalId, string? fullName, CancellationToken cancellationToken = default)
    {
        var now = TimeHelper.Now;
        var id = ThaiNationalId.Normalize(nationalId);
        var name = (fullName ?? string.Empty).Trim();

        return await _db.BlacklistEntries
            .Where(x => x.IsActive && (x.ExpiresAt == null || x.ExpiresAt > now))
            .Where(x =>
                (!string.IsNullOrEmpty(id) && x.NationalId == id) ||
                (!string.IsNullOrEmpty(name) && x.FullName == name))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
