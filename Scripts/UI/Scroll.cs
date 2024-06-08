
using Godot;
using System;

namespace PianoTrainer.Scripts.GameElements;
using static TimeUtils;

public partial class Scroll : Control
{
    private static readonly MusicPlayer musicPlayer = MusicPlayer.Instance;

    [Export] private float scrollDamping = 3f;
    [Export] private float epsilon = 0.01f;
    [Export] private float minFriction = 2.5f;
    [Export] private float scrollAcceleration = 1f;

    private float scrollVelocity = 0;

    private float ScrollFriction(float speed) => scrollDamping / (Mathf.Abs(speed) + epsilon) + minFriction;

    public override void _Process(double delta)
    {
        float deltaTime = (float)delta;
        float acceletation = scrollVelocity * deltaTime;
        scrollVelocity = (scrollVelocity + acceletation) * (1 - Mathf.Min(ScrollFriction(scrollVelocity) * deltaTime, 1));
        musicPlayer.ScrollMs += scrollVelocity * deltaTime * SEC_TO_MS;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.Pressed)
        {
            if (eventMouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                scrollVelocity += scrollAcceleration;
                Pause(true);
            }
            else if (eventMouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                scrollVelocity -= scrollAcceleration;
                Pause(true);
            }
        }
        else if (@event is InputEventKey eventKeyboard && eventKeyboard.Pressed)
        {
            if (eventKeyboard.Keycode == Key.Space && musicPlayer.PlayingState != PlayState.Playing)
            {
                Pause(false);
                scrollVelocity = 0;
                musicPlayer.ScrollMs = 0;
            }
        }
    }

    private static void Pause(bool pause)
    {
        bool alreadyPaused = musicPlayer.PlayingState != PlayState.Playing;
        if (pause == alreadyPaused) return;

        (pause ? (Action)musicPlayer.Pause : musicPlayer.Play)();
        Alerts.Instance.ShowPaused(pause);
    }
}
