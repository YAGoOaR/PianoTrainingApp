
using Commons.Music.Midi;
using PianoTrainer.Scripts.PianoInteraction;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts.Devices;

public abstract class Device<T> where T : IMidiPort
{
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

    public Task Connect() => Task.Run(async () =>
    {
        if (port.IsConnected) return;

        var portDetails = await port.OpenPort();

        portDetails.MessageReceived += OnMessage;
    });

    public override Task Stop() => port?.ClosePort();
}

public class PianoInputDevice(string deviceName) : InputDevice(deviceName)
{
    public KeyState Piano { get; private set; } = new();

    public override void OnMessage(object _, MidiReceivedEventArgs message)
    {
        var msgType = message.Data[0];

        bool isNoteData = msgType == MidiEvent.NoteOn || msgType == MidiEvent.NoteOff;

        if (isNoteData)
        {
            byte note = message.Data[1];
            Piano.SetKey(new(note, msgType == MidiEvent.NoteOn));
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
    public LightState Ligths { get; private set; } = new();

    private KeyboardConnectionHolder keyLightsHolder;

    private LightsMIDIInterface lightsInterface;

    public override Task Connect() => Task.Run(async () =>
    {
        if (port.IsConnected) return;

        var portDetails = await port.OpenPort();
        lightsInterface = new LightsMIDIInterface(portDetails);
        Ligths.KeyChange += OnKeyChange;
        keyLightsHolder = new KeyboardConnectionHolder(lightsInterface, OnLightsDisconnect);
        keyLightsHolder.StartLoop();
    });

    private void OnKeyChange(SimpleMsg msg) => lightsInterface.SendProprietary(msg);

    public override Task Stop()
    {
        Ligths.KeyChange -= OnKeyChange;
        lightsInterface = null;

        keyLightsHolder?.Dispose();
        keyLightsHolder = null;

        return port?.ClosePort();
    }
}
