
using Godot;
using System;

namespace PianoTrainer.Scripts.GameElements;
using static TimeUtils;

public partial class Scroll : Control
{
    private static readonly MusicPlayer musicPlayer = MusicPlayer.Instance;

    public float TimeMs { get; private set; } = 0;
    public float TimeSpan { get; private set; } = GameSettings.Instance.PlayerSettings.Timespan;

    [Export] private float scrollDamping = 3f;
    [Export] private float epsilon = 0.01f;
    [Export] private float minFriction = 2.5f;
    [Export] private float scrollAcceleration = 1f;

    private float scrollVeclocity = 0;

    private float ScrollFriction(float speed) => scrollDamping / (Mathf.Abs(speed) + epsilon) + minFriction;

    public override void _Process(double delta)
    {
        float deltaTime = (float)delta;
        float acceletation = scrollVeclocity * deltaTime;
        scrollVeclocity = (scrollVeclocity + acceletation) * (1 - Mathf.Min(ScrollFriction(scrollVeclocity) * deltaTime, 1));
        TimeMs += scrollVeclocity * deltaTime * SEC_TO_MS;
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
                TimeMs = 0;
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
}
