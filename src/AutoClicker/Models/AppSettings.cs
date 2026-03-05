using System.Text.Json.Serialization;

namespace AutoClicker.Models;

public sealed class AppSettings
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "single";

    [JsonPropertyName("click_type")]
    public string ClickTypeName { get; set; } = "left";

    [JsonPropertyName("interval_ms")]
    public int IntervalMs { get; set; } = 100;

    [JsonPropertyName("count_mode")]
    public string CountMode { get; set; } = "infinite";

    [JsonPropertyName("count_fixed")]
    public int CountFixed { get; set; } = 100;

    [JsonPropertyName("single_x")]
    public int SingleX { get; set; }

    [JsonPropertyName("single_y")]
    public int SingleY { get; set; }

    [JsonPropertyName("multi_points")]
    public List<ClickPointData> MultiPoints { get; set; } = [];

    [JsonPropertyName("hotkey_toggle")]
    public string HotkeyToggle { get; set; } = "F6";

    [JsonPropertyName("hotkey_stop")]
    public string HotkeyStop { get; set; } = "F7";

    [JsonPropertyName("resident_tray")]
    public bool ResidentTray { get; set; } = true;
}

public sealed class ClickPointData
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("extra_wait_ms")]
    public int ExtraWaitMs { get; set; }
}
