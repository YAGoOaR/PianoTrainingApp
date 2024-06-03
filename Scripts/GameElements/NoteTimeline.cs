
using Godot;
using System;

namespace PianoTrainer.Scripts.GameElements;

public abstract partial class NoteTimeline : PianoLayout
{
    public static MusicPlayerState PlayerState { get => musicPlayer.State; }

    [Export] private float interpolationStep = 0.2f;

    protected static readonly MusicPlayer musicPlayer = MusicPlayer.Instance;
    protected static readonly GSettings settings = GameSettings.Instance.Settings;
    protected static readonly PlayerSettings playerSettings = GameSettings.Instance.PlayerSettings;

    protected int timeSpan = 4000;
    protected float timelineOffset = 0;
    private float timeOffsetInterpolationTarget = 0;

    private int step = 500;

    public override void _Ready()
    {
        base._Ready();
        timeSpan = playerSettings.Timespan;
    }

    public override void _Process(double delta)
    {
        timelineOffset = Mathf.Lerp(timelineOffset, timeOffsetInterpolationTarget, interpolationStep);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.Pressed)
        {
            if (eventMouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                timeOffsetInterpolationTarget += step;
                Pause(true);
            }
            else if (eventMouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                timeOffsetInterpolationTarget -= step;
                Pause(true);
            }
        }
        else if (@event is InputEventKey eventKeyboard && eventKeyboard.Pressed)
        {
            if (eventKeyboard.Keycode == Key.Space && musicPlayer.PlayingState != PlayState.Playing)
            {
                Pause(false);
                timeOffsetInterpolationTarget = 0;
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
        var visionTimeStart = musicPlayer.TimeMilis + timelineOffset;
        var visionTimeEnd = visionTimeStart + timeSpan;

        return visionTimeStart <= timeMs && timeMs <= visionTimeEnd;
    }
}
