using SolarShield.Core.Enums;

namespace SolarShield.Core.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int? OrganizationId { get; set; }
    public string? PhotoUrl { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Organization? Organization { get; set; }
    public ICollection<UserSiteAssignment> SiteAssignments { get; set; } = [];
    public ICollection<Fault> AssignedFaults { get; set; } = [];
    public ICollection<WorkTask> AssignedTasks { get; set; } = [];
}
