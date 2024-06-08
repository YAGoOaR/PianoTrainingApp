using Godot;
using System.Collections.Generic;

namespace PianoTrainer.Scripts.GameElements;
using static TimeUtils;

// Draws lines that visualize each beat on the music timeline
public partial class BeatDrawer : Control
{
    private static readonly MusicPlayer musicPlayer = MusicPlayer.Instance;

    [Export] private Scroll scroll;
    [Export] private Color lineColor;
    [Export] private int LineWidth = 2;

    private readonly List<Line2D> lines = [];

    public override void _Ready()
    {
        base._Ready();

        int beatsInTimespan = Mathf.CeilToInt(scroll.TimeSpan * MS_TO_SEC / musicPlayer.BeatTime);

        for (int i = 0; i < beatsInTimespan; i++)
        {
            var line = new Line2D
            {
                Points = [Vector2.Zero, Vector2.Zero],
                ZIndex = -1,
                Width = LineWidth,
                DefaultColor = lineColor,
            };

            lines.Add(line);
            AddChild(line);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        float currentTime = (musicPlayer.TimeMilis + scroll.TimeMs) * MS_TO_SEC;

        float beatTime = (float)musicPlayer.BeatTime;

        float offsetToNextTempoLine = currentTime % beatTime;

        for (int i = 0; i < lines.Count; i++)
        {
            var vPos = Size.Y - ((i + 1) * beatTime - offsetToNextTempoLine) / (scroll.TimeSpan * MS_TO_SEC) * Size.Y;
            lines[i].SetPointPosition(0, new(0, vPos));
            lines[i].SetPointPosition(1, new(Size.X, vPos));
        }
    }
}
