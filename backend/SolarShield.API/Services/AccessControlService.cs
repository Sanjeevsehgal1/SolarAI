using Microsoft.EntityFrameworkCore;
using SolarShield.Core.Constants;
using SolarShield.Core.DTOs;
using SolarShield.Core.Entities;
using SolarShield.Core.Enums;
using SolarShield.Infrastructure.Data;

namespace SolarShield.API.Services;

public class AccessControlService
{
    private readonly ApplicationDbContext _db;

    private static readonly Dictionary<UserRole, HashSet<string>> RolePermissions = BuildRolePermissions();

    public AccessControlService(ApplicationDbContext db) => _db = db;

    public UserRole ParseRole(string role) =>
        Enum.TryParse<UserRole>(role, true, out var r) ? r : UserRole.Viewer;

    public UserRole NormalizeRole(UserRole role) =>
        role == UserRole.Admin ? UserRole.CompanyAdmin : role;

    public bool IsSuperAdmin(UserRole role) => NormalizeRole(role) == UserRole.SuperAdmin;

    public bool IsCompanyAdmin(UserRole role)
    {
        var r = NormalizeRole(role);
        return r is UserRole.SuperAdmin or UserRole.CompanyAdmin or UserRole.CompanyTechnicalAdmin;
    }

    public bool HasPermission(UserRole role, string permissionCode)
    {
        var normalized = NormalizeRole(role);
        return RolePermissions.TryGetValue(normalized, out var perms) && perms.Contains(permissionCode);
    }

    public List<string> GetPermissions(UserRole role) =>
        RolePermissions.TryGetValue(NormalizeRole(role), out var perms) ? perms.OrderBy(p => p).ToList() : [];

    public async Task<List<int>> GetAccessibleSiteIdsAsync(int userId, UserRole role, int? organizationId)
    {
        var normalized = NormalizeRole(role);
        var query = _db.SolarSites.AsNoTracking().Where(s => s.IsActive);

        if (normalized == UserRole.SuperAdmin)
            return await query.Select(s => s.Id).ToListAsync();

        if (organizationId.HasValue)
            query = query.Where(s => s.OrganizationId == organizationId.Value);

        if (normalized is UserRole.CompanyAdmin or UserRole.CompanyTechnicalAdmin)
            return await query.Select(s => s.Id).ToListAsync();

        var assignedSiteIds = await _db.UserSiteAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => a.SiteId)
            .ToListAsync();

