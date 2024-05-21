using Commons.Music.Midi;
using PianoTrainer.Scripts.GameElements;
using PianoTrainer.Scripts.PianoInteraction;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts.Devices;

internal class DeviceManager
{
    public PianoInputDevice DefaultPiano = new(GameSettings.Instance.Settings.PianoDeviceName);
    public PianoLightsOutputDevice DefaultLights = new(GameSettings.Instance.Settings.PianoDeviceName);

    private readonly MusicPlayer musicPlayer = MusicPlayer.Instance;

    private static DeviceManager instance;
    public static DeviceManager Instance
    {
        get
        {
            instance ??= new();
            return instance;
        }
    }

    private DeviceManager()
    {
        DefaultPiano.Piano.KeyChange += _ => musicPlayer.OnKeyChange(DefaultPiano.Piano.State);
    }

    public Task ConnectAllDevices() => Task.Run(async () =>
    {
        await DefaultPiano.Connect();
        await DefaultLights.Connect();
    });

    public static void DisconnectDevices()
    {
        Device<IMidiInput>.StopDevices();
        Device<IMidiOutput>.StopDevices();
    }
}

public abstract class Device<T> where T : IMidiPort
{
    private static readonly List<Device<T>> devices = [];

    public Device()
    {
        devices.Add(this);
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
        var portDetails = await port.OpenPort();

        portDetails.MessageReceived += OnMessage;
    });

    public override Task Stop() => port.ClosePort();
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

    private KeyboardInterface lightsInterface;

    public override Task Connect() => Task.Run(async () =>
    {
        var portDetails = await port.OpenPort();
        lightsInterface = new KeyboardInterface(portDetails);
        Ligths.KeyChange += lightsInterface.SendProprietary;
        keyLightsHolder = new KeyboardConnectionHolder(lightsInterface);
    });

    public override Task Stop()
    {
        Ligths.KeyChange -= lightsInterface.SendProprietary;
        lightsInterface = null;

        keyLightsHolder?.Dispose();
        keyLightsHolder = null;

        return port.ClosePort();
    } 
}
