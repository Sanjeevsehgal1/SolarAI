using Microsoft.EntityFrameworkCore;
using SolarShield.Core.DTOs;
using SolarShield.Core.Enums;
using SolarShield.Infrastructure.Data;

namespace SolarShield.API.Services;

public class ReportsService
{
    private readonly ApplicationDbContext _db;
    private readonly AccessControlService _access;

    public ReportsService(ApplicationDbContext db, AccessControlService access)
    {
        _db = db;
        _access = access;
    }

    public async Task<List<ReportSummaryDto>> GetReportsAsync(int? siteId, int userId, UserRole role, int? organizationId)
    {
        var site = await _access.ResolveSiteAsync(siteId, userId, role, organizationId);
        if (site == null) return [];

        var today = DateTime.UtcNow.Date;
        var periodStart = today.AddDays(-30);

        var generation = await _db.GenerationRecords
            .Where(g => g.SiteId == site.Id && g.RecordDate >= periodStart)
            .OrderBy(g => g.RecordDate)
            .Select(g => new { g.RecordDate, g.ActualKwh, g.TheoreticalKwh, g.PerformanceRatio })
            .ToListAsync();

        var faults = await _db.Faults
            .Where(f => f.SiteId == site.Id && f.DetectedAt >= periodStart)
            .OrderByDescending(f => f.DetectedAt)
            .Select(f => new { f.Title, Severity = f.Severity.ToString(), Status = f.Status.ToString(), f.DetectedAt, f.IsPredicted })
            .Take(20)
            .ToListAsync();

        var tasks = await _db.WorkTasks
            .Where(t => t.SiteId == site.Id)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        var assets = await _db.Assets
            .Where(a => a.SiteId == site.Id && a.IsActive)
            .Select(a => new { a.AssetCode, Type = a.Type.ToString(), a.HealthScore, Status = a.Status.ToString() })
            .ToListAsync();

        var now = DateTime.UtcNow;
        return
        [
            new ReportSummaryDto("plant-performance", "Plant Performance Report", "Last 30 days", now,
                new { siteName = site.Name, generation, avgPr = generation.Any() ? generation.Average(g => g.PerformanceRatio) : 0 }),
            new ReportSummaryDto("energy-generation", "Energy Generation Report", "Last 30 days", now,
                new { totalActual = generation.Sum(g => g.ActualKwh), totalTheoretical = generation.Sum(g => g.TheoreticalKwh), daily = generation }),
            new ReportSummaryDto("energy-loss", "Energy Loss Report", "Today", now,
                new { expected = generation.LastOrDefault()?.TheoreticalKwh ?? 0, actual = generation.LastOrDefault()?.ActualKwh ?? 0 }),
            new ReportSummaryDto("equipment-health", "Equipment Health Report", "Current", now, assets),
            new ReportSummaryDto("fault-history", "Fault History Report", "Last 30 days", now, faults),
            new ReportSummaryDto("maintenance", "Maintenance Report", "Current", now, tasks),
            new ReportSummaryDto("ai-predictions", "AI Prediction Report", "Last 30 days", now,
                faults.Where(f => f.IsPredicted).ToList()),
            new ReportSummaryDto("downtime", "Downtime Report", "Current", now,
                assets.Where(a => a.Status != "Online").ToList())
        ];
    }
}
