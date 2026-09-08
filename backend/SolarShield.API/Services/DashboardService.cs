using Microsoft.EntityFrameworkCore;
using SolarShield.Core.DTOs;
using SolarShield.Core.Enums;
using SolarShield.Infrastructure.Data;

namespace SolarShield.API.Services;

public class DashboardService
{
    private readonly ApplicationDbContext _db;
    private readonly AccessControlService _access;

    public DashboardService(ApplicationDbContext db, AccessControlService access)
    {
        _db = db;
        _access = access;
    }

    public async Task<DashboardSummaryDto?> GetSummaryAsync(int? siteId, int userId, UserRole role, int? organizationId)
    {
        var site = await _access.ResolveSiteAsync(siteId, userId, role, organizationId);
        if (site == null) return null;

        var today = DateTime.UtcNow.Date;
        var genToday = await _db.GenerationRecords
            .Where(g => g.SiteId == site.Id && g.RecordDate == today)
            .FirstOrDefaultAsync();

        var latestSoiling = await _db.SoilingRecords
            .Where(s => s.SiteId == site.Id)
            .OrderByDescending(s => s.RecordDate)
            .ToListAsync();

        var avgSoiling = latestSoiling.Any() ? latestSoiling.Average(s => s.SoilingIndex) : 0;

        var faultsToday = await _db.Faults
            .CountAsync(f => f.SiteId == site.Id && f.DetectedAt.Date == today && f.Status != FaultStatus.Resolved);

        var criticalFaults = await _db.Faults
            .CountAsync(f => f.SiteId == site.Id && f.Severity == FaultSeverity.Critical && f.Status != FaultStatus.Resolved);

        var pendingTasks = await _db.WorkTasks
            .CountAsync(t => t.SiteId == site.Id && t.Status != WorkTaskStatus.Done);

        var assets = await _db.Assets.Where(a => a.SiteId == site.Id && a.IsActive).ToListAsync();
        var onlineAssets = assets.Count(a => a.Status == Core.Enums.AssetStatus.Online);
        var equipmentHealth = assets.Any() ? assets.Average(a => a.HealthScore) : 100;

        var actual = genToday?.ActualKwh ?? 0;
        var target = genToday?.TheoreticalKwh ?? 0;
        var lossKwh = Math.Max(0, target - actual);
        var lossPercent = target > 0 ? Math.Round(lossKwh / target * 100, 1) : 0;

        return new DashboardSummaryDto(
            faultsToday,
            criticalFaults,
            pendingTasks,
            genToday?.PerformanceRatio ?? 0,
            Math.Round(avgSoiling, 1),
            actual,
            target,
            site.Name,
            site.Id,
            Math.Round(lossKwh, 2),
            lossPercent,
            onlineAssets,
            assets.Count,
            Math.Round(equipmentHealth, 1)
        );
    }
}
