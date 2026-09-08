using Microsoft.EntityFrameworkCore;
using SolarShield.Core.Entities;

namespace SolarShield.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<User> Users => Set<User>();
    public DbSet<SolarSite> SolarSites => Set<SolarSite>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Panel> Panels => Set<Panel>();
    public DbSet<Fault> Faults => Set<Fault>();
    public DbSet<FaultComment> FaultComments => Set<FaultComment>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<CleaningSchedule> CleaningSchedules => Set<CleaningSchedule>();
    public DbSet<GenerationRecord> GenerationRecords => Set<GenerationRecord>();
    public DbSet<SoilingRecord> SoilingRecords => Set<SoilingRecord>();
    public DbSet<UserSiteAssignment> UserSiteAssignments => Set<UserSiteAssignment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<ScadaReading> ScadaReadings => Set<ScadaReading>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(e =>
        {
            e.HasIndex(o => o.Code).IsUnique();
            e.Property(o => o.Name).HasMaxLength(200);
            e.Property(o => o.Code).HasMaxLength(50);
        });

        modelBuilder.Entity<Permission>(e =>
        {
            e.HasIndex(p => p.Code).IsUnique();
            e.Property(p => p.Code).HasMaxLength(64);
            e.Property(p => p.Name).HasMaxLength(128);
        });

        modelBuilder.Entity<RolePermission>(e =>
        {
            e.HasIndex(rp => new { rp.Role, rp.PermissionId }).IsUnique();
            e.HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions).HasForeignKey(rp => rp.PermissionId);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(256);
            e.Property(u => u.FullName).HasMaxLength(200);
            e.HasOne(u => u.Organization).WithMany(o => o.Users).HasForeignKey(u => u.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SolarSite>(e =>
        {
            e.HasIndex(s => new { s.OrganizationId, s.PlantCode }).IsUnique();
            e.Property(s => s.PlantCode).HasMaxLength(32);
            e.HasOne(s => s.Organization).WithMany(o => o.Plants).HasForeignKey(s => s.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserSiteAssignment>(e =>
        {
            e.HasKey(x => new { x.UserId, x.SiteId });
            e.HasOne(x => x.User).WithMany(u => u.SiteAssignments).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Site).WithMany(s => s.UserAssignments).HasForeignKey(x => x.SiteId);
        });

        modelBuilder.Entity<Fault>(e =>
        {
            e.HasOne(f => f.Site).WithMany(s => s.Faults).HasForeignKey(f => f.SiteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(f => f.Zone).WithMany().HasForeignKey(f => f.ZoneId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(f => f.AssignedTo).WithMany(u => u.AssignedFaults).HasForeignKey(f => f.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkTask>(e =>
        {
            e.HasOne(t => t.Site).WithMany(s => s.Tasks).HasForeignKey(t => t.SiteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Zone).WithMany().HasForeignKey(t => t.ZoneId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.AssignedTo).WithMany(u => u.AssignedTasks).HasForeignKey(t => t.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.CreatedBy).WithMany().HasForeignKey(t => t.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Zone>(e =>
        {
            e.HasOne(z => z.Site).WithMany(s => s.Zones).HasForeignKey(z => z.SiteId);
        });

        modelBuilder.Entity<Panel>(e =>
        {
            e.HasOne(p => p.Zone).WithMany(z => z.Panels).HasForeignKey(p => p.ZoneId);
        });

        modelBuilder.Entity<CleaningSchedule>(e =>
        {
            e.HasOne(c => c.Site).WithMany(s => s.CleaningSchedules).HasForeignKey(c => c.SiteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Zone).WithMany(z => z.CleaningSchedules).HasForeignKey(c => c.ZoneId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SoilingRecord>(e =>
        {
            e.HasOne(s => s.Site).WithMany(site => site.SoilingRecords).HasForeignKey(s => s.SiteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Zone).WithMany(z => z.SoilingRecords).HasForeignKey(s => s.ZoneId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GenerationRecord>(e =>
        {
            e.HasOne(g => g.Site).WithMany(s => s.GenerationRecords).HasForeignKey(g => g.SiteId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Asset>(e =>
        {
            e.HasIndex(a => new { a.SiteId, a.AssetCode }).IsUnique();
            e.Property(a => a.AssetCode).HasMaxLength(32);
            e.HasOne(a => a.Site).WithMany(s => s.Assets).HasForeignKey(a => a.SiteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Zone).WithMany().HasForeignKey(a => a.ZoneId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ScadaReading>(e =>
        {
            e.HasOne(r => r.Site).WithMany(s => s.ScadaReadings).HasForeignKey(r => r.SiteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Asset).WithMany(a => a.ScadaReadings).HasForeignKey(r => r.AssetId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasIndex(i => i.InvoiceNumber).IsUnique();
            e.Property(i => i.InvoiceNumber).HasMaxLength(32);
            e.HasOne(i => i.Organization).WithMany(o => o.Invoices).HasForeignKey(i => i.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
