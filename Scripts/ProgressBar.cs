using Godot;
using PianoTrainer.Scripts.MIDI;

public partial class ProgressBar : Node2D
{
    [Export]
    private MIDIManager MIDIManager { get; set; }

    [Export]
    private RichTextLabel Txt { get; set; }

    [Export]
    private Color RangeRectColor { get; set; } = Colors.Yellow;

    private ColorRect progressRect;
    private ColorRect rangeRect;
    private ColorRect bgRect;
    private float rectLen;
    private float rectH = 80;

    private (float, float)? timeRange = null;

    private bool active = false;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        rectLen = GetViewportRect().Size.X;

        bgRect = new ColorRect()
        {
            Color = new(1/6f, 1/6f, 1/6f),
            Position = new Vector2(0, 0),
            Size = new Vector2(rectLen, rectH),
            ZIndex = 10
        };
        AddChild(bgRect);

        progressRect = new ColorRect()
        {
            Color = Colors.Green,
            Position = new Vector2(0, 0),
            Size = new Vector2(0, rectH),
            ZIndex = 11
        };
        AddChild(progressRect);

        rangeRect = new ColorRect()
        {
            Color = RangeRectColor,
            Position = new Vector2(0, 0),
            Size = new Vector2(0, rectH),
            ZIndex = 12
        };
        AddChild(rangeRect);
    }

    public void SetProgress(MIDIPlayer p, float time)
    {
        var totalT = p.TotalTimeMilis / 1000f;

        if (timeRange is (float s, float e))
        {
            progressRect.Position = new(s / totalT * rectLen, 0);

            progressRect.Size = new(Mathf.Max(0, time - s) / totalT * rectLen, rectH);
        }
        else
        {
            progressRect.Position = Vector2.Zero;
            progressRect.Size = new(rectLen * time / totalT, rectH);
        }
    }

    public void SetTimeRange(MIDIPlayer p, (float, float)? range)
    {
        timeRange = range;

        if (range is (float, float) r)
        {
            var totalTime = p.TotalTimeMilis / 1000f;
            rangeRect.Size = new((r.Item2 - r.Item1) / totalTime * rectLen, rectH);
            rangeRect.Position = new(r.Item1 / totalTime * rectLen, 0);
        }
        else
        {
            rangeRect.Size = Vector2.Zero;
            rangeRect.Position = Vector2.Zero;
            return;
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (MIDIManager.Instance.State != MIDIManager.MIDIManagerState.Playing) return;

        var p = MIDIManager.Player;

        if (p != null && p.TotalTimeMilis != 0)
        {
            var t = p.TimelineManager.CurrentTimeMilis / 1000f;
            SetProgress(p, t);
            Txt.Text = $"{t:0.00}";
        }
    }
}
