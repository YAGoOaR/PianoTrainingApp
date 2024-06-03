
using Godot;
using System;

namespace PianoTrainer.Scripts.GameElements;

public abstract partial class NoteTimeline : PianoLayout
{
    public static MusicPlayerState PlayerState { get => musicPlayer.State; }

    protected static readonly MusicPlayer musicPlayer = MusicPlayer.Instance;
    protected static readonly GSettings settings = GameSettings.Instance.Settings;
    protected static readonly PlayerSettings playerSettings = GameSettings.Instance.PlayerSettings;

    protected int timeSpan = 4000;
    protected float scroll = 0;
    private float scrollVeclocity = 0;

    private const float scrollDamping = 1400f;
    private const float epsilon = 0.01f;
    private const float minFriction = 2.5f;
    private const float scrollAcceleration = 1000f;

    public override void _Ready()
    {
        base._Ready();
        timeSpan = playerSettings.Timespan;
    }

    private static float ScrollFriction(float speed) => scrollDamping / (Mathf.Abs(speed) + epsilon) + minFriction;

    public override void _Process(double delta)
    {
        float deltaTime = (float)delta;
        float acceletation = scrollVeclocity * deltaTime;
        scrollVeclocity = (scrollVeclocity + acceletation) * (1 - Mathf.Min(ScrollFriction(scrollVeclocity) * deltaTime, 1));
        scroll += scrollVeclocity * deltaTime;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.Pressed)
        {
            if (eventMouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                scrollVeclocity += scrollAcceleration;
                Pause(true);
            }
            else if (eventMouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                scrollVeclocity -= scrollAcceleration;
                Pause(true);
            }
        }
        else if (@event is InputEventKey eventKeyboard && eventKeyboard.Pressed)
        {
            if (eventKeyboard.Keycode == Key.Space && musicPlayer.PlayingState != PlayState.Playing)
            {
                Pause(false);
                scrollVeclocity = 0;
            }
        }
    }

    private static void Pause(bool pause)
    {
        bool pauseState = musicPlayer.PlayingState != PlayState.Playing;
        if (pause == pauseState) return;

        (pause ? (Action)musicPlayer.Pause : musicPlayer.Play)();
        Alerts.Instance.ShowPaused(pause);
    }

    protected bool IsNoteVisible(int timeMs)
    {
        var visionTimeStart = musicPlayer.TimeMilis + scroll;
        var visionTimeEnd = visionTimeStart + timeSpan;

        return visionTimeStart <= timeMs && timeMs <= visionTimeEnd;
    }
}
