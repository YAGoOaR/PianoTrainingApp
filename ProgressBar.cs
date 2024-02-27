using Godot;
using System;

public partial class ProgressBar : Node2D
{
    [Export]
    MIDIManager MIDIManager { get; set; }

    private ColorRect progressRect;
    private ColorRect bgRect;
    private float rectLen;
    private float rectH = 80;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        rectLen = GetViewportRect().Size.X;

        bgRect = new ColorRect()
        {
            Color = new(1/6f, 1/6f, 1/6f),
            Position = new Vector2(0, 0),
            Size = new Vector2(rectLen, rectH)
        };
        AddChild(bgRect);

        progressRect = new ColorRect()
        {
            Color = Colors.Green,
            Position = new Vector2(0, 0),
            Size = new Vector2(rectLen, rectH)
        };
        AddChild(progressRect);
    }

    public void SetProgress(float progress)
    {
        progressRect.Size = new(rectLen*progress, rectH);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        var p = MIDIManager.Player;

        if (p != null && p.TotalTimeMilis != 0)
        {
            var lastT = p.LastTimeCheck;
            var now = DateTime.Now;
            var newOffsetTime = Mathf.Min((float)(now - lastT).TotalMilliseconds, p.TimeToNextMsg);
            var currentTime = newOffsetTime + p.RelativeMessageTime;

            var progress = currentTime / p.TotalTimeMilis;
            SetProgress(progress);
        }
    }
}
