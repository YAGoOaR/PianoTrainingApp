
using Commons.Music.Midi;
using PianoTrainer.Scripts.GameElements;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts.Devices;

internal class DeviceManager
{
    public static DeviceManager Instance
    {
        get
        {
            instance ??= new();
            return instance;
        }
    }

    private static readonly GSettings settings = GameSettings.Instance.Settings;
    private static readonly MusicPlayer musicPlayer = MusicPlayer.Instance;
    private static DeviceManager instance;

    public PianoInputDevice DefaultPiano = new(settings.PianoDeviceName);
    public PianoLightsOutputDevice DefaultLights = new(settings.PianoDeviceName);

    private DeviceManager()
    {
        DefaultPiano.Keys.KeyChange += _ => musicPlayer.OnKeyChange(DefaultPiano.Keys.State);

        DefaultLights.OnDisconnect += Reconnect;
    }

    private async void Reconnect()
    {
        try
        {
            await DefaultPiano.Stop();
        }
        catch (Win32Exception)
        {
            Debug.WriteLine("Piano was already shutdown.");
        }

        Alerts.Instance?.ShowDisconnected(true);
        await ConnectAllDevices();
        Alerts.Instance?.ShowDisconnected(false);
    }

    public async Task ConnectAllDevices()
    {
        await DefaultPiano.Connect();
        await DefaultLights.Connect();
        Debug.WriteLine("Devices connected successfuly");
    }

    public static void DisconnectDevices()
    {
        Device<IMidiInput>.StopDevices();
        Device<IMidiOutput>.StopDevices();
    }
}
