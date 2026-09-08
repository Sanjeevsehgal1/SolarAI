using Microsoft.EntityFrameworkCore;
using SolarShield.Core.DTOs;
using SolarShield.Core.Entities;
using SolarShield.Core.Enums;
using SolarShield.Infrastructure.Data;

namespace SolarShield.API.Services;

public class FaultService
{
    private readonly ApplicationDbContext _db;
    private readonly AccessControlService _access;

    public FaultService(ApplicationDbContext db, AccessControlService access)
    {
        _db = db;
        _access = access;
    }

    public async Task<List<FaultDto>> GetFaultsAsync(int? siteId, string? status, string? severity, int userId, UserRole role, int? organizationId)
    {
        var accessibleIds = await _access.GetAccessibleSiteIdsAsync(userId, role, organizationId);
        if (accessibleIds.Count == 0) return [];

        var query = _db.Faults.AsNoTracking().AsQueryable();

        if (siteId.HasValue)
        {
            if (!accessibleIds.Contains(siteId.Value)) return [];
            query = query.Where(f => f.SiteId == siteId.Value);
        }
        else
        {
            query = query.Where(f => accessibleIds.Contains(f.SiteId));
        }

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<FaultStatus>(status, true, out var st))
            query = query.Where(f => f.Status == st);
        if (!string.IsNullOrEmpty(severity) && Enum.TryParse<FaultSeverity>(severity, true, out var sev))
            query = query.Where(f => f.Severity == sev);

        return await query
            .OrderByDescending(f => f.DetectedAt)
            .Select(f => new FaultDto(
                f.Id, f.SiteId, f.Site.Name, f.ZoneId, f.Zone != null ? f.Zone.Name : null,
                f.Type.ToString(), f.Severity.ToString(), f.Status.ToString(),
                f.Title, f.Description, f.IsPredicted, f.ConfidenceScore,
                f.AssignedTo != null ? f.AssignedTo.FullName : null, f.AssignedToUserId,
                f.FixNotes, f.PhotoUrl, f.DetectedAt, f.ResolvedAt))
            .ToListAsync();
    }

    public async Task<FaultDto?> GetFaultByIdAsync(int id, int userId, UserRole role, int? organizationId)
    {
        var fault = await _db.Faults
            .Include(f => f.Site).Include(f => f.Zone).Include(f => f.AssignedTo)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (fault == null) return null;
        if (!await _access.CanAccessSiteAsync(userId, role, organizationId, fault.SiteId))
            return null;
        return MapFault(fault);
    }

    public async Task<FaultDto> CreateFaultAsync(CreateFaultRequest request)
    {
        var fault = new Fault
        {
            SiteId = request.SiteId,
            ZoneId = request.ZoneId,
            Type = Enum.Parse<FaultType>(request.Type, true),
            Severity = Enum.Parse<FaultSeverity>(request.Severity, true),
            Title = request.Title,
            Description = request.Description,
            IsPredicted = request.IsPredicted,
            ConfidenceScore = request.ConfidenceScore
        };
        _db.Faults.Add(fault);
        await _db.SaveChangesAsync();
        return MapFault(await _db.Faults.Include(f => f.Site).Include(f => f.Zone).Include(f => f.AssignedTo).FirstAsync(f => f.Id == fault.Id));
    }

    public async Task<FaultDto?> UpdateFaultAsync(int id, UpdateFaultRequest request)
    {
        var fault = await _db.Faults.FindAsync(id);
        if (fault == null) return null;

        if (!string.IsNullOrEmpty(request.Status))
            fault.Status = Enum.Parse<FaultStatus>(request.Status, true);
        if (request.AssignedToUserId.HasValue)
            fault.AssignedToUserId = request.AssignedToUserId;
        if (request.FixNotes != null) fault.FixNotes = request.FixNotes;
        if (request.PhotoUrl != null) fault.PhotoUrl = request.PhotoUrl;
        if (fault.Status == FaultStatus.Resolved && !fault.ResolvedAt.HasValue)
            fault.ResolvedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapFault(await _db.Faults.Include(f => f.Site).Include(f => f.Zone).Include(f => f.AssignedTo).FirstAsync(f => f.Id == id));
    }

    public async Task<bool> AddCommentAsync(int faultId, int userId, string comment)
    {
        var fault = await _db.Faults.FindAsync(faultId);
        if (fault == null) return false;

        _db.FaultComments.Add(new FaultComment { FaultId = faultId, UserId = userId, Comment = comment });
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Rule-based fault prediction using PR drop and soiling thresholds.
    /// </summary>
    public async Task<List<FaultDto>> RunPredictionAsync(int siteId)
    {
        var newFaults = new List<Fault>();
        var today = DateTime.UtcNow.Date;

        var recentGen = await _db.GenerationRecords
            .Where(g => g.SiteId == siteId)
            .OrderByDescending(g => g.RecordDate)
            .Take(3)
            .ToListAsync();

        if (recentGen.Count >= 2)
        {
            var prDrop = recentGen[0].PerformanceRatio - recentGen[^1].PerformanceRatio;
            if (prDrop < -5)
            {
                newFaults.Add(new Fault
                {
                    SiteId = siteId,
                    Type = FaultType.TemperatureAnomaly,
                    Severity = FaultSeverity.Medium,
                    Title = "Performance Ratio Drop Detected",
                    Description = $"PR dropped {Math.Abs(prDrop):F1}% over last 3 days. Possible temperature anomaly or degradation.",
                    IsPredicted = true,
                    ConfidenceScore = Math.Min(95, 60 + Math.Abs(prDrop) * 3)
                });
            }
        }

        var soilingRecords = await _db.SoilingRecords
            .Include(s => s.Zone)
            .Where(s => s.SiteId == siteId && s.RecordDate == today)
            .ToListAsync();

        foreach (var record in soilingRecords.Where(s => s.SoilingIndex > 35))
        {
            var exists = await _db.Faults.AnyAsync(f =>
                f.SiteId == siteId && f.ZoneId == record.ZoneId &&
                f.Type == FaultType.SoilingLoss && f.Status != FaultStatus.Resolved);

            if (!exists)
            {
                newFaults.Add(new Fault
                {
                    SiteId = siteId,
                    ZoneId = record.ZoneId,
                    Type = FaultType.SoilingLoss,
                    Severity = record.SoilingIndex > 50 ? FaultSeverity.Critical : FaultSeverity.Medium,
                    Title = $"High Soiling Predicted - {record.Zone.Name}",
                    Description = $"Soiling index at {record.SoilingIndex:F1}%. Estimated loss: {record.EstimatedLossPercent:F1}%. Schedule cleaning.",
                    IsPredicted = true,
                    ConfidenceScore = Math.Min(98, 70 + record.SoilingIndex * 0.5)
                });
            }
        }

        if (newFaults.Any())
        {
            _db.Faults.AddRange(newFaults);
            await _db.SaveChangesAsync();
        }

        return newFaults.Select(f => MapFault(f)).ToList();
    }

    private static FaultDto MapFault(Fault f) => new(
        f.Id, f.SiteId, f.Site?.Name ?? "", f.ZoneId, f.Zone?.Name,
        f.Type.ToString(), f.Severity.ToString(), f.Status.ToString(),
        f.Title, f.Description, f.IsPredicted, f.ConfidenceScore,
        f.AssignedTo?.FullName, f.AssignedToUserId, f.FixNotes, f.PhotoUrl,
        f.DetectedAt, f.ResolvedAt
    );
}
