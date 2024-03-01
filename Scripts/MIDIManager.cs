using Commons.Music.Midi;
using Godot;
using PianoTrainer.Scripts.MIDI;
using System.Diagnostics;
using System.IO;
using System.Linq;

public partial class MIDIManager : Node2D
{

    public KeyState Piano { get; private set; }

    [Export]
    public MIDIPlayer Player {  get; private set; }

    public static MIDIManager Instance { get; private set; }

    public KeyLightsManager LightsManager { get; private set; }

    private IMidiOutput output;
    private IMidiInput input;

    private bool stopped = false;
    private bool exit = false;

    public override void _Ready()
    {
        ListDevices();
        Instance = this;
        Piano = new KeyState();

        output = new OutputPortManager("CASIO USB-MIDI").OpenPort();
        input = new InputPortManager("CASIO USB-MIDI").OpenPort();
        var lights = new KeyLights(output);
        LightsManager = new KeyLightsManager(lights);

        input.MessageReceived += OnMessage;
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

        Player.Load(filePath);

        Player.Play(LightsManager);

        Player.PlayManager.OnStopped += () => Player.Play(LightsManager);
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
        if (!stopped) return;
        if (exit) return;

        GetTree().ChangeSceneToFile("res://Scenes/main.tscn");

        exit = true;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            stopped = true;
        }
    }

    public override void _ExitTree()
    {
        LightsManager.Dispose();
        output.Dispose();
        input.Dispose();
    }
}
