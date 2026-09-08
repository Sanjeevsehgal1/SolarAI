using SolarShield.Core.Enums;

namespace SolarShield.Core.Entities;

public class RolePermission
{
    public int Id { get; set; }
    public UserRole Role { get; set; }
    public int PermissionId { get; set; }

    public Permission Permission { get; set; } = null!;
}
