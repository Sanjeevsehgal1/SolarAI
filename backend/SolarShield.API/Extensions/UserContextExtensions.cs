using System.Security.Claims;
using SolarShield.Core.Enums;

namespace SolarShield.API.Extensions;

public record UserContext(int UserId, UserRole Role, int? OrganizationId)
{
    public static UserContext FromClaims(ClaimsPrincipal user)
    {
        var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = Enum.TryParse<UserRole>(user.FindFirstValue(ClaimTypes.Role), true, out var r)
            ? r : UserRole.Viewer;
        int? orgId = int.TryParse(user.FindFirstValue("organizationId"), out var oid) ? oid : null;
        return new UserContext(userId, role, orgId);
    }
}
