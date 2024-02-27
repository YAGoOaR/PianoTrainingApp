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

    // Called when the node enters the scene tree for the first time.

    public event Action<byte, bool> KeyPressed; // TODO: CHANGE TO STATE's event

    private KeyState piano;

    public PreBlinkPlayer Player {  get; private set; }

    //[Signal]
    //public delegate void PianoKeyPressedEventHandler(byte key, bool state);

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
        // TODO: REFACTOR EXCEPTIONS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        //while(true)
        {
            try
            {
                Debug.WriteLine("Playing music...");

                piano = new KeyState();
                piano.KeyChange += (k, s) => KeyPressed?.Invoke(k, s);

                using var output = new OutputPortManager("CASIO USB-MIDI").OpenPort();
                using var input = new InputPortManager("CASIO USB-MIDI").OpenPort();
                var lights = new KeyLights(output);
                using var lightsManager = new KeyLightsManager(lights);
                Player = new PreBlinkPlayer(piano, lightsManager); // TODO: Dispose!

                lights.OnError += () => Player.StopSignal.TrySetResult();

                input.MessageReceived += OnMessage;

                bool err = false;

                lights.OnError += () => err = true;

                while (!err)
                {
                    Player.Load(filePath);
                    // TODO: Make config for player
                    Player.Play();
                    Player.StopSignal.Task.Wait();
                }
            } catch (MidiException e)
            {
                Player.Dispose();
                Debug.WriteLine(e.Message);
            }
        }
    }

	//// Called every frame. 'delta' is the elapsed time since the previous frame.
	//public override void _Process(double delta)
	//{

	//}

    public void OnMessage(object input, MidiReceivedEventArgs message)
    {
        var msgType = message.Data[0];

        bool isKeyData = msgType == MidiEvent.NoteOn || msgType == MidiEvent.NoteOff;

        if (isKeyData)
        {
            byte note = message.Data[1];
            piano.SetKey(new(note, msgType == MidiEvent.NoteOn));
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

    //public override void _ExitTree()
    //{
        
    //}
}
