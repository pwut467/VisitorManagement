using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using VisitorManagement.Web.Data;
using VisitorManagement.Web.Models;

namespace VisitorManagement.Web.Services;

public interface ICloudVisitSyncService
{
    Task<bool> TrySyncVisitAsync(int visitId, CancellationToken cancellationToken = default);
    Task<int> SyncPendingAsync(CancellationToken cancellationToken = default);
    Task<bool> ProbeAsync(CancellationToken cancellationToken = default);
}

public sealed class CloudVisitSyncService : ICloudVisitSyncService
{
    private static readonly SemaphoreSlim SchemaLock = new(1, 1);
    private static readonly HashSet<string> ReadySchemas = new(StringComparer.OrdinalIgnoreCase);

    private readonly AppDbContext _local;
    private readonly ICloudConnectionStatus _status;
    private readonly ICloudOptionsProvider _optionsProvider;
    private readonly ILogger<CloudVisitSyncService> _logger;

    public CloudVisitSyncService(
        AppDbContext local,
        ICloudConnectionStatus status,
        ICloudOptionsProvider optionsProvider,
        ILogger<CloudVisitSyncService> logger)
    {
        _local = local;
        _status = status;
        _optionsProvider = optionsProvider;
        _logger = logger;
    }

    public async Task<bool> ProbeAsync(CancellationToken cancellationToken = default)
    {
        var options = await _optionsProvider.GetAsync(cancellationToken);
        if (!options.Enabled)
        {
            _status.SetHealth(false, "ปิดการซิงก์คลาวด์", options);
            return false;
        }

        if (!options.IsConfigured || string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            _status.SetHealth(false, "ยังไม่ได้ตั้งค่า Username/Password ของ Cloud SQL (ไปที่ ตั้งค่าบริษัท)", options);
            return false;
        }

        try
        {
            await using var cloud = CreateCloudContext(options);
            var ok = await cloud.Database.CanConnectAsync(cancellationToken);
            if (!ok)
            {
                _status.SetHealth(false, "เชื่อมต่อ Cloud SQL ไม่ได้", options);
                return false;
            }

            await EnsureCloudSchemaAsync(cloud, options, cancellationToken);
            _status.SetHealth(true, null, options);
            await RefreshPendingCountAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            var message = CloudOptions.DescribeError(ex);
            _logger.LogWarning(ex, "Cloud SQL health check failed for {Server}/{Database}", options.Server, options.Database);
            _status.SetHealth(false, message, options);
            return false;
        }
    }

    public async Task<bool> TrySyncVisitAsync(int visitId, CancellationToken cancellationToken = default)
    {
        var options = await _optionsProvider.GetAsync(cancellationToken);
        var visit = await LoadLocalVisitAsync(visitId, cancellationToken);
        if (visit is null)
        {
            return false;
        }

        if (!options.Enabled)
        {
            await MarkLocalPendingAsync(visit, "ปิดการซิงก์คลาวด์", cancellationToken);
            _status.SetHealth(false, "ปิดการซิงก์คลาวด์", options);
            return false;
        }

        if (!options.IsConfigured || string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            await MarkLocalPendingAsync(visit, "ยังไม่ได้ตั้งค่า Username/Password ของ Cloud SQL", cancellationToken);
            _status.SetHealth(false, "ยังไม่ได้ตั้งค่า Username/Password ของ Cloud SQL (ไปที่ ตั้งค่าบริษัท)", options);
            return false;
        }

        try
        {
            await using var cloud = CreateCloudContext(options);
            if (!await cloud.Database.CanConnectAsync(cancellationToken))
            {
                await MarkLocalPendingAsync(visit, "เชื่อมต่อ Cloud SQL ไม่ได้", cancellationToken);
                _status.SetHealth(false, "เชื่อมต่อ Cloud SQL ไม่ได้", options);
                return false;
            }

            await EnsureCloudSchemaAsync(cloud, options, cancellationToken);
            await UpsertVisitAsync(cloud, visit, cancellationToken);
            await cloud.SaveChangesAsync(cancellationToken);

            visit.CloudSynced = true;
            visit.CloudSyncedAt = TimeHelper.Now;
            visit.CloudSyncError = null;
            await _local.SaveChangesAsync(cancellationToken);
            _status.SetHealth(true, null, options);
            await RefreshPendingCountAsync(cancellationToken);
            _logger.LogInformation(
                "Synced visit {VisitNumber} to cloud {Server}/{Database}",
                visit.VisitNumber,
                options.Server,
                options.Database);
            return true;
        }
        catch (Exception ex)
        {
            var message = CloudOptions.DescribeError(ex);
            _logger.LogWarning(ex, "Failed syncing visit {VisitNumber} to cloud {Server}/{Database}", visit.VisitNumber, options.Server, options.Database);
            await MarkLocalPendingAsync(visit, message, cancellationToken);
            _status.SetHealth(false, message, options);
            return false;
        }
    }

