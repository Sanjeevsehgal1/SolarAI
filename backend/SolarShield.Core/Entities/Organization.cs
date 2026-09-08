namespace SolarShield.Core.Entities;

public class Organization
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string SubscriptionPlan { get; set; } = "Standard";
    public int MaxPlants { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = [];
    public ICollection<SolarSite> Plants { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
}
