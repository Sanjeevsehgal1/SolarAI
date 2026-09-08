namespace SolarShield.Core.Entities;

public class Zone
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StringId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int PanelCount { get; set; }

    public SolarSite Site { get; set; } = null!;
    public ICollection<Panel> Panels { get; set; } = [];
    public ICollection<SoilingRecord> SoilingRecords { get; set; } = [];
    public ICollection<CleaningSchedule> CleaningSchedules { get; set; } = [];
}
