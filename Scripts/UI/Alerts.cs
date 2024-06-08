using Godot;
using System;

namespace PianoTrainer.Scripts.GameElements;

// Alerts that popup using in-game windows
public partial class Alerts : Control
{
    [Export] private Panel deviceDisconnectedPanel;
    [Export] private Panel waitingForDevicePanel;
    [Export] private Panel pausedPanel;

    public static Alerts Instance { get; private set; }
    public override void _Ready()
    {
        Instance = this;
    }

    static Action<bool> WindowToggler(Panel panel) => (bool show) => panel.CallDeferred(show ? CanvasItem.MethodName.Show : CanvasItem.MethodName.Hide);
    public void ShowDisconnected(bool show) => WindowToggler(deviceDisconnectedPanel)(show);
    public void ShowWaiting(bool show) => WindowToggler(waitingForDevicePanel)(show);
    public void ShowPaused(bool show) => WindowToggler(pausedPanel)(show);
}
