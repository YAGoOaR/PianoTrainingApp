
using System.IO;
using System.Text.Json;

namespace PianoTrainer.Scripts;

// Singleton class to handle settings 
public partial class GameSettings
{
    public static string MenuScene { get; } = "res://Scenes/main.tscn";
    public static string GameScene { get; } = "res://Scenes/PlayScene.tscn";

    private const string settingsPath = @"./player_settings.json";
    private const string defaultDevice = "CASIO USB-MIDI";

    public readonly struct PlayerSettings()
    {
        public int KeyTimeOffset { get; } = 100;
        public int StartOffset { get; } = 2000;
        public int BlinkStartOffset { get; } = 3000;
        public int BlinkInterval { get; } = 80;
        public int BlinkSlowInterval { get; } = 200;
        public int BlinkFastStartOffset { get; } = 1000;
        public int LateHintOutdateTime { get; } = 300;
    }

    public struct GSettings
    {
        public PlayerSettings PlayerSettings { get; set; }
        public string MusicFolder { get; set; }
        public string MusicPath { get; set; }
        public string PianoDeviceName { get; set; }
    }
    public GSettings Settings = new() { MusicPath = "", PlayerSettings = new() };

    private static GameSettings instance;
    public static GameSettings Instance
    {
        get
        {
            instance ??= new();
            return instance;
        }
    }

    private GameSettings()
    {
        if (!Load()) Save(); // Create a new settings file if does not exist

        Settings.PianoDeviceName ??= defaultDevice;
    }

    public bool Load()
    {
        if (!File.Exists(settingsPath)) return false;

        Settings = JsonSerializer.Deserialize<GSettings>(File.ReadAllText(settingsPath));

        return true;
    }

    public void Save()
    {
        File.WriteAllText(settingsPath, JsonSerializer.Serialize(Settings));
    }
}
