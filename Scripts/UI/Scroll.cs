
using Godot;
using System;

namespace PianoTrainer.Scripts.GameElements;
using static TimeUtils;

public class Scroll
{
    public float TimeMs { get; private set; } = 0;

    public static MusicPlayerState PlayerState { get => musicPlayer.State; }

    private static readonly MusicPlayer musicPlayer = MusicPlayer.Instance;

    private float scrollVeclocity = 0;

    private const float scrollDamping = 3f;
    private const float epsilon = 0.01f;
    private const float minFriction = 2.5f;
    private const float scrollAcceleration = 1f;

    private static float ScrollFriction(float speed) => scrollDamping / (Mathf.Abs(speed) + epsilon) + minFriction;

    public void Update(double delta)
    {
        float deltaTime = (float)delta;
        float acceletation = scrollVeclocity * deltaTime;
        scrollVeclocity = (scrollVeclocity + acceletation) * (1 - Mathf.Min(ScrollFriction(scrollVeclocity) * deltaTime, 1));
        TimeMs += scrollVeclocity * deltaTime * SecondsToMs;
    }

    public void OnInput(InputEvent @event)
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
