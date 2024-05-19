using Godot;
using PianoTrainer.Scripts;
using PianoTrainer.Scripts.MIDI;

public partial class ProgressBar : Control
{
    [Export]
    private MIDIManager MIDIManager { get; set; }

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

    private bool active = false;

    public override void _Ready()
    {
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

    public void SetProgress(MIDIPlayer p, float time)
    {
        SetProgressRectBounds(progressRect, 0, time, p.TotalTimeSeconds);
    }

    public void SetTimeRange(MIDIPlayer p, (float, float)? range)
    {
        if (range is (float s, float e))
        {
            SetProgressRectBounds(rangeRect, s, e, p.TotalTimeSeconds);
        }
        else
        {
            rangeRect.Size = Vector2.Zero;
            rangeRect.Position = Vector2.Zero;
        }
    }

    public void SetSelectionPreview(MIDIPlayer player, (float, float) range)
    {
        (float start, float end) = range;
        if (start > end) return;
        SetProgressRectBounds(rangeSelectRect, start, end, player.TotalTimeSeconds);
    }

    public override void _Process(double delta)
    {
        if (MIDIManager.Instance.State != MIDIManager.MIDIManagerState.Playing) return;

        var player = MIDIManager.Player;

        if (player != null && player.TotalTimeMilis != 0)
        {
            var time = player.PlayManager.TimeMilis * Utils.MilisToSecond;
            SetProgress(player, time);
            Txt.Text = $"{time:0.00}";
        }
    }
}
