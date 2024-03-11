using Godot;
using System;
using System.IO;
using System.Text.Json;

public partial class GameSettings
{
    public struct GSettings
    {
        public string MusicPath { get; set; }
    }
    public string SettingsPath { get; set; } = @"./player_settings.json";

    [Signal]
    public delegate void SettingsLoadedEventHandler();

    public GSettings Settings = new() { MusicPath = "" };
    public GameSettings()
    {
        if (File.Exists(SettingsPath))
        {
            Settings = JsonSerializer.Deserialize<GSettings>(File.ReadAllText(SettingsPath));
        }
    }
}
