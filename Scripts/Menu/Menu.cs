
using Godot;
using System.IO;
namespace PianoTrainer.Scripts.Menu;

internal partial class Menu : Control
{
    [Export] private ItemList itemList;
    [Export] private Texture2D icon;
    [Export] private TextEdit SongPath;
    [Export] private TextEdit FolderPath;
    [Export] private FileDialog fileDialog;

    private string[] midis;

    private GameSettings settings;

    public void OnItemSelect(int idx)
    {
        var path = Utils.FixPath(midis[idx]);
        settings.Settings.MusicPath = path;
        SongPath.Text = path;
        settings.Save();
    }

    public override void _Ready()
    {
        settings = GameSettings.Instance;

        var musicDir = settings.Settings.MusicFolderPath;

        SongPath.Text = Utils.FixPath(settings.Settings.MusicPath);
        FolderPath.Text = musicDir;

        if (string.IsNullOrEmpty(musicDir))
        {
            fileDialog.Show();
            return;
        }

        UpdateItems(musicDir);
    }

    public void UpdateItems(string folder)
    {
        itemList.Clear();

        midis = [.. Directory.GetFiles(folder, "*.mid"), .. Directory.GetFiles(folder, "*.midi")];

        foreach (string midiFile in midis)
        {
            itemList.AddItem(Path.GetFileName(midiFile), icon);
        }
    }

    public void OnPlayPressed()
    {
        GetTree().ChangeSceneToFile(GameSettings.GameScene);
    }

    public void OnBrowsePressed()
    {
        if (!string.IsNullOrEmpty(settings.Settings.MusicFolderPath))
        {
            fileDialog.CurrentPath = settings.Settings.MusicFolderPath;
        }

        fileDialog.Show();
    }

    public void OnQuitPressed()
    {
        GetTree().Quit();
    }

    public void OnFolderSelect(string folder)
    {
        settings.Settings.MusicFolderPath = folder;
        UpdateItems(folder);
        FolderPath.Text = folder;
        settings.Save();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_accept")) OnPlayPressed();
        else if (@event.IsActionPressed("ui_cancel")) OnQuitPressed();
    }
}
