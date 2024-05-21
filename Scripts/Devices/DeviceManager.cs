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
