using Microsoft.EntityFrameworkCore;
using SolarShield.Core.DTOs;
using SolarShield.Core.Enums;
using SolarShield.Infrastructure.Data;

namespace SolarShield.API.Services;

public class AssetService
{
    private readonly ApplicationDbContext _db;
    private readonly AccessControlService _access;

    public AssetService(ApplicationDbContext db, AccessControlService access)
    {
        _db = db;
        _access = access;
    }

    public async Task<List<AssetDto>> GetAssetsAsync(int? siteId, int userId, UserRole role, int? organizationId)
    {
        var accessibleIds = await _access.GetAccessibleSiteIdsAsync(userId, role, organizationId);
        if (accessibleIds.Count == 0) return [];

        var query = _db.Assets.AsNoTracking()
            .Include(a => a.Site).Include(a => a.Zone)
            .Where(a => a.IsActive && accessibleIds.Contains(a.SiteId));

        if (siteId.HasValue)
        {
            if (!accessibleIds.Contains(siteId.Value)) return [];
            query = query.Where(a => a.SiteId == siteId.Value);
        }

        return await query.OrderBy(a => a.AssetCode)
            .Select(a => new AssetDto(
                a.Id, a.SiteId, a.Site.Name, a.ZoneId, a.Zone != null ? a.Zone.Name : null,
                a.AssetCode, a.Type.ToString(), a.SerialNumber, a.Manufacturer, a.Model,
                a.InstallationDate, a.CapacityKw, a.Location, a.Status.ToString(),
                a.HealthScore, a.LastMaintenanceAt))
            .ToListAsync();
    }

    public async Task<AssetDto?> GetAssetByIdAsync(int id, int userId, UserRole role, int? organizationId)
    {
        var asset = await _db.Assets.AsNoTracking()
            .Include(a => a.Site).Include(a => a.Zone)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (asset == null) return null;
        if (!await _access.CanAccessSiteAsync(userId, role, organizationId, asset.SiteId))
            return null;
        return new AssetDto(
            asset.Id, asset.SiteId, asset.Site.Name, asset.ZoneId, asset.Zone?.Name,
            asset.AssetCode, asset.Type.ToString(), asset.SerialNumber, asset.Manufacturer, asset.Model,
            asset.InstallationDate, asset.CapacityKw, asset.Location, asset.Status.ToString(),
            asset.HealthScore, asset.LastMaintenanceAt);
    }
}
