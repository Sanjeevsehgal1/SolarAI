namespace SolarShield.Core.Entities;

public class FaultComment
{
    public int Id { get; set; }
    public int FaultId { get; set; }
    public int UserId { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Fault Fault { get; set; } = null!;
    public User User { get; set; } = null!;
}
