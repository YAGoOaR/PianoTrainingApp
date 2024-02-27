using Godot;
using System.Diagnostics;
using System.IO;

using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

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
        //var listNode = GetNode("ItemList");

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
        File.WriteAllText("./settings.txt", JsonSerializer.Serialize(s));

        //GameSettings.GSettings s = settings.Settings;
        GetTree().ChangeSceneToFile("res://PlayScene.tscn");
        //PlayScene scene2 = GetTree().Root.GetChild(0) as PlayScene;
        //scene2.Settings.Settings = s;

        //Debug.WriteLine("Update settings to:");
        //Debug.WriteLine(s);
    }

    public void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
