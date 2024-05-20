using Godot;
using PianoTrainer.Scripts;

public partial class ProgressBar : Control
{
    [Export]
    private RichTextLabel Txt { get; set; }

    [Export]
    private ColorRect bgRect;
    [Export]
    private ColorRect progressRect;
    [Export]
    private ColorRect rangeRect;
    [Export]
    private ColorRect rangeSelectRect;

    [Export]
    private Color RangeRectColor { get; set; } = Colors.Yellow;

    [Export]
    private Color RangeSelectRectColor { get; set; } = Colors.White;

    private GameManager gameManager;

    private bool active = false;

    public override void _Ready()
    {
        gameManager = GameManager.Instance;
        bgRect.Size = new(Size.X, Size.Y);
        progressRect.Size = new(0, Size.Y);
        rangeRect.Size = new(0, Size.Y);
        rangeSelectRect.Size = new(0, Size.Y);
    }

    private void SetProgressRectBounds(ColorRect rect, float tStart, float tEnd, float totalTime)
    {
        rect.Position = new(Size.X * tStart / totalTime, 0);
        rect.Size = new(Size.X * (tEnd - tStart) / totalTime, Size.Y);
    }

    public void SetProgress(float time)
    {
        var player = gameManager.MusicPlayer;
        SetProgressRectBounds(progressRect, 0, time, player.TotalTimeSeconds);
    }

    public void SetTimeRange((float, float)? range)
    {
        var player = gameManager.MusicPlayer;

        if (range is (float s, float e))
        {
            SetProgressRectBounds(rangeRect, s, e, player.TotalTimeSeconds);
        }
        else
        {
            rangeRect.Size = Vector2.Zero;
            rangeRect.Position = Vector2.Zero;
        }
    }

    public void SetSelectionPreview((float, float) range)
    {
        (float start, float end) = range;
        if (start > end) return;
        SetProgressRectBounds(rangeSelectRect, start, end, gameManager.MusicPlayer.TotalTimeSeconds);
    }

    public override void _Process(double delta)
    {
        if (GameManager.Instance.State != GameManager.GameState.Running) return;

        var player = gameManager.MusicPlayer;

        if (player != null && player.TotalTimeMilis != 0)
        {
            var time = player.TimeMilis * Utils.MsToSeconds;
            SetProgress(time);
            Txt.Text = $"{time:0.00}";
        }
    }
}
