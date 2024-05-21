using Commons.Music.Midi;
using Godot;
using PianoTrainer.Scripts.GameElements;
using PianoTrainer.Scripts.PianoInteraction;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts;

/// <summary>
/// The main class that handles the game flow.
/// </summary>
public partial class GameManager : Node2D
{
    public static GameManager Instance { get; private set; }
    public KeyState Piano { get; private set; }
    public PianoKeyLighting Lights { get; private set; }

    private readonly MusicPlayer musicPlayer = MusicPlayer.Instance;
    private IMidiOutput output;
    private IMidiInput input;

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
        Instance = this;

        Piano = new();

        var parsedMusic = MIDIReader.LoadSelectedMusic(noteFilter: Piano.HasKey);

        musicPlayer.Setup(parsedMusic);

        SetupDevice();
    }

    // Opens I/O MIDI ports to devices and sets events asynchronously
    public Task SetupDevice() => Task.Run(async () =>
    {
        var device = GameSettings.Instance.Settings.PianoDeviceName;

        output = await new OutputPortManager(device).OpenPort();
        input = await new InputPortManager(device).OpenPort();

        var lightsPort = new KeyboardInterface(output);
        Lights = new PianoKeyLighting(lightsPort);

        var keyHints = new NoteHints(Lights);

        input.MessageReceived += OnMessage;

        Piano.KeyChange += (_, _) => musicPlayer.OnKeyChange(Piano.State);

        musicPlayer.OnTargetChanged += keyHints.OnTargetCompleted;
        musicPlayer.OnStopped += Lights.Reset;

        State = GameState.Ready;
    });

    // Handles MIDI messages that come from piano device
    public void OnMessage(object _, MidiReceivedEventArgs message)
    {
        var msgType = message.Data[0];

        bool isNoteData = msgType == MidiEvent.NoteOn || msgType == MidiEvent.NoteOff;

        if (isNoteData)
        {
            byte note = message.Data[1];
            Piano.SetKey(new(note, msgType == MidiEvent.NoteOn));
        }
    }

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

    public void Update(double delta) => musicPlayer.Update((float)delta);

    public override void _ExitTree()
    {
        Lights.Dispose();
        output.Dispose();
        input.Dispose();
    }
}
