namespace SwitchBotInfluxDbExporter.Options;

public class Target
{
    public string DeviceId { get; set; } = string.Empty;

    public string Measurement { get; set; } = string.Empty;

    public List<string> Keys { get; set; } = [];

    public List<Tags> Tags { get; set; } = [];

    public int ApiInterval { get; set; } = 60;


}

public class Tags
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
