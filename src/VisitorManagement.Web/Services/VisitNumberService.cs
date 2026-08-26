using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;

namespace VisitorManagement.Web.Services;

public interface IVisitNumberService
{
    Task<string> NextAsync(DateTime localDate, int companyProfileId, string companyCode, CancellationToken cancellationToken = default);
}

public class VisitNumberService : IVisitNumberService
{
    private readonly AppDbContext _db;

    public VisitNumberService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string> NextAsync(
        DateTime localDate,
        int companyProfileId,
        string companyCode,
        CancellationToken cancellationToken = default)
    {
        var code = CompanyContext.NormalizeCode(companyCode);
        if (code.Length == 0)
        {
            code = "DEFAULT";
        }

        var prefix = $"{code}-V{localDate:yyyyMMdd}-";
        var last = await _db.Visits
            .Where(v => v.CompanyProfileId == companyProfileId && v.VisitNumber.StartsWith(prefix))
            .OrderByDescending(v => v.VisitNumber)
            .Select(v => v.VisitNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var seq = 1;
        if (!string.IsNullOrEmpty(last) && last.Length >= prefix.Length + 4
            && int.TryParse(last[prefix.Length..], out var parsed))
        {
            seq = parsed + 1;
        }

        return $"{prefix}{seq:0000}";
    }
}
