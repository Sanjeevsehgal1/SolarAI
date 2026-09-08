using Microsoft.EntityFrameworkCore;
using SolarShield.Core.DTOs;
using SolarShield.Core.Enums;
using SolarShield.Infrastructure.Data;

namespace SolarShield.API.Services;

public class AnalyticsService
{
    private readonly ApplicationDbContext _db;
    private readonly AccessControlService _access;

    public AnalyticsService(ApplicationDbContext db, AccessControlService access)
    {
        _db = db;
        _access = access;
    }

    public async Task<AnalyticsSummaryDto?> GetSummaryAsync(int siteId, int userId, UserRole role, int? organizationId, int days = 30)
    {
        if (!await _access.CanAccessSiteAsync(userId, role, organizationId, siteId))
            return null;

        var site = await _db.SolarSites.FindAsync(siteId);
        if (site == null) return null;

        days = Math.Clamp(days, 7, 90);
        var periodStart = DateTime.UtcNow.Date.AddDays(-(days - 1));

        var generation = await _db.GenerationRecords
            .Where(g => g.SiteId == siteId && g.RecordDate >= periodStart)
            .OrderBy(g => g.RecordDate)
            .Select(g => new GenerationAnalyticsDto(
                g.RecordDate, g.ActualKwh, g.TheoreticalKwh, g.PerformanceRatio,
                g.Irradiance, g.Temperature))
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var soiling = await _db.SoilingRecords
            .Include(s => s.Zone)
            .Where(s => s.SiteId == siteId && s.RecordDate == today)
            .Select(s => new ZoneSoilingDto(
                s.ZoneId, s.Zone.Name, s.Zone.StringId,
                s.SoilingIndex, s.EstimatedLossPercent, s.RecordDate,
                s.SoilingIndex > 40 ? "High" : s.SoilingIndex > 20 ? "Medium" : "Low"
            ))
            .ToListAsync();

        var weekStart = DateTime.UtcNow.Date.AddDays(-7);
        var openFaultCount = await _db.Faults.CountAsync(f => f.SiteId == siteId && f.Status != FaultStatus.Resolved);
        var openFaults = await _db.Faults
            .Include(f => f.Zone)
            .Where(f => f.SiteId == siteId && f.Status != FaultStatus.Resolved)
            .OrderByDescending(f => f.Severity)
            .ThenByDescending(f => f.DetectedAt)
            .Take(5)
            .Select(f => new FaultSummaryDto(
                f.Id, f.Title, f.Severity.ToString(), f.Status.ToString(),
                f.Zone != null ? f.Zone.Name : null, f.DetectedAt))
            .ToListAsync();

        var tasksDone = await _db.WorkTasks.CountAsync(t => t.SiteId == siteId && t.Status == WorkTaskStatus.Done && t.CompletedAt >= weekStart);
        var tasksPending = await _db.WorkTasks.CountAsync(t => t.SiteId == siteId && t.Status != WorkTaskStatus.Done);

        var last7 = generation.Count >= 7 ? generation.TakeLast(7) : generation;
        var avgPr = last7.Any() ? Math.Round(last7.Average(g => g.PerformanceRatio), 2) : 0;
        var totalActual = generation.Any() ? Math.Round(generation.Sum(g => g.ActualKwh), 2) : 0;
        var totalTheoretical = generation.Any() ? Math.Round(generation.Sum(g => g.TheoreticalKwh), 2) : 0;
        var avgSoiling = soiling.Any() ? Math.Round(soiling.Average(s => s.SoilingIndex), 2) : 0;

        return new AnalyticsSummaryDto(
            generation, soiling, openFaultCount, tasksDone, tasksPending, avgPr,
            totalActual, totalTheoretical, avgSoiling, openFaults);
    }
}
