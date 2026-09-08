namespace SolarShield.Core.Enums;

public enum AssetType
{
    Inverter = 0,
    Transformer = 1,
    SolarPanel = 2,
    WeatherStation = 3,
    Sensor = 4,
    CombinerBox = 5,
    Other = 6
}

public enum AssetStatus
{
    Online = 0,
    Offline = 1,
    Warning = 2,
    Critical = 3,
    Maintenance = 4
}