    public async Task<int> SyncPendingAsync(CancellationToken cancellationToken = default)
    {
        var options = await _optionsProvider.GetAsync(cancellationToken);
        if (!options.Enabled || !options.IsConfigured || string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            await RefreshPendingCountAsync(cancellationToken);
            return 0;
        }

        if (!await ProbeAsync(cancellationToken))
        {
            await RefreshPendingCountAsync(cancellationToken);
            return 0;
        }

        var pendingIds = await _local.Visits
            .AsNoTracking()
            .Where(v => !v.CloudSynced)
            .OrderBy(v => v.Id)
            .Select(v => v.Id)
            .Take(100)
            .ToListAsync(cancellationToken);

        var synced = 0;
        foreach (var id in pendingIds)
        {
            if (await TrySyncVisitAsync(id, cancellationToken))
            {
                synced++;
            }
        }

        await RefreshPendingCountAsync(cancellationToken);
        return synced;
    }

    private async Task RefreshPendingCountAsync(CancellationToken cancellationToken)
    {
        var count = await _local.Visits.CountAsync(v => !v.CloudSynced, cancellationToken);
        _status.SetPendingCount(count);
    }

    private async Task MarkLocalPendingAsync(Visit visit, string error, CancellationToken cancellationToken)
    {
        visit.CloudSynced = false;
        visit.CloudSyncError = error.Length <= 500 ? error : error[..500];
        await _local.SaveChangesAsync(cancellationToken);
        await RefreshPendingCountAsync(cancellationToken);
    }

    private Task<Visit?> LoadLocalVisitAsync(int visitId, CancellationToken cancellationToken) =>
        _local.Visits
            .Include(v => v.Visitor)
            .Include(v => v.VisitorType)
            .Include(v => v.VisitPurpose)
            .Include(v => v.HostEmployee).ThenInclude(h => h.Department)
            .Include(v => v.GateIn)
            .Include(v => v.GateOut)
            .FirstOrDefaultAsync(v => v.Id == visitId, cancellationToken);

