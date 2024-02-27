using Godot;
using System.IO;
using System.Text.Json;

public partial class GameSettings : Node2D
{
    public struct GSettings
    {
        public string MusicPath { get; set; }
    }
    public string SettingsPath { get; set; } = @"./player_settings.json";


    [Signal]
    public delegate void SettingsLoadedEventHandler();

    public GSettings Settings = new() { MusicPath = "" };
    public override void _Ready()
    {
        if (File.Exists(SettingsPath))
        {
            Settings = JsonSerializer.Deserialize<GSettings>(File.ReadAllText(SettingsPath));
            EmitSignal(SignalName.SettingsLoaded);
        }
    }
}
