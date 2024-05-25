using Godot;
using System.Collections.Generic;

namespace PianoTrainer.Scripts.GameElements;
using static TimeUtils;

public partial class BeatDrawer : Control
{
	private readonly MusicPlayer musicPlayer = MusicPlayer.Instance;
    private readonly GameSettings settings = GameSettings.Instance;
    [Export] private Color lineColor;

    private readonly List<Line2D> lines = [];

	public override void _Ready()
	{
        double beatTime = GetBeatTime(musicPlayer.Bpm);

        int timespan = settings.Settings.PlayerSettings.Timespan;
        int beatsInTimespan = Mathf.CeilToInt(timespan / beatTime);

        for (int i = 0; i < beatsInTimespan; i++)
        {
            var line = new Line2D
            {
                Points = [new Vector2(0, 0), new Vector2(0, 0)],
                ZIndex = -100,
                Width = 2,
                DefaultColor = lineColor,
            };

            lines.Add(line);
            AddChild(line);
        }
    }

    public override void _Process(double delta)
    {
        float currentTime = musicPlayer.TimeMilis * MsToSeconds;

        float beatTime = (float)GetBeatTime(musicPlayer.Bpm);

        float offsetToNextTempoLine = currentTime % beatTime;
        int timespan = settings.Settings.PlayerSettings.Timespan;

        for (int i = 0; i < lines.Count; i++)
        {
            var vPos = Size.Y - ((i+1) * beatTime - offsetToNextTempoLine) / timespan * Size.Y;

            lines[i].SetPointPosition(0, new(0, vPos));
            lines[i].SetPointPosition(1, new(Size.X, vPos));
        }
    }
}
