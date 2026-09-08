using Microsoft.EntityFrameworkCore;
using SolarShield.Core.Constants;
using SolarShield.Core.DTOs;
using SolarShield.Core.Enums;
using SolarShield.Infrastructure.Data;

namespace SolarShield.API.Services;

public class OrganizationService
{
    private readonly ApplicationDbContext _db;
    private readonly AccessControlService _access;

    public OrganizationService(ApplicationDbContext db, AccessControlService access)
    {
        _db = db;
        _access = access;
    }

    public async Task<List<OrganizationDto>> GetAllAsync(UserRole role, int? organizationId)
    {
        if (!_access.HasPermission(role, PermissionCodes.OrganizationView))
            return [];

        var orgs = await _access.ScopeOrganizations(role, organizationId)
            .OrderBy(o => o.Name)
            .ToListAsync();

        return await MapOrganizations(orgs);
    }

    public async Task<OrganizationDto?> GetByIdAsync(int id, UserRole role, int? organizationId)
    {
        if (!_access.HasPermission(role, PermissionCodes.OrganizationView))
            return null;

        var org = await _access.ScopeOrganizations(role, organizationId)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (org == null) return null;
        return (await MapOrganizations([org])).First();
    }

    public async Task<OrganizationDto?> CreateAsync(CreateOrganizationRequest request, UserRole role)
    {
        if (!_access.HasPermission(role, PermissionCodes.OrganizationCreate))
            return null;

        if (await _db.Organizations.AnyAsync(o => o.Code == request.Code))
            return null;

        var org = new Core.Entities.Organization
        {
            Name = request.Name,
            Code = request.Code.ToUpperInvariant(),
            ContactEmail = request.ContactEmail,
            SubscriptionPlan = request.SubscriptionPlan,
            MaxPlants = request.MaxPlants
        };

        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();
        return (await MapOrganizations([org])).First();
    }

    public async Task<OrganizationDto?> UpdateAsync(int id, UpdateOrganizationRequest request, UserRole role, int? organizationId)
    {
        if (!_access.HasPermission(role, PermissionCodes.OrganizationEdit))
            return null;

        var org = await _access.ScopeOrganizations(role, organizationId).FirstOrDefaultAsync(o => o.Id == id);
        if (org == null) return null;

        if (request.Name != null) org.Name = request.Name;
        if (request.ContactEmail != null) org.ContactEmail = request.ContactEmail;
        if (request.SubscriptionPlan != null) org.SubscriptionPlan = request.SubscriptionPlan;
        if (request.MaxPlants.HasValue) org.MaxPlants = request.MaxPlants.Value;
        if (request.IsActive.HasValue) org.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();
        return (await MapOrganizations([org])).First();
    }

    public async Task<PlatformStatsDto?> GetPlatformStatsAsync(UserRole role)
    {
        if (!_access.IsSuperAdmin(role)) return null;

        return new PlatformStatsDto(
            await _db.Organizations.CountAsync(),
            await _db.Organizations.CountAsync(o => o.IsActive),
            await _db.SolarSites.CountAsync(s => s.IsActive),
            await _db.SolarSites.Where(s => s.IsActive).SumAsync(s => s.CapacityMw),
            await _db.Users.CountAsync(u => u.IsActive),
            await _db.Faults.CountAsync(f => f.Status != Core.Enums.FaultStatus.Resolved),
            await _db.WorkTasks.CountAsync(t => t.Status != Core.Enums.WorkTaskStatus.Done),
            await _db.Faults.CountAsync(f => f.Severity == Core.Enums.FaultSeverity.Critical && f.Status != Core.Enums.FaultStatus.Resolved),
            await _db.Organizations.CountAsync(o => o.IsActive),
            await _db.Organizations.CountAsync(o => o.IsActive && o.CreatedAt > DateTime.UtcNow.AddMonths(-11)),
            await _db.Invoices.Where(i => i.IssueDate.Month == DateTime.UtcNow.Month && i.Status == "Paid").SumAsync(i => i.Total)
        );
    }

    private async Task<List<OrganizationDto>> MapOrganizations(List<Core.Entities.Organization> orgs)
    {
        var result = new List<OrganizationDto>();
        foreach (var org in orgs)
        {
            var plantCount = await _db.SolarSites.CountAsync(s => s.OrganizationId == org.Id && s.IsActive);
            var userCount = await _db.Users.CountAsync(u => u.OrganizationId == org.Id && u.IsActive);
            var capacity = await _db.SolarSites.Where(s => s.OrganizationId == org.Id && s.IsActive).SumAsync(s => s.CapacityMw);

            result.Add(new OrganizationDto(
                org.Id, org.Name, org.Code, org.ContactEmail, org.SubscriptionPlan,
                org.MaxPlants, plantCount, userCount, capacity, org.IsActive, org.CreatedAt));
        }
        return result;
    }
}
