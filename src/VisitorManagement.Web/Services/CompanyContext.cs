using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;

namespace VisitorManagement.Web.Services;

public interface ICompanyContext
{
    Task<CompanyProfile> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompanyProfile>> ListAsync(CancellationToken cancellationToken = default);
    Task SetActiveAsync(int companyId, CancellationToken cancellationToken = default);
    Task<CompanyProfile> CreateAsync(string companyCode, string name, CancellationToken cancellationToken = default);
}

public sealed class CompanyContext : ICompanyContext
{
    public const string CookieName = "vm.activeCompanyId";

    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public CompanyContext(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task<CompanyProfile> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var companies = await _db.CompanyProfiles.OrderBy(c => c.Id).ToListAsync(cancellationToken);
        if (companies.Count == 0)
        {
            throw new InvalidOperationException("ยังไม่มีข้อมูลบริษัทในระบบ");
        }

        var cookieId = ReadCookieId();
        if (cookieId is int id)
        {
            var fromCookie = companies.FirstOrDefault(c => c.Id == id);
            if (fromCookie is not null)
            {
                return fromCookie;
            }
        }

        return companies.FirstOrDefault(c => c.IsActive) ?? companies[0];
    }

    public async Task<IReadOnlyList<CompanyProfile>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.CompanyProfiles.OrderBy(c => c.CompanyCode).ThenBy(c => c.Name).ToListAsync(cancellationToken);

    public async Task SetActiveAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var companies = await _db.CompanyProfiles.ToListAsync(cancellationToken);
        var selected = companies.FirstOrDefault(c => c.Id == companyId)
            ?? throw new InvalidOperationException("ไม่พบบริษัทที่เลือก");

        foreach (var c in companies)
        {
            c.IsActive = c.Id == selected.Id;
        }

        await _db.SaveChangesAsync(cancellationToken);
        WriteCookie(selected.Id);
    }

    public async Task<CompanyProfile> CreateAsync(string companyCode, string name, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(companyCode);
        if (code.Length == 0)
        {
            throw new InvalidOperationException("กรุณากรอกรหัสบริษัท");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("กรุณากรอกชื่อบริษัท");
        }

        if (await _db.CompanyProfiles.AnyAsync(c => c.CompanyCode == code, cancellationToken))
        {
            throw new InvalidOperationException($"รหัสบริษัท '{code}' มีอยู่แล้ว");
        }

        var template = await _db.CompanyProfiles.OrderByDescending(c => c.IsActive).ThenBy(c => c.Id).FirstOrDefaultAsync(cancellationToken);
        var company = new CompanyProfile
        {
            CompanyCode = code,
            Name = name.Trim(),
            Address = template?.Address,
            BadgeFooter = template?.BadgeFooter ?? "กรุณาติดบัตรนี้ตลอดเวลาที่อยู่ในบริษัท และคืนบัตรเมื่อออก",
            DefaultVisitHours = template?.DefaultVisitHours > 0 ? template.DefaultVisitHours : 2,
            OverstayGraceMinutes = template?.OverstayGraceMinutes ?? 15,
            AutoPrintBadge = template?.AutoPrintBadge ?? true,
            IsActive = false,
            SeedRevision = template?.SeedRevision ?? 2,
            CloudEnabled = template?.CloudEnabled ?? true,
            CloudServer = template?.CloudServer ?? "192.168.11.204",
            CloudDatabase = template?.CloudDatabase ?? "VisitorManagment",
            CloudUseWindowsAuth = template?.CloudUseWindowsAuth ?? false,
            CloudUserId = template?.CloudUserId,
            CloudPassword = template?.CloudPassword
        };
        _db.CompanyProfiles.Add(company);
        await _db.SaveChangesAsync(cancellationToken);
        await SetActiveAsync(company.Id, cancellationToken);
        return company;
    }

    public static string NormalizeCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var chars = value.Trim().ToUpperInvariant()
            .Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_')
            .ToArray();
        return new string(chars);
    }

    private int? ReadCookieId()
    {
        var raw = _http.HttpContext?.Request.Cookies[CookieName];
        return int.TryParse(raw, out var id) ? id : null;
    }

    private void WriteCookie(int companyId)
    {
        var ctx = _http.HttpContext;
        if (ctx is null)
        {
            return;
        }

        ctx.Response.Cookies.Append(CookieName, companyId.ToString(), new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddYears(1)
        });
    }
}
