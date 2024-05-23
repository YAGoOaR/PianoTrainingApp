
using Godot;
using PianoTrainer.Scripts.Devices;
using PianoTrainer.Scripts.GameElements;
using PianoTrainer.Scripts.PianoInteraction;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts;

/// <summary>
/// The main class that handles the game flow.
/// </summary>
public partial class GameManager : Node2D
{
    private readonly MusicPlayer musicPlayer = MusicPlayer.Instance;

    public enum GameState
    {
        Preparing,
        Ready,
        Running,
        Stopped,
        Exited,
    }

    public GameState State { get; private set; } = GameState.Preparing;

    // Called when game scene is loaded
    public override void _Ready()
    {
        NoteHints.Init();

        var parsedMusic = MIDIReader.LoadSelectedMusic(noteFilter: DeviceManager.Instance.DefaultPiano.Piano.HasKey);

        musicPlayer.Setup(parsedMusic);

        SetupDevices();
    }

    public Task SetupDevices() => Task.Run(async () =>
    {
        await DeviceManager.Instance.ConnectAllDevices();
        State = GameState.Ready;
        Alerts.Instance?.ShowWaiting(false);
    });

    // Called each game frame.
    public override void _Process(double delta)
    {
        if (State == GameState.Running)
        {
            musicPlayer.Update((float)delta);
        }
        else if (State == GameState.Ready)
        {
            musicPlayer.Play();
            State = GameState.Running;
        }
        else if (State == GameState.Stopped)
        {
            GetTree().ChangeSceneToFile(GameSettings.MenuScene);
            State = GameState.Exited;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            State = GameState.Stopped;
        }
    }

    public override void _ExitTree()
    {
        DeviceManager.DisconnectDevices();
    }
}
