using Commons.Music.Midi;
using Godot;
using PianoTrainer.Scripts.MIDI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public partial class MIDIManager : Node2D
{
    public static MIDIManager Instance { get; private set; }

    public KeyState Piano { get; private set; }

    public MIDIPlayer Player { get; private set; }

    [Export]
    public ProgressBar PBar { get; private set; }

    [Export]
    public FallingNotes MSheet { get; private set; }

    public PianoKeyLighting LightsManager { get; private set; }

    public GameSettings Settings { get; private set; }

    private IMidiOutput output;
    private IMidiInput input;

    public enum MIDIManagerState
    {
        Preparing,
        Ready,
        Playing,
        Stopped,
        Exited,
    }

    public MIDIManagerState State { get; private set; } = MIDIManagerState.Preparing;

    public void SetState(MIDIManagerState state)
    {
        State = state;
    }

    public override void _Ready()
    {
        ListDevices();
        Instance = this;
        Piano = new KeyState();
        Player = new MIDIPlayer();
        Settings = GameSettings.Instance;

        Task.Run(async () =>
        {
            var device = GameSettings.Instance.Settings.PianoDeviceName;

            output = await new OutputPortManager(device).OpenPort();
            input = await new InputPortManager(device).OpenPort();

            var lights = new KeyboardInterface(output);
            LightsManager = new PianoKeyLighting(lights);

            input.MessageReceived += OnMessage;

            Player.Setup(this);

            SetState(MIDIManagerState.Ready);
        });
    }

    public void PlayMIDI(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.WriteLine("File not found.");
            return;
        }
        Play(filePath);
    }

    public void SelectRange(float start, float end)
    {
        if (start == 0 && end == 1)
        {
            Play(Settings.Settings.MusicPath);
            return;
        }
        var range = (start * Player.TotalTimeSeconds, end * Player.TotalTimeSeconds);
        Play(Settings.Settings.MusicPath, range);
    }

    public void RangeSelectionMoved(float start, float end)
    {
        var range = (start * Player.TotalTimeSeconds, end * Player.TotalTimeSeconds);
        PBar.SetSelectionPreview(Player, range);
    }

    private void Play(string filePath, (float, float)? tRange = null)
    {
        Debug.WriteLine("Playing music...");

        MSheet.Init();

        Player.LoadMIDI(filePath);

        Player.Play(LightsManager, tRange);

        PBar.SetTimeRange(Player, tRange);

        Player.PlayManager.OnStopped += () => Player.Play(LightsManager, tRange);
    }

    public void OnMessage(object input, MidiReceivedEventArgs message)
    {
        var msgType = message.Data[0];

        bool isKeyData = msgType == MidiEvent.NoteOn || msgType == MidiEvent.NoteOff;

        if (isKeyData)
        {
            byte note = message.Data[1];
            Piano.SetKey(new(note, msgType == MidiEvent.NoteOn));
        }
    }

    static void ListDevices()
    {
        Debug.WriteLine("Available input devices:");
        MidiAccessManager.Default.Inputs.ToList().ForEach(x => Debug.WriteLine(x.Name));
        Debug.WriteLine("");

        Debug.WriteLine("Available output devices:");
        MidiAccessManager.Default.Outputs.ToList().ForEach(x => Debug.WriteLine(x.Name));
        Debug.WriteLine("");
    }

    public override void _Process(double delta)
    {
        if (State == MIDIManagerState.Ready)
        {
            PlayMIDI(Settings.Settings.MusicPath);
            SetState(MIDIManagerState.Playing);
        }

        Player.Process(delta);

        if (State != MIDIManagerState.Stopped || State == MIDIManagerState.Exited) return;

        Debug.WriteLine("Returned to menu.");

        GetTree().ChangeSceneToFile(SceneManager.MenuScene);
        SetState(MIDIManagerState.Exited);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            SetState(MIDIManagerState.Stopped);
        }
    }

    public override void _ExitTree()
    {
        LightsManager.Dispose();
        output.Dispose();
        input.Dispose();
    }
}
