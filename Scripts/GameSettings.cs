using Godot;
using System.IO;
using System.Text.Json;

public partial class GameSettings
{
    public struct GSettings
    {
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

    public GSettings Settings = new() { MusicPath = "" };
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
