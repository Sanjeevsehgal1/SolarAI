using Microsoft.EntityFrameworkCore;
using SolarShield.Core.DTOs;
using SolarShield.Core.Enums;
using SolarShield.Infrastructure.Data;

namespace SolarShield.API.Services;

public class ScadaService
{
    private readonly ApplicationDbContext _db;
    private readonly AccessControlService _access;

    public ScadaService(ApplicationDbContext db, AccessControlService access)
    {
        _db = db;
        _access = access;
    }

    public async Task<ScadaSnapshotDto?> GetSnapshotAsync(int siteId, int userId, UserRole role, int? organizationId)
    {
        if (!await _access.CanAccessSiteAsync(userId, role, organizationId, siteId))
            return null;

        var site = await _db.SolarSites.AsNoTracking().FirstOrDefaultAsync(s => s.Id == siteId);
        if (site == null) return null;

        var latest = await _db.ScadaReadings.AsNoTracking()
            .Where(r => r.SiteId == siteId && r.AssetId == null)
            .OrderByDescending(r => r.RecordedAt)
            .FirstOrDefaultAsync();

        var assetReadings = await _db.ScadaReadings.AsNoTracking()
            .Include(r => r.Asset)
            .Where(r => r.SiteId == siteId && r.AssetId != null)
            .ToListAsync();

        var latestByAsset = assetReadings
            .GroupBy(r => r.AssetId)
            .Select(g => g.OrderByDescending(r => r.RecordedAt).First())
            .ToList();

        var assets = latestByAsset.Select(r => new AssetScadaDto(
            r.AssetId!.Value,
            r.Asset!.AssetCode,
            r.Asset.Type.ToString(),
            r.PowerKw,
            r.Temperature,
            r.Asset.Status.ToString(),
            r.Asset.HealthScore
        )).ToList();

        if (latest == null)
        {
            return new ScadaSnapshotDto(
                siteId, site.Name, 0, 0, 0, 0, 0, "Unknown", "Disconnected", 0,
                DateTime.UtcNow, assets
            );
        }

        return new ScadaSnapshotDto(
            siteId, site.Name, latest.Voltage, latest.Current, latest.PowerKw,
            latest.Temperature, latest.Frequency, latest.InverterStatus,
            latest.CommunicationStatus, latest.EnergyGenerationKwh,
            latest.RecordedAt, assets
        );
    }
}
