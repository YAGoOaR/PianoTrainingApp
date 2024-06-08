
using System.IO;
using System.Text.Json;

namespace PianoTrainer.Scripts;

// General settings
public class GSettings()
{
    public string MusicFolderPath { get; set; } = "";
    public string MusicPath { get; set; } = "";
    public string PianoDeviceName { get; set; } = "CASIO USB-MIDI";
    public bool Autoretry { get; set; } = true;
    public byte PianoKeyCount { get; } = 61;
    public byte PianoMinMIDIKey { get; } = 36;
    public byte PianoMaxMIDIKey { get => (byte)(PianoMinMIDIKey + PianoKeyCount); }
}

// Music flow parameters
public class PlayerSettings()
{
    public int KeyTimeOffset { get; } = 100;
    public int BlinkStartOffset { get; } = 3000;
    public int BlinkInterval { get; } = 80;
    public int BlinkSlowInterval { get; } = 200;
    public int BlinkFastStartOffset { get; } = 1000;
    public int LateHintOutdateTime { get; } = 300;
    public int Timespan { get; } = 4000;
    public int DefaultTempo { get; } = 500000;
    public int StartBeatsOffset { get; } = 4;
}

// Manages all settings
public partial class GameSettings
{
    public static string MenuScene { get; } = "res://Scenes/main.tscn";
    public static string GameScene { get; } = "res://Scenes/PlayScene.tscn";

    private const string settingsPath = @"./player_settings.json";

    public GSettings Settings { get; private set; } = new();
    public PlayerSettings PlayerSettings { get; private set; } = new();

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
        if (!Load()) Save(); // Create a new settings file if can't load one
    }

    public bool Load()
    {
        if (!File.Exists(settingsPath)) return false;

        try
        {
            Settings = JsonSerializer.Deserialize<GSettings>(File.ReadAllText(settingsPath));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Save()
    {
        File.WriteAllText(settingsPath, JsonSerializer.Serialize(Settings));
    }
}
