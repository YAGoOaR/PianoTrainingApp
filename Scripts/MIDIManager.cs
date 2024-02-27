using Commons.Music.Midi;
using CoreMidi;
using Godot;
using PianoTrainer.Scripts.MIDI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

public partial class MIDIManager : Node2D
{
    private Thread playerThread;

    public event Action<byte, bool> KeyPressed; // TODO: CHANGE TO STATE's event

    public KeyState Piano { get; private set; }

    public MIDIPlayer Player {  get; private set; }

    public void PlayMIDI(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.WriteLine("File not found.");
            return;
        }

        playerThread = new(() => Play(filePath));
        playerThread.Start();
    }

    public override void _Ready()
    {
        ListDevices();
    }

    private void Play(string filePath)
    {
        Debug.WriteLine("Playing music...");

        Piano = new KeyState();

        using var output = new OutputPortManager("CASIO USB-MIDI").OpenPort();
        using var input = new InputPortManager("CASIO USB-MIDI").OpenPort();
        var lights = new KeyLights(output);
        using var lightsManager = new KeyLightsManager(lights);
        Player = new MIDIPlayer(Piano, lightsManager);

        input.MessageReceived += OnMessage;

        Player.Load(filePath);

        while (true)
        {
            Player.Play();
        }
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
}
