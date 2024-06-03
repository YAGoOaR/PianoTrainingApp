
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
    protected int timelineOffset = 0;
    private int step = 500;

    public override void _Ready()
    {
        base._Ready();
        timeSpan = playerSettings.Timespan;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.Pressed)
        {
            if (eventMouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                timelineOffset += step;
                Pause(true);
            }
            else if (eventMouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                timelineOffset -= step;
                Pause(true);
            }
        }
        else if (@event is InputEventKey eventKeyboard && eventKeyboard.Pressed)
        {
            if (eventKeyboard.Keycode == Key.Space && musicPlayer.PlayingState != PlayState.Playing)
            {
                Pause(false);
                timelineOffset = 0;
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
