using Godot;
using PianoTrainer.Scripts.MIDI;

public partial class ProgressBar : Node2D
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

    private float rectLen;
    public static float RectH { get; } = 80;

    private bool active = false;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        rectLen = GetViewportRect().Size.X;

        bgRect.Size = new(rectLen, RectH);
        progressRect.Size = new(0, RectH);
        rangeRect.Size = new(0, RectH);
        rangeSelectRect.Size = new(0, RectH);
    }

    private void SetProgressRectBounds(ColorRect rect, float tStart, float tEnd, float totalTime)
    {
        rect.Position = new(rectLen * tStart / totalTime, 0);
        rect.Size = new(rectLen * (tEnd - tStart) / totalTime, RectH);
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
            return;
        }
    }

    public void SetSelectionPreview(MIDIPlayer p, (float, float) range)
    {
        (float s, float e) = range;
        if (s > e) return;
        SetProgressRectBounds(rangeSelectRect, s, e, p.TotalTimeSeconds);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (MIDIManager.Instance.State != MIDIManager.MIDIManagerState.Playing) return;

        var p = MIDIManager.Player;

        if (p != null && p.TotalTimeMilis != 0)
        {
            var t = p.PlayManager.CurrentTimeMilis / 1000f;
            SetProgress(p, t);
            Txt.Text = $"{t:0.00}";
        }
    }
}
