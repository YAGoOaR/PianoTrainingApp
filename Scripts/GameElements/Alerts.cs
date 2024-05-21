using Godot;
using System.Diagnostics;

namespace PianoTrainer.Scripts.GameElements;

public partial class Alerts : Control
{
    [Export] public Window deviceDisconnectedPanel { get; private set; }
    [Export] public Window waitingForDevicePanel { get; private set; }

    public static Alerts Instance { get; private set; }
    public override void _Ready()
    {
        Instance = this;
    }
}
