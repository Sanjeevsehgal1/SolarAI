namespace SolarShield.Core.Entities;

public class UserSiteAssignment
{
    public int UserId { get; set; }
    public int SiteId { get; set; }

    public User User { get; set; } = null!;
    public SolarSite Site { get; set; } = null!;
}
