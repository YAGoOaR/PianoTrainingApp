using Godot;
using System.Collections.Generic;

namespace PianoTrainer.Scripts.GameElements;
using static TimeUtils;

// Draws lines that visualize each beat on the music timeline
public partial class BeatDividedTimeline : NoteTimeline
{
    [Export] private Color lineColor;
    [Export] private int LineWidth = 2;

    private readonly List<Line2D> lines = [];

    private const int LineZIndex = -100;

    public override void _Ready()
    {
        base._Ready();
        double beatTime = BPM2BeatTime(musicPlayer.Bpm);

        int timespan = playerSettings.Timespan;
        int beatsInTimespan = Mathf.CeilToInt(timespan / beatTime);

        for (int i = 0; i < beatsInTimespan; i++)
        {
            var line = new Line2D
            {
                Points = [Vector2.Zero, Vector2.Zero],
                ZIndex = LineZIndex,
                Width = LineWidth,
                DefaultColor = lineColor,
            };

            lines.Add(line);
            AddChild(line);
        }
    }

    public override void _Process(double delta)
    {
        float currentTime = (musicPlayer.TimeMilis + timelineOffset) * MsToSeconds;

        float beatTime = (float)BPM2BeatTime(musicPlayer.Bpm);

        float offsetToNextTempoLine = currentTime % beatTime;
        int timespan = playerSettings.Timespan;

        for (int i = 0; i < lines.Count; i++)
        {
            var vPos = Size.Y - ((i + 1) * beatTime - offsetToNextTempoLine) / timespan * Size.Y;

            lines[i].SetPointPosition(0, new(0, vPos));
            lines[i].SetPointPosition(1, new(Size.X, vPos));
        }
    }
}