    private async Task UpsertVisitAsync(AppDbContext cloud, Visit local, CancellationToken cancellationToken)
    {
        var visitor = await UpsertVisitorAsync(cloud, local.Visitor, cancellationToken);
        var type = await EnsureNamedAsync(
            cloud.VisitorTypes,
            x => x.Name == local.VisitorType.Name,
            () => new VisitorType
            {
                Name = local.VisitorType.Name,
                BadgeLabel = local.VisitorType.BadgeLabel,
                Color = local.VisitorType.Color,
                RequiresEscortDefault = local.VisitorType.RequiresEscortDefault,
                IsActive = true
            },
            cancellationToken);
        var purpose = await EnsureNamedAsync(
            cloud.VisitPurposes,
            x => x.Name == local.VisitPurpose.Name,
            () => new VisitPurpose { Name = local.VisitPurpose.Name, IsActive = true },
            cancellationToken);
        var host = await UpsertHostAsync(cloud, local.HostEmployee, cancellationToken);
        var gateIn = local.GateIn is null
            ? null
            : await EnsureNamedAsync(
                cloud.Gates,
                x => x.Name == local.GateIn.Name,
                () => new Gate { Name = local.GateIn.Name, Location = local.GateIn.Location, IsActive = true },
                cancellationToken);
        var gateOut = local.GateOut is null
            ? null
            : await EnsureNamedAsync(
                cloud.Gates,
                x => x.Name == local.GateOut.Name,
                () => new Gate { Name = local.GateOut.Name, Location = local.GateOut.Location, IsActive = true },
                cancellationToken);

        var cloudVisit = await cloud.Visits.FirstOrDefaultAsync(v => v.VisitCode == local.VisitCode, cancellationToken);
        if (cloudVisit is null)
        {
            cloudVisit = new Visit
            {
                VisitCode = local.VisitCode,
                VisitNumber = local.VisitNumber,
                CreatedAt = local.CreatedAt
            };
            cloud.Visits.Add(cloudVisit);
        }

        cloudVisit.VisitNumber = local.VisitNumber;
        cloudVisit.Visitor = visitor;
        cloudVisit.VisitorType = type;
        cloudVisit.VisitPurpose = purpose;
        cloudVisit.HostEmployee = host;
        cloudVisit.GateIn = gateIn;
        cloudVisit.GateOut = gateOut;
        cloudVisit.CompanyName = local.CompanyName;
        cloudVisit.GuestTitle = local.GuestTitle;
        cloudVisit.GuestFirstName = local.GuestFirstName;
        cloudVisit.GuestLastName = local.GuestLastName;
        cloudVisit.GuestPhone = local.GuestPhone;
        cloudVisit.PurposeDetail = local.PurposeDetail;
        cloudVisit.VehiclePlate = local.VehiclePlate;
        cloudVisit.VehicleType = local.VehicleType;
        cloudVisit.ItemsBrought = local.ItemsBrought;
        cloudVisit.AccompanyingCount = local.AccompanyingCount;
        cloudVisit.AccompanyingNames = local.AccompanyingNames;
        cloudVisit.RequiresEscort = local.RequiresEscort;
        cloudVisit.AccessArea = local.AccessArea;
        cloudVisit.Notes = local.Notes;
        cloudVisit.AppointmentAt = local.AppointmentAt;
        cloudVisit.CheckInAt = local.CheckInAt;
        cloudVisit.CheckOutAt = local.CheckOutAt;
        cloudVisit.ExpectedCheckoutAt = local.ExpectedCheckoutAt;
        cloudVisit.BadgePrintedAt = local.BadgePrintedAt;
        cloudVisit.PdpaConsentAt = local.PdpaConsentAt;
        cloudVisit.Status = local.Status;
        cloudVisit.PhotoPath = local.PhotoPath;
        cloudVisit.RegisteredByUserId = null;
        cloudVisit.CheckedOutByUserId = null;
        cloudVisit.CloudSynced = true;
        cloudVisit.CloudSyncedAt = TimeHelper.Now;
        cloudVisit.CloudSyncError = null;
    }

    private static async Task<Visitor> UpsertVisitorAsync(AppDbContext cloud, Visitor local, CancellationToken cancellationToken)
    {
        var visitor = await cloud.Visitors.FirstOrDefaultAsync(v => v.NationalId == local.NationalId, cancellationToken);
        if (visitor is null)
        {
            visitor = new Visitor
            {
                NationalId = local.NationalId,
                Title = local.Title,
                FirstName = local.FirstName,
                LastName = local.LastName,
                Phone = local.Phone,
                Email = local.Email,
                CompanyName = local.CompanyName,
                Address = local.Address,
                DateOfBirth = local.DateOfBirth,
                PhotoPath = local.PhotoPath,
                CreatedAt = local.CreatedAt,
                UpdatedAt = local.UpdatedAt
            };
            cloud.Visitors.Add(visitor);
            return visitor;
        }

        // Keep the first-known master identity on cloud; visit-level names live on Visits.Guest*.
        return visitor;
    }

