using Commons.Music.Midi;
using Godot;
using PianoTrainer.MIDI;
using PianoTrainer.Scripts.GameElements;
using PianoTrainer.Scripts.MIDI;
using PianoTrainer.Scripts.PianoInteraction;
using PianoTrainer.Settings;
using System.Threading.Tasks;

namespace PianoTrainer.Game;

public partial class GameManager : Node2D
{
    public static GameManager Instance { get; private set; }

    public KeyState Piano { get; private set; }

    [Export]
    public ProgressBar PBar { get; private set; }

    [Export]
    public FallingNotes FallingNotes { get; private set; }

    public PianoKeyLighting Lights { get; private set; }

    public MusicPlayer MusicPlayer { get; private set; } = new();

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

    public override void _Ready()
    {
        Instance = this;

        Piano = new();

        var parsedMusic = MIDIReader.LoadSelectedMusic(noteFilter: Piano.HasKey);

        MusicPlayer.Setup(parsedMusic);

        SetupDevice();
    }

    public Task SetupDevice() => Task.Run(async () =>
    {
        var device = GameSettings.Instance.Settings.PianoDeviceName;

        output = await new OutputPortManager(device).OpenPort();
        input = await new InputPortManager(device).OpenPort();

        var lightsPort = new KeyboardInterface(output);
        Lights = new PianoKeyLighting(lightsPort);

        var keyHints = new NoteHints(Lights, MusicPlayer);

        input.MessageReceived += OnMessage;

        Piano.KeyChange += (_, _) => MusicPlayer.OnKeyChange(Piano.State);

        MusicPlayer.OnTargetChanged += keyHints.OnTargetCompleted;
        MusicPlayer.OnStopped += () => Lights.Reset();

        State = GameState.Ready;
    });

    public void OnMessage(object input, MidiReceivedEventArgs message)
    {
        var msgType = message.Data[0];

        bool isNoteData = msgType == MidiEvent.NoteOn || msgType == MidiEvent.NoteOff;

        if (isNoteData)
        {
            byte note = message.Data[1];
            Piano.SetKey(new(note, msgType == MidiEvent.NoteOn));
        }
    }

    public override void _Process(double delta)
    {
        if (State == GameState.Running)
        {
            MusicPlayer.Update((float)delta);
        }
        else if (State == GameState.Ready)
        {
            MusicPlayer.Play();
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

    public void Update(double delta) => MusicPlayer.Update((float)delta);

    public override void _ExitTree()
    {
        Lights.Dispose();
        output.Dispose();
        input.Dispose();
    }
}
