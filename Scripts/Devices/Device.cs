
using Commons.Music.Midi;
using PianoTrainer.Scripts.PianoInteraction;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts.Devices;

public abstract class Device<T> where T : IMidiPort
{
    protected static readonly GSettings settings = GameSettings.Instance.Settings;

    public event Action OnDisconnect;

    private static readonly List<Device<T>> devices = [];

    public Device()
    {
        devices.Add(this);
    }

    protected void OnLightsDisconnect()
    {
        Stop();
        OnDisconnect.Invoke();
    }

    public abstract Task Stop();

    public static void StopDevices()
    {
        foreach (var device in devices)
        {
            device.Stop();
        }
    }
}

public abstract class InputDevice(string deviceName) : Device<IMidiInput>
{
    public abstract void OnMessage(object _, MidiReceivedEventArgs message);
    private readonly InputPort port = new(deviceName);

    public async Task Connect()
    {
        if (port.IsConnected) return;

        var portDetails = await port.OpenPort();

        portDetails.MessageReceived += OnMessage;
    }

    public override async Task Stop() => await port?.ClosePort();
}

public class PianoInputDevice(string deviceName) : InputDevice(deviceName)
{
    public KeyState Keys { get; private set; } = new(settings.PianoMinMIDIKey, settings.PianoMaxMIDIKey);

    public override void OnMessage(object _, MidiReceivedEventArgs message)
    {
        var msgType = message.Data[0];

        bool isNoteData = msgType == MidiEvent.NoteOn || msgType == MidiEvent.NoteOff;

        if (isNoteData)
        {
            byte note = message.Data[1];
            Keys.SetKey(new(note, msgType == MidiEvent.NoteOn));
        }
    }
}

public abstract class OutputDevice(string deviceName) : Device<IMidiOutput>
{
    protected readonly OutputPort port = new(deviceName);

    public abstract Task Connect();
}

public sealed class PianoLightsOutputDevice(string deviceName) : OutputDevice(deviceName)
{
    public LightState Ligths { get; private set; } = new(settings.PianoMinMIDIKey, settings.PianoMaxMIDIKey);

    private KeyboardConnectionHolder lightsHolder;

    private LightsMIDIInterface lightsInterface;

    public override async Task Connect()
    {
        if (port.IsConnected) return;

        var portDetails = await port.OpenPort();
        lightsInterface = new LightsMIDIInterface(portDetails);
        Ligths.KeyChange += OnKeyChange;
        lightsHolder = new KeyboardConnectionHolder(lightsInterface, OnLightsDisconnect);
        lightsHolder.StartLoop();
    }

    private void OnKeyChange(NoteMessage msg) => lightsInterface.SendProprietary(msg);

    public override async Task Stop()
    {
        Ligths.KeyChange -= OnKeyChange;
        lightsInterface = null;

        lightsHolder?.Dispose();
        lightsHolder = null;

        await port?.ClosePort();
    }
}
