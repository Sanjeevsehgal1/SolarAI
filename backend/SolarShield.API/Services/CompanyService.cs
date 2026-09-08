using Microsoft.EntityFrameworkCore;
using SolarShield.Core.DTOs;
using SolarShield.Core.Enums;
using SolarShield.Infrastructure.Data;

namespace SolarShield.API.Services;

public class CompanyService
{
    private readonly ApplicationDbContext _db;
    private readonly AccessControlService _access;

    public CompanyService(ApplicationDbContext db, AccessControlService access)
    {
        _db = db;
        _access = access;
    }

    public async Task<CompanyDashboardDto?> GetDashboardAsync(int userId, UserRole role, int? organizationId)
    {
        if (!_access.IsCompanyAdmin(role) && !_access.IsSuperAdmin(role))
            return null;

        var siteIds = await _access.GetAccessibleSiteIdsAsync(userId, role, organizationId);
        if (siteIds.Count == 0) return null;

        var today = DateTime.UtcNow.Date;
        var genToday = await _db.GenerationRecords
            .Where(g => siteIds.Contains(g.SiteId) && g.RecordDate == today)
            .ToListAsync();

        var actual = genToday.Sum(g => g.ActualKwh);
        var target = genToday.Sum(g => g.TheoreticalKwh);
        var loss = Math.Max(0, target - actual);
        var efficiency = target > 0 ? Math.Round(actual / target * 100, 1) : 0;

        var assets = await _db.Assets.Where(a => siteIds.Contains(a.SiteId) && a.IsActive).ToListAsync();
        var onlineAssets = assets.Count(a => a.Status == AssetStatus.Online);
        var health = assets.Any() ? Math.Round(assets.Average(a => a.HealthScore), 1) : 100;

        var criticalAlerts = await _db.Faults.CountAsync(f =>
            siteIds.Contains(f.SiteId) && f.Severity == FaultSeverity.Critical && f.Status != FaultStatus.Resolved);

        var openMaintenance = await _db.WorkTasks.CountAsync(t =>
            siteIds.Contains(t.SiteId) && t.Status != WorkTaskStatus.Done);

        var aiIssues = await _db.Faults.CountAsync(f =>
            siteIds.Contains(f.SiteId) && f.IsPredicted && f.Status != FaultStatus.Resolved);

        var activeIssues = await _db.Faults.CountAsync(f =>
            siteIds.Contains(f.SiteId) && f.Status != FaultStatus.Resolved);

        var capacity = await _db.SolarSites.Where(s => siteIds.Contains(s.Id)).SumAsync(s => s.CapacityMw);

        return new CompanyDashboardDto(
            siteIds.Count, capacity, onlineAssets, assets.Count,
            criticalAlerts, openMaintenance, actual, target, loss, efficiency, health,
            aiIssues, activeIssues
        );
    }

    public async Task<OperationsEfficiencyDto?> GetOperationsEfficiencyAsync(int userId, UserRole role, int? organizationId)
    {
        if (!_access.IsCompanyAdmin(role) && !_access.IsSuperAdmin(role))
            return null;

        var siteIds = await _access.GetAccessibleSiteIdsAsync(userId, role, organizationId);
        if (siteIds.Count == 0) return null;

        var capacity = await _db.SolarSites.Where(s => siteIds.Contains(s.Id)).SumAsync(s => s.CapacityMw);
        var assets = await _db.Assets.CountAsync(a => siteIds.Contains(a.SiteId) && a.IsActive);

        var activeIssues = await _db.Faults.CountAsync(f =>
            siteIds.Contains(f.SiteId) && f.Status != FaultStatus.Resolved);

        var aiIssues = await _db.Faults.CountAsync(f =>
            siteIds.Contains(f.SiteId) && f.IsPredicted && f.Status != FaultStatus.Resolved);

        var maintenanceTasks = await _db.WorkTasks.CountAsync(t =>
            siteIds.Contains(t.SiteId) && t.Status != WorkTaskStatus.Done);

        var criticalTasks = await _db.WorkTasks.CountAsync(t =>
            siteIds.Contains(t.SiteId) && t.Status != WorkTaskStatus.Done &&
            _db.Faults.Any(f => f.SiteId == t.SiteId && f.Severity == FaultSeverity.Critical));

        var normalTasks = maintenanceTasks - criticalTasks;
        var onlineAssets = await _db.Assets.CountAsync(a =>
            siteIds.Contains(a.SiteId) && a.Status == AssetStatus.Online);
        var coverage = assets > 0 ? Math.Round((double)onlineAssets / assets * 100, 1) : 0;

        return new OperationsEfficiencyDto(
            siteIds.Count, capacity, assets, activeIssues, aiIssues,
            maintenanceTasks, criticalTasks, normalTasks, coverage
        );
    }

    public async Task<EnergyLossDto?> GetEnergyLossAsync(int? siteId, int userId, UserRole role, int? organizationId)
    {
        var site = await _access.ResolveSiteAsync(siteId, userId, role, organizationId);
        if (site == null) return null;

        var today = DateTime.UtcNow.Date;
        var gen = await _db.GenerationRecords
            .Where(g => g.SiteId == site.Id && g.RecordDate == today)
            .FirstOrDefaultAsync();

        var expected = gen?.TheoreticalKwh ?? 0;
        var actual = gen?.ActualKwh ?? 0;
        var loss = Math.Max(0, expected - actual);
        var lossPercent = expected > 0 ? Math.Round(loss / expected * 100, 1) : 0;

        var causes = new List<string>();
        if (await _db.Faults.AnyAsync(f => f.SiteId == site.Id && f.Type == FaultType.InverterError && f.Status != FaultStatus.Resolved))
            causes.Add("Inverter issue");
        if (await _db.Faults.AnyAsync(f => f.SiteId == site.Id && f.Type == FaultType.PanelDegradation && f.Status != FaultStatus.Resolved))
            causes.Add("Panel degradation");
        if (gen?.Temperature > 38) causes.Add("High temperature");
        if (await _db.Faults.AnyAsync(f => f.SiteId == site.Id && f.Type == FaultType.StringMismatch && f.Status != FaultStatus.Resolved))
            causes.Add("Grid / string issue");
        if (await _db.Assets.AnyAsync(a => a.SiteId == site.Id && a.Status == AssetStatus.Offline))
            causes.Add("Equipment downtime");
        if (causes.Count == 0) causes.Add("Normal operational variance");

        return new EnergyLossDto(expected, actual, loss, lossPercent, causes);
    }
}
