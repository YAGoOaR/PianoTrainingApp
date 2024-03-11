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

    public KeyLightsManager LightsManager { get; private set; }

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
        Settings = new GameSettings();

        Task.Run(async () =>
        {
            output = await new OutputPortManager("CASIO USB-MIDI").OpenPort();
            input = await new InputPortManager("CASIO USB-MIDI").OpenPort();

            var lights = new KeyLights(output);
            LightsManager = new KeyLightsManager(lights);

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

    private void Play(string filePath)
    {
        Debug.WriteLine("Playing music...");

        Player.LoadMIDI(filePath);

        (float, float)? tRange = null; //(0f, 44.5f / Player.Settings.tempoRatio);

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

        GetTree().ChangeSceneToFile("res://Scenes/main.tscn");
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
