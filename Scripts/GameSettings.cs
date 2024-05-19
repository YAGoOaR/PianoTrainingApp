using System.IO;
using System.Text.Json;

public partial class GameSettings
{
    public struct PlayerSettings()
    {
        public int KeyTimeOffset { get; } = 100;
        public int StartOffset { get; } = 2000;
        public int BlinkStartOffset { get; } = 3000;
        public int BlinkInterval { get; } = 80;
        public int BlinkSlowInterval { get; } = 200;
        public int BlinkFastStartOffset { get; } = 1000;
        public int BlinkOutdateTime { get; } = 300;
    }

    public struct GSettings
    {
        public PlayerSettings playerSettings;

        public string MusicFolder { get; set; }
        public string MusicPath { get; set; }
        public string PianoDeviceName { get; set; }
    }
    public string SettingsPath { get; set; } = @"./player_settings.json";

    private const string defaultDevice = "CASIO USB-MIDI";

    private static GameSettings instance;
    public static GameSettings Instance
    {
        get
        {
            instance ??= new();
            return instance;
        }
    }

    public GSettings Settings = new() { MusicPath = "", playerSettings = new()};
    private GameSettings()
    {
        if (!Load()) Save(); // Create a new settings file if does not exist

        Settings.PianoDeviceName ??= defaultDevice;
    }

    public bool Load()
    {
        if (!File.Exists(SettingsPath)) return false;

        Settings = JsonSerializer.Deserialize<GSettings>(File.ReadAllText(SettingsPath));

        return true;
    }

    public void Save()
    {
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Settings));
    }
}
