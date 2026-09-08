namespace SolarShield.Core.Entities;

public class Panel
{
    public int Id { get; set; }
    public int ZoneId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RatedPowerW { get; set; } = 540;
    public DateTime InstallDate { get; set; }

    public Zone Zone { get; set; } = null!;
}
