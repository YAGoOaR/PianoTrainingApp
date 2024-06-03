using Godot;
using System;

namespace PianoTrainer.Scripts.GameElements;

// Alerts that popup using in-game windows
public partial class Alerts : Control
{
    [Export] private Window deviceDisconnectedPanel;
    [Export] private Window waitingForDevicePanel;
    [Export] private Window pausedPanel;

    public static Alerts Instance { get; private set; }
    public override void _Ready()
    {
        Instance = this;
    }

    static Action<bool> WindowToggler(Window window) => (bool show) => window.CallDeferred(show ? Window.MethodName.Show : Window.MethodName.Hide);
    public void ShowDisconnected(bool show) => WindowToggler(deviceDisconnectedPanel)(show);
    public void ShowWaiting(bool show) => WindowToggler(waitingForDevicePanel)(show);
    public void ShowPaused(bool show) => WindowToggler(pausedPanel)(show);
}
