
using Godot;
using PianoTrainer.Scripts.Devices;
using PianoTrainer.Scripts.GameElements;
using PianoTrainer.Scripts.PianoInteraction;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts;

public enum GameState
{
    Preparing,
    Ready,
    Running,
    Stopped,
    Exited,
}

/// <summary>
/// The main class that handles the game flow.
/// </summary>
public partial class GameManager : Node2D
{
    private static readonly GSettings settings = GameSettings.Instance.Settings;

    private readonly MusicPlayer musicPlayer = MusicPlayer.Instance;
    private readonly DeviceManager deviceManager = DeviceManager.Instance;

    public GameState State { get; private set; } = GameState.Preparing;

    // Called when game scene is loaded
    public override void _Ready()
    {
        NoteHints.Init();

        var parsedMusic = MIDIReader.LoadSelectedMusic(noteFilter: deviceManager.DefaultPiano.Keys.HasKey);

        musicPlayer.Setup(parsedMusic);

        Task.Run(SetupDevices);
    }

    public async Task SetupDevices()
    {
        await deviceManager.ConnectAllDevices();
        State = GameState.Ready;
        Alerts.Instance?.ShowWaiting(false);
    }

    // Called each game frame.
    public override void _Process(double delta)
    {
        if (State == GameState.Running)
        {
            if (musicPlayer.PlayingState == PlayState.Stopped)
            {
                State = GameState.Stopped;
                return;
            }

            musicPlayer.Update((float)delta);
        }
        else if (State == GameState.Ready)
        {
            musicPlayer.Play();
            State = GameState.Running;
        }
        else if (State == GameState.Stopped)
        {
            if (settings.Autoretry)
            {
                State = GameState.Ready;
                return;
            }

            State = GameState.Exited;
            Exit();
        }
    }

    public void Exit()
    {
        GetTree().ChangeSceneToFile(GameSettings.MenuScene);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            State = GameState.Exited;
            Exit();
        }
    }

    public override void _ExitTree()
    {
        DeviceManager.DisconnectDevices();
    }
}
