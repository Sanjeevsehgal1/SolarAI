using SolarShield.Core.Enums;

namespace SolarShield.Core.Entities;

public class Fault
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public int? ZoneId { get; set; }
    public FaultType Type { get; set; }
    public FaultSeverity Severity { get; set; }
    public FaultStatus Status { get; set; } = FaultStatus.Open;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPredicted { get; set; }
    public double? ConfidenceScore { get; set; }
    public int? AssignedToUserId { get; set; }
    public string? FixNotes { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public SolarSite Site { get; set; } = null!;
    public Zone? Zone { get; set; }
    public User? AssignedTo { get; set; }
    public ICollection<FaultComment> Comments { get; set; } = [];
}
