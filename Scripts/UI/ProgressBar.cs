
using Godot;

namespace PianoTrainer.Scripts.GameElements;

// Progress of the music flow
public partial class ProgressBar : Control
{
    private static readonly MusicPlayer musicPlayer = MusicPlayer.Instance;

    [Export] private Label progressLabel;
    [Export] private Panel progressPanel;

    private void SetProgressRectBounds(Panel rect, float tStart, float tEnd, float totalTime)
    {
        rect.Position = new(Size.X * tStart / totalTime, 0);
        rect.Size = new(Size.X * (tEnd - tStart) / totalTime, Size.Y);
    }

    public void SetProgress(float time)
    {
        SetProgressRectBounds(progressPanel, 0, time, musicPlayer.TotalSeconds);
    }

    public override void _Process(double delta)
    {
        if (musicPlayer.PlayingState == PlayState.Stopped) return;

        var time = musicPlayer.TimeMilis * TimeUtils.MS_TO_SEC;
        SetProgress(time);
        progressLabel.Text = $"{time / musicPlayer.TotalSeconds:0%}";
    }
}
