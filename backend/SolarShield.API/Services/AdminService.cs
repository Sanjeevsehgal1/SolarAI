using Microsoft.EntityFrameworkCore;
using SolarShield.Core.Constants;
using SolarShield.Core.DTOs;
using SolarShield.Core.Entities;
using SolarShield.Core.Enums;
using SolarShield.Infrastructure.Data;

namespace SolarShield.API.Services;

public class AdminService
{
    private readonly ApplicationDbContext _db;
    private readonly AccessControlService _access;

    public AdminService(ApplicationDbContext db, AccessControlService access)
    {
        _db = db;
        _access = access;
    }

    public async Task<List<AdminUserDto>> GetUsersAsync(UserRole role, int? organizationId, int? filterOrgId)
    {
        if (!_access.HasPermission(role, PermissionCodes.UserView))
            return [];

        var query = _access.ScopeUsers(role, organizationId);
        if (filterOrgId.HasValue && _access.IsSuperAdmin(role))
            query = query.Where(u => u.OrganizationId == filterOrgId.Value);

        var users = await query
            .Include(u => u.Organization)
            .Include(u => u.SiteAssignments).ThenInclude(a => a.Site).ThenInclude(s => s.Organization)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        return users.Select(MapUser).ToList();
    }

    public async Task<AdminUserDto?> CreateUserAsync(CreateUserRequest request, UserRole actorRole, int? actorOrgId, int actorUserId)
    {
        if (!_access.HasPermission(actorRole, PermissionCodes.UserCreate))
            return null;

        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return null;

        var role = Enum.Parse<UserRole>(request.Role, true);
        if (role == UserRole.SuperAdmin && !_access.IsSuperAdmin(actorRole))
            return null;

        var orgId = request.OrganizationId ?? actorOrgId;
        if (!_access.IsSuperAdmin(actorRole) && orgId != actorOrgId)
            return null;

        if (role != UserRole.SuperAdmin && !orgId.HasValue)
            return null;

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            OrganizationId = role == UserRole.SuperAdmin ? null : orgId
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await AssignSitesAsync(user.Id, request.SiteIds, actorUserId, actorRole, actorOrgId);
        return await GetUserByIdAsync(user.Id, actorRole, actorOrgId);
    }

    public async Task<AdminUserDto?> UpdateUserAsync(int id, UpdateUserRequest request, UserRole actorRole, int? actorOrgId, int actorUserId)
    {
        if (!_access.HasPermission(actorRole, PermissionCodes.UserEdit))
            return null;

        var user = await _access.ScopeUsers(actorRole, actorOrgId).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return null;

        if (request.FullName != null) user.FullName = request.FullName;
        if (request.Phone != null) user.Phone = request.Phone;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
        if (request.Role != null)
        {
            var newRole = Enum.Parse<UserRole>(request.Role, true);
            if (newRole == UserRole.SuperAdmin && !_access.IsSuperAdmin(actorRole))
                return null;
            user.Role = newRole;
        }

        await _db.SaveChangesAsync();

        if (request.SiteIds != null)
            await AssignSitesAsync(user.Id, request.SiteIds, actorUserId, actorRole, actorOrgId);

        return await GetUserByIdAsync(id, actorRole, actorOrgId);
    }

    public async Task<SiteDto?> CreatePlantAsync(CreatePlantRequest request, UserRole role, int? organizationId)
    {
        if (!_access.HasPermission(role, PermissionCodes.PlantCreate))
            return null;

        if (!_access.IsSuperAdmin(role) && request.OrganizationId != organizationId)
            return null;

        var org = await _access.ScopeOrganizations(role, organizationId)
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId && o.IsActive);
        if (org == null) return null;

        var plantCount = await _db.SolarSites.CountAsync(s => s.OrganizationId == org.Id && s.IsActive);
        if (plantCount >= org.MaxPlants) return null;

        if (await _db.SolarSites.AnyAsync(s => s.OrganizationId == org.Id && s.PlantCode == request.PlantCode))
            return null;

        var site = new SolarSite
        {
            OrganizationId = org.Id,
            PlantCode = request.PlantCode.ToUpperInvariant(),
            Name = request.Name,
            Location = request.Location,
            CapacityMw = request.CapacityMw,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            TargetPr = request.TargetPr
        };

        _db.SolarSites.Add(site);
        await _db.SaveChangesAsync();

        return new SiteDto(site.Id, site.Name, site.Location, site.CapacityMw,
            site.Latitude, site.Longitude, site.TargetPr, site.OrganizationId ?? 0, site.PlantCode, org.Name);
    }

    private async Task AssignSitesAsync(int targetUserId, List<int> siteIds, int actorUserId, UserRole actorRole, int? actorOrgId)
    {
        var accessible = await _access.GetAccessibleSitesAsync(actorUserId, actorRole, actorOrgId);
        var allowedIds = accessible.Select(s => s.Id).ToHashSet();
        var validIds = siteIds.Where(allowedIds.Contains).ToList();

        var existing = await _db.UserSiteAssignments.Where(a => a.UserId == targetUserId).ToListAsync();
        _db.UserSiteAssignments.RemoveRange(existing);

        foreach (var siteId in validIds)
            _db.UserSiteAssignments.Add(new UserSiteAssignment { UserId = targetUserId, SiteId = siteId });

        await _db.SaveChangesAsync();
    }

    private async Task<AdminUserDto?> GetUserByIdAsync(int id, UserRole role, int? orgId)
    {
        var users = await GetUsersAsync(role, orgId, null);
        return users.FirstOrDefault(u => u.Id == id);
    }

    private static AdminUserDto MapUser(User user) => new(
        user.Id, user.FullName, user.Email, user.Phone, user.Role.ToString(),
        user.OrganizationId, user.Organization?.Name, user.IsActive,
        user.SiteAssignments.Select(a => new SiteDto(
            a.Site.Id, a.Site.Name, a.Site.Location, a.Site.CapacityMw,
            a.Site.Latitude, a.Site.Longitude, a.Site.TargetPr,
            a.Site.OrganizationId ?? 0, a.Site.PlantCode, a.Site.Organization?.Name)).ToList(),
        user.CreatedAt);
}