        return await query.Where(s => assignedSiteIds.Contains(s.Id)).Select(s => s.Id).ToListAsync();
    }

    public async Task<List<SiteDto>> GetAccessibleSitesAsync(int userId, UserRole role, int? organizationId)
    {
        var normalized = NormalizeRole(role);
        var query = _db.SolarSites.AsNoTracking().Include(s => s.Organization).Where(s => s.IsActive);

        if (normalized == UserRole.SuperAdmin)
            return await MapSites(query);

        if (organizationId.HasValue)
            query = query.Where(s => s.OrganizationId == organizationId.Value);

        if (normalized is UserRole.CompanyAdmin or UserRole.CompanyTechnicalAdmin)
            return await MapSites(query);

        var assignedSiteIds = await _db.UserSiteAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => a.SiteId)
            .ToListAsync();

        return await MapSites(query.Where(s => assignedSiteIds.Contains(s.Id)));
    }

    public async Task<bool> CanAccessSiteAsync(int userId, UserRole role, int? organizationId, int siteId)
    {
        var ids = await GetAccessibleSiteIdsAsync(userId, role, organizationId);
        return ids.Contains(siteId);
    }

    public async Task<SolarSite?> ResolveSiteAsync(int? siteId, int userId, UserRole role, int? organizationId)
    {
        if (!siteId.HasValue)
        {
            var accessible = await GetAccessibleSitesAsync(userId, role, organizationId);
            var first = accessible.FirstOrDefault();
            if (first == null) return null;
            siteId = first.Id;
        }

        if (!await CanAccessSiteAsync(userId, role, organizationId, siteId.Value))
            return null;

        return await _db.SolarSites
            .Include(s => s.Organization)
            .FirstOrDefaultAsync(s => s.Id == siteId.Value && s.IsActive);
    }

    public IQueryable<Organization> ScopeOrganizations(UserRole role, int? organizationId)
    {
        var query = _db.Organizations.AsQueryable();
        var normalized = NormalizeRole(role);
        if (normalized == UserRole.SuperAdmin) return query;
        if (organizationId.HasValue) return query.Where(o => o.Id == organizationId.Value);
        return query.Where(_ => false);
    }

    public IQueryable<User> ScopeUsers(UserRole role, int? organizationId)
    {
        var query = _db.Users.AsQueryable();
        var normalized = NormalizeRole(role);
        if (normalized == UserRole.SuperAdmin) return query;
        if (normalized is UserRole.CompanyAdmin or UserRole.CompanyTechnicalAdmin or UserRole.PlantAdmin)
        {
            if (!organizationId.HasValue) return query.Where(_ => false);
            return query.Where(u => u.OrganizationId == organizationId.Value && u.Role != UserRole.SuperAdmin);
        }
        return query.Where(u => u.Id == 0);
    }

    private static async Task<List<SiteDto>> MapSites(IQueryable<SolarSite> query) =>
        await query.OrderBy(s => s.Name).Select(s => new SiteDto(
            s.Id, s.Name, s.Location, s.CapacityMw, s.Latitude, s.Longitude, s.TargetPr,
            s.OrganizationId ?? 0, s.PlantCode, s.Organization != null ? s.Organization.Name : null)).ToListAsync();

    private static Dictionary<UserRole, HashSet<string>> BuildRolePermissions()
    {
        var all = new HashSet<string>
        {
            PermissionCodes.OrganizationView, PermissionCodes.OrganizationCreate, PermissionCodes.OrganizationEdit,
            PermissionCodes.PlantCreate, PermissionCodes.PlantEdit, PermissionCodes.PlantView,
            PermissionCodes.UserCreate, PermissionCodes.UserEdit, PermissionCodes.UserView,
            PermissionCodes.AlertView, PermissionCodes.AlertAcknowledge,
            PermissionCodes.MaintenanceCreate, PermissionCodes.MaintenanceAssign, PermissionCodes.MaintenanceComplete,
            PermissionCodes.ReportView, PermissionCodes.ReportExport,
            PermissionCodes.ScadaView, PermissionCodes.AiPredictionView, PermissionCodes.PlatformAdmin
        };

        var companyAdmin = new HashSet<string>
        {
            PermissionCodes.OrganizationView, PermissionCodes.PlantCreate, PermissionCodes.PlantEdit, PermissionCodes.PlantView,
            PermissionCodes.UserCreate, PermissionCodes.UserEdit, PermissionCodes.UserView,
            PermissionCodes.AlertView, PermissionCodes.AlertAcknowledge,
            PermissionCodes.MaintenanceCreate, PermissionCodes.MaintenanceAssign, PermissionCodes.MaintenanceComplete,
            PermissionCodes.ReportView, PermissionCodes.ReportExport, PermissionCodes.ScadaView, PermissionCodes.AiPredictionView
        };

        var companyTech = new HashSet<string>(companyAdmin);
        companyTech.Remove(PermissionCodes.PlantCreate);
        companyTech.Remove(PermissionCodes.UserCreate);

        var plantAdmin = new HashSet<string>
        {
            PermissionCodes.PlantView, PermissionCodes.UserCreate, PermissionCodes.UserEdit, PermissionCodes.UserView,
            PermissionCodes.AlertView, PermissionCodes.AlertAcknowledge,
            PermissionCodes.MaintenanceCreate, PermissionCodes.MaintenanceAssign, PermissionCodes.MaintenanceComplete,
            PermissionCodes.ReportView, PermissionCodes.ReportExport, PermissionCodes.ScadaView, PermissionCodes.AiPredictionView
        };

        var plantLead = new HashSet<string>
        {
            PermissionCodes.PlantView, PermissionCodes.AlertView, PermissionCodes.AlertAcknowledge,
            PermissionCodes.MaintenanceCreate, PermissionCodes.MaintenanceAssign, PermissionCodes.MaintenanceComplete,
            PermissionCodes.ReportView, PermissionCodes.ScadaView, PermissionCodes.AiPredictionView
        };

        var engineer = new HashSet<string>
        {
            PermissionCodes.PlantView, PermissionCodes.AlertView, PermissionCodes.AlertAcknowledge,
            PermissionCodes.MaintenanceComplete, PermissionCodes.ReportView, PermissionCodes.ScadaView, PermissionCodes.AiPredictionView
        };

        var technician = new HashSet<string>
        {
            PermissionCodes.PlantView, PermissionCodes.AlertView,
            PermissionCodes.MaintenanceComplete, PermissionCodes.AiPredictionView
        };

        var supervisor = new HashSet<string>
        {
            PermissionCodes.PlantView, PermissionCodes.AlertView, PermissionCodes.AlertAcknowledge,
            PermissionCodes.MaintenanceAssign, PermissionCodes.MaintenanceComplete,
            PermissionCodes.ReportView, PermissionCodes.AiPredictionView
        };

        var viewer = new HashSet<string>
        {
            PermissionCodes.PlantView, PermissionCodes.AlertView, PermissionCodes.ReportView, PermissionCodes.AiPredictionView
        };

        return new Dictionary<UserRole, HashSet<string>>
        {
            [UserRole.SuperAdmin] = all,
            [UserRole.CompanyAdmin] = companyAdmin,
            [UserRole.CompanyTechnicalAdmin] = companyTech,
            [UserRole.PlantAdmin] = plantAdmin,
            [UserRole.PlantTechnicalLead] = plantLead,
            [UserRole.Engineer] = engineer,
            [UserRole.Technician] = technician,
            [UserRole.Supervisor] = supervisor,
            [UserRole.Viewer] = viewer,
            [UserRole.Admin] = companyAdmin
        };
    }
}
