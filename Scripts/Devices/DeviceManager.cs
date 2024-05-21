
using Godot;
using Commons.Music.Midi;
using PianoTrainer.Scripts.GameElements;
using System.Diagnostics;
using System.Threading.Tasks;
using System.ComponentModel;

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

        Alerts.Instance.deviceDisconnectedPanel.CallDeferred(Window.MethodName.Show);
        await ConnectAllDevices();
        Alerts.Instance.deviceDisconnectedPanel.CallDeferred(Window.MethodName.Hide);
    }

    public Task ConnectAllDevices() => Task.Run(async () =>
    {
        await DefaultPiano.Connect();
        await DefaultLights.Connect();
        Debug.WriteLine("Devices connected successfuly");
    });

    public static void DisconnectDevices()
    {
        Device<IMidiInput>.StopDevices();
        Device<IMidiOutput>.StopDevices();
    }
}
