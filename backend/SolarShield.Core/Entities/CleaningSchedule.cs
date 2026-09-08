namespace SolarShield.Core.Entities;

public class CleaningSchedule
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public int ZoneId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public double SoilingIndexAtSchedule { get; set; }
    public int Priority { get; set; }
    public bool IsCompleted { get; set; }
    public string? PhotoProofUrl { get; set; }
    public string? Notes { get; set; }

    public SolarSite Site { get; set; } = null!;
    public Zone Zone { get; set; } = null!;
}
