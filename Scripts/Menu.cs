using Godot;
using System.IO;

using System.Text.Json;

public partial class Menu : Node2D
{
    private string directoryPath = @"C:\Users\yagooar\Desktop\midi_files";

    [Export]
    ItemList itemList;

    [Export]
    Texture2D icon;

    [Export]
    GameSettings settings;

    [Export]
    TextEdit DisplayText;

    private string[] midis;

    public void OnItemSelect(int idx)
    {
        settings.Settings.MusicPath = midis[idx];
        DisplayText.Text = midis[idx];
    }

    public override void _Ready()
    {
        midis = Directory.GetFiles(directoryPath, "*.mid");
        foreach (string midiFile in midis)
        {
            itemList.AddItem(Path.GetFileName(midiFile), icon);
        }
    }

    public void UpdateText()
    {
        DisplayText.Text = settings.Settings.MusicPath;
    }

    public void OnPlayPressed()
    {
        GameSettings.GSettings s = settings.Settings;
        File.WriteAllText(settings.SettingsPath, JsonSerializer.Serialize(s));
        GetTree().ChangeSceneToFile("res://PlayScene.tscn");
    }

    public void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
