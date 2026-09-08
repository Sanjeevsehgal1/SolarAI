using Microsoft.EntityFrameworkCore;
using SolarShield.Core.DTOs;
using SolarShield.Core.Enums;
using SolarShield.Infrastructure.Data;

namespace SolarShield.API.Services;

public class CleaningService
{
    private readonly ApplicationDbContext _db;
    private readonly AccessControlService _access;

    public CleaningService(ApplicationDbContext db, AccessControlService access)
    {
        _db = db;
        _access = access;
    }

    public async Task<List<CleaningScheduleDto>> GetSchedulesAsync(int? siteId, int userId, UserRole role, int? organizationId)
    {
        var accessibleIds = (await _access.GetAccessibleSitesAsync(userId, role, organizationId)).Select(s => s.Id).ToList();
        if (accessibleIds.Count == 0) return [];

        if (siteId.HasValue)
        {
            if (!accessibleIds.Contains(siteId.Value)) return [];
            accessibleIds = [siteId.Value];
        }

        var query = _db.CleaningSchedules
            .Include(c => c.Zone)
            .Where(c => accessibleIds.Contains(c.SiteId))
            .AsQueryable();

        var schedules = await query.OrderBy(c => c.Priority).ThenBy(c => c.ScheduledDate).ToListAsync();
        return schedules.Select(c => new CleaningScheduleDto(
            c.Id, c.SiteId, c.ZoneId, c.Zone.Name,
            c.ScheduledDate, c.CompletedDate, c.SoilingIndexAtSchedule,
            c.Priority, c.IsCompleted, c.PhotoProofUrl
        )).ToList();
    }

    public async Task<List<ZoneSoilingDto>> GetSoilingIndexAsync(int siteId, int userId, UserRole role, int? organizationId)
    {
        if (!await _access.CanAccessSiteAsync(userId, role, organizationId, siteId))
            return [];
        var today = DateTime.UtcNow.Date;
        var records = await _db.SoilingRecords
            .Include(s => s.Zone)
            .Where(s => s.SiteId == siteId && s.RecordDate == today)
            .ToListAsync();

        return records.Select(r => new ZoneSoilingDto(
            r.ZoneId,
            r.Zone.Name,
            r.Zone.StringId,
            r.SoilingIndex,
            r.EstimatedLossPercent,
            r.RecordDate,
            r.SoilingIndex > 40 ? "High" : r.SoilingIndex > 20 ? "Medium" : "Low"
        )).OrderByDescending(z => z.SoilingIndex).ToList();
    }

    public async Task<bool> MarkCompletedAsync(int scheduleId, string? photoUrl, string? notes)
    {
        var schedule = await _db.CleaningSchedules.FindAsync(scheduleId);
        if (schedule == null) return false;

        schedule.IsCompleted = true;
        schedule.CompletedDate = DateTime.UtcNow;
        schedule.PhotoProofUrl = photoUrl;
        schedule.Notes = notes;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Basic soiling index calculator based on days since last clean and zone dust factor.
    /// </summary>
    public async Task RecalculateSoilingAsync(int siteId)
    {
        var zones = await _db.Zones.Where(z => z.SiteId == siteId).ToListAsync();
        var today = DateTime.UtcNow.Date;
        var random = new Random(siteId + today.Day);

        foreach (var zone in zones)
        {
            var lastClean = await _db.CleaningSchedules
                .Where(c => c.ZoneId == zone.Id && c.IsCompleted)
                .OrderByDescending(c => c.CompletedDate)
                .Select(c => c.CompletedDate)
                .FirstOrDefaultAsync();

            var daysSinceClean = lastClean.HasValue ? (today - lastClean.Value.Date).Days : 30;
            var baseSoiling = Math.Min(60, daysSinceClean * 1.2 + random.NextDouble() * 5);
            var lossPercent = Math.Round(baseSoiling * 0.35, 2);

            var existing = await _db.SoilingRecords
                .FirstOrDefaultAsync(s => s.SiteId == siteId && s.ZoneId == zone.Id && s.RecordDate == today);

            if (existing != null)
            {
                existing.SoilingIndex = Math.Round(baseSoiling, 1);
                existing.EstimatedLossPercent = lossPercent;
            }
            else
            {
                _db.SoilingRecords.Add(new Core.Entities.SoilingRecord
                {
                    SiteId = siteId,
                    ZoneId = zone.Id,
                    RecordDate = today,
                    SoilingIndex = Math.Round(baseSoiling, 1),
                    EstimatedLossPercent = lossPercent
                });
            }
        }

        await _db.SaveChangesAsync();
    }
}