    private static async Task<Employee> UpsertHostAsync(AppDbContext cloud, Employee local, CancellationToken cancellationToken)
    {
        var deptCode = local.Department?.Code ?? "GEN";
        var deptName = local.Department?.Name ?? "ทั่วไป";
        var dept = await EnsureNamedAsync(
            cloud.Departments,
            x => x.Code == deptCode,
            () => new Department { Code = deptCode, Name = deptName },
            cancellationToken);

        var host = await cloud.Employees.FirstOrDefaultAsync(e => e.EmployeeCode == local.EmployeeCode, cancellationToken);
        if (host is null)
        {
            host = await cloud.Employees.FirstOrDefaultAsync(e => e.FullName == local.FullName, cancellationToken);
        }

        if (host is null)
        {
            host = new Employee
            {
                EmployeeCode = local.EmployeeCode,
                FullName = local.FullName,
                Department = dept,
                IsActive = true
            };
            cloud.Employees.Add(host);
        }
        else
        {
            host.FullName = local.FullName;
            host.Department = dept;
            host.IsActive = true;
        }

        host.Phone = local.Phone;
        host.Email = local.Email;
        return host;
    }

    private static async Task<T> EnsureNamedAsync<T>(
        DbSet<T> set,
        System.Linq.Expressions.Expression<Func<T, bool>> predicate,
        Func<T> factory,
        CancellationToken cancellationToken) where T : class
    {
        var existing = await set.FirstOrDefaultAsync(predicate, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var created = factory();
        set.Add(created);
        return created;
    }

    private async Task EnsureCloudSchemaAsync(AppDbContext cloud, CloudOptions options, CancellationToken cancellationToken)
    {
        var key = $"{options.Server}|{options.Database}";
        lock (ReadySchemas)
        {
            if (ReadySchemas.Contains(key))
            {
                return;
            }
        }

        await SchemaLock.WaitAsync(cancellationToken);
        try
        {
            lock (ReadySchemas)
            {
                if (ReadySchemas.Contains(key))
                {
                    return;
                }
            }

            if (!await CloudHasVisitsTableAsync(cloud, cancellationToken))
            {
                try
                {
                    await cloud.Database.MigrateAsync(cancellationToken);
                }
                catch (Exception migrateEx)
                {
                    _logger.LogWarning(migrateEx, "Cloud MigrateAsync failed; trying CreateTables for {Key}", key);
                    try
                    {
                        var creator = cloud.Database.GetService<IRelationalDatabaseCreator>();
                        await creator.CreateTablesAsync(cancellationToken);
                    }
                    catch (Exception createEx)
                    {
                        _logger.LogWarning(createEx, "Cloud CreateTables failed; trying EnsureCreated for {Key}", key);
                        await cloud.Database.EnsureCreatedAsync(cancellationToken);
                    }
                }

                if (!await CloudHasVisitsTableAsync(cloud, cancellationToken))
                {
                    throw new InvalidOperationException(
                        "สร้างตาราง Visits บน Cloud ไม่สำเร็จ — ให้ login มีสิทธิ์ db_owner หรือสร้าง schema บนฐานข้อมูล Cloud");
                }
            }

            lock (ReadySchemas)
            {
                ReadySchemas.Add(key);
            }
        }
        finally
        {
            SchemaLock.Release();
        }
    }

    private static async Task<bool> CloudHasVisitsTableAsync(AppDbContext cloud, CancellationToken cancellationToken)
    {
        var connection = cloud.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await cloud.Database.OpenConnectionAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CASE WHEN OBJECT_ID(N'[dbo].[Visits]', 'U') IS NULL THEN 0 ELSE 1 END";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null && Convert.ToInt32(result) == 1;
    }

    private AppDbContext CreateCloudContext(CloudOptions options)
    {
        var cs = options.ConnectionString
            ?? throw new InvalidOperationException("Cloud SQL connection string is not configured.");
        var builder = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(cs);
        return new AppDbContext(builder.Options);
    }
}
