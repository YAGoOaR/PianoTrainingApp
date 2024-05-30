
using Godot;

namespace PianoTrainer.Scripts.GameElements;

public partial class ProgressBar : Control
{
    [Export] private Label progressLabel;
    [Export] private ColorRect progressRect;

    private readonly MusicPlayer musicPlayer = MusicPlayer.Instance;

    private bool active = false;

    private void SetProgressRectBounds(ColorRect rect, float tStart, float tEnd, float totalTime)
    {
        rect.Position = new(Size.X * tStart / totalTime, 0);
        rect.Size = new(Size.X * (tEnd - tStart) / totalTime, Size.Y);
    }

    public void SetProgress(float time)
    {
        SetProgressRectBounds(progressRect, 0, time, musicPlayer.TotalSeconds);
    }

    public override void _Process(double delta)
    {
        if (musicPlayer.PlayingState == PlayState.Stopped) return;

        var time = musicPlayer.TimeMilis * TimeUtils.MsToSeconds;
        SetProgress(time);
        progressLabel.Text = $"{time / musicPlayer.TotalSeconds:0%}";
    }
}
