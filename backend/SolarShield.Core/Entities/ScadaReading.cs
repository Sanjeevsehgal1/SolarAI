namespace SolarShield.Core.Entities;

public class ScadaReading
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public int? AssetId { get; set; }
    public double Voltage { get; set; }
    public double Current { get; set; }
    public double PowerKw { get; set; }
    public double Temperature { get; set; }
    public double Frequency { get; set; }
    public string InverterStatus { get; set; } = "Running";
    public string CommunicationStatus { get; set; } = "Connected";
    public double EnergyGenerationKwh { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public SolarSite Site { get; set; } = null!;
    public Asset? Asset { get; set; }
}
