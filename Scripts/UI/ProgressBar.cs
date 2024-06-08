
using Godot;

namespace PianoTrainer.Scripts.GameElements;

// Progress of the music flow
public partial class ProgressBar : Control
{
    private static readonly MusicPlayer musicPlayer = MusicPlayer.Instance;

    [Export] private Label progressLabel;
    [Export] private Panel progressPanel;

    public override void _Process(double delta)
    {
        if (musicPlayer.PlayingState == PlayState.Stopped) return;

        var time = musicPlayer.TimeMilis * TimeUtils.MS_TO_SEC;

        float progress = Mathf.Clamp(time / musicPlayer.TotalSeconds, 0, 1);

        progressPanel.Size = new(Size.X * progress, Size.Y);
        progressLabel.Text = $"{progress:0%}";
    }
}
