
using Godot;

namespace PianoTrainer.Scripts.GameElements;
public abstract partial class NoteTimeline : PianoLayout
{
    public MusicPlayerState PlayerState { get => musicPlayer.State; }

    protected static readonly MusicPlayer musicPlayer = MusicPlayer.Instance;
    protected static readonly GSettings settings = GameSettings.Instance.Settings;
    protected static readonly PlayerSettings playerSettings = GameSettings.Instance.PlayerSettings;

    protected int timeSpan = 5;
    protected int timelineOffset = 0;
    private int step = 500;

    public override void _Ready()
    {
        base._Ready();
        timeSpan = playerSettings.Timespan;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton emb && emb.Pressed)
        {
            if (emb.ButtonIndex == MouseButton.WheelUp)
            {
                timelineOffset += step;
            }
            else if (emb.ButtonIndex == MouseButton.WheelDown)
            {
                timelineOffset -= step;
            }
        }
    }
}
