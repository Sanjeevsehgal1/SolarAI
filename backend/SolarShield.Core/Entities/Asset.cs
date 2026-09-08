using SolarShield.Core.Enums;

namespace SolarShield.Core.Entities;

public class Asset
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public int? ZoneId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public AssetType Type { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime? InstallationDate { get; set; }
    public double CapacityKw { get; set; }
    public string Location { get; set; } = string.Empty;
    public AssetStatus Status { get; set; } = AssetStatus.Online;
    public double HealthScore { get; set; } = 100;
    public DateTime? LastMaintenanceAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SolarSite Site { get; set; } = null!;
    public Zone? Zone { get; set; }
    public ICollection<ScadaReading> ScadaReadings { get; set; } = [];
}
