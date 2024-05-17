using Godot;
using System.IO;
using System.Text.Json;

public partial class GameSettings
{
    public struct GSettings
    {
        public string MusicFolder { get; set; }
        public string MusicPath { get; set; }
    }
    public string SettingsPath { get; set; } = @"./player_settings.json";

    [Signal]
    public delegate void SettingsLoadedEventHandler();

    public GSettings Settings = new() { MusicPath = "" };
    public GameSettings()
    {
        Load();
    }

    public void Load()
    {
        if (File.Exists(SettingsPath))
        {
            Settings = JsonSerializer.Deserialize<GSettings>(File.ReadAllText(SettingsPath));
        }
        else
        {
            Save();
        }
    }

    public void Save()
    {
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Settings));
    }
}
