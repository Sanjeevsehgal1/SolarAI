namespace SolarShield.Core.Entities;

public class SolarSite
{
    public int Id { get; set; }
    public int? OrganizationId { get; set; }
    public string PlantCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double CapacityMw { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double TargetPr { get; set; } = 80.0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Organization? Organization { get; set; } = null!;
    public ICollection<Zone> Zones { get; set; } = [];
    public ICollection<UserSiteAssignment> UserAssignments { get; set; } = [];
    public ICollection<Fault> Faults { get; set; } = [];
    public ICollection<WorkTask> Tasks { get; set; } = [];
    public ICollection<GenerationRecord> GenerationRecords { get; set; } = [];
    public ICollection<SoilingRecord> SoilingRecords { get; set; } = [];
    public ICollection<CleaningSchedule> CleaningSchedules { get; set; } = [];
    public ICollection<Asset> Assets { get; set; } = [];
    public ICollection<ScadaReading> ScadaReadings { get; set; } = [];
}
