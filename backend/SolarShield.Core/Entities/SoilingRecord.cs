namespace SolarShield.Core.Entities;

public class SoilingRecord
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public int ZoneId { get; set; }
    public DateTime RecordDate { get; set; }
    public double SoilingIndex { get; set; }
    public double EstimatedLossPercent { get; set; }

    public SolarSite Site { get; set; } = null!;
    public Zone Zone { get; set; } = null!;
}
