using Godot;
using System.IO;
using System.Text.Json;

public partial class GameSettings : Node2D
{
    public struct GSettings
    {
        public string MusicPath { get; set; }
    }

    [Signal]
    public delegate void SettingsLoadedEventHandler();

    public GSettings Settings = new() { MusicPath = "" };
    public override void _Ready()
    {
        if (File.Exists("./settings.txt"))
        {
            Settings = JsonSerializer.Deserialize<GSettings>(File.ReadAllText("./settings.txt"));
            EmitSignal(SignalName.SettingsLoaded);
        }
    }
}
