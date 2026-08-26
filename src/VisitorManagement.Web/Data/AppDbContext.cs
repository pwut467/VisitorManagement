using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Models;

namespace VisitorManagement.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Gate> Gates => Set<Gate>();
    public DbSet<VisitorType> VisitorTypes => Set<VisitorType>();
    public DbSet<VisitPurpose> VisitPurposes => Set<VisitPurpose>();
    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<VisitItem> VisitItems => Set<VisitItem>();
    public DbSet<BlacklistEntry> BlacklistEntries => Set<BlacklistEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Visitor>(e =>
        {
            e.HasIndex(x => x.NationalId).IsUnique();
            e.HasIndex(x => new { x.LastName, x.FirstName });
        });

        builder.Entity<Visit>(e =>
        {
            e.HasIndex(x => x.VisitNumber).IsUnique();
            e.HasIndex(x => x.VisitCode).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.CheckInAt);
            e.HasOne(x => x.RegisteredByUser)
                .WithMany()
                .HasForeignKey(x => x.RegisteredByUserId)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.CheckedOutByUser)
                .WithMany()
                .HasForeignKey(x => x.CheckedOutByUserId)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.HostEmployee)
                .WithMany()
                .HasForeignKey(x => x.HostEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Employee>(e =>
        {
            e.HasIndex(x => x.EmployeeCode).IsUnique();
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Department>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<BlacklistEntry>().HasIndex(x => x.NationalId);
    }
}
