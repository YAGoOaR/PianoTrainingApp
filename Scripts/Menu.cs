using Godot;
using System.IO;
using System.Text.Json;

internal partial class Menu : Node2D
{
    private string directoryPath = @"C:\Users\yagooar\Desktop\midi_files";

    [Export]
    private string playScenePath = "res://Scenes/PlayScene.tscn";

    [Export]
    private ItemList itemList;

    [Export]
    private Texture2D icon;

    [Export]
    private TextEdit DisplayText;

    private string[] midis;

    private GameSettings Settings;

    public void OnItemSelect(int idx)
    {
        Settings.Settings.MusicPath = midis[idx];
        DisplayText.Text = midis[idx];
    }

    public override void _Ready()
    {
        Settings = new GameSettings();

        midis = [..Directory.GetFiles(directoryPath, "*.mid"), ..Directory.GetFiles(directoryPath, "*.midi")];

        foreach (string midiFile in midis)
        {
            itemList.AddItem(Path.GetFileName(midiFile), icon);
        }
    }

    public void UpdateText()
    {
        DisplayText.Text = Settings.Settings.MusicPath;
    }

    public void OnPlayPressed()
    {
        GameSettings.GSettings s = Settings.Settings;
        File.WriteAllText(Settings.SettingsPath, JsonSerializer.Serialize(s));
        GetTree().ChangeSceneToFile(playScenePath);
    }

    public void OnQuitPressed()
    {
        GetTree().Quit();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_accept"))
        {
            OnPlayPressed();
        }

        if (@event.IsActionPressed("ui_cancel"))
        {
            OnQuitPressed();
        }
    }
}
