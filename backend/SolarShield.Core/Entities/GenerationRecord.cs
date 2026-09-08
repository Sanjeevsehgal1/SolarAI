namespace SolarShield.Core.Entities;

public class GenerationRecord
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public DateTime RecordDate { get; set; }
    public double ActualKwh { get; set; }
    public double TheoreticalKwh { get; set; }
    public double PerformanceRatio { get; set; }
    public double Irradiance { get; set; }
    public double Temperature { get; set; }

    public SolarSite Site { get; set; } = null!;
}
