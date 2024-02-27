using Godot;
using PianoTrainer.Scripts.MIDI;
using System.Collections.Generic;
using System.Diagnostics;

public partial class Piano : Node2D
{
	private Vector2 noteSize;
    Vector2 blackNoteSize;

    List<ColorRect> noteRects = [];

    Queue<(byte, bool)> changes = [];

    [Export]
    MIDIManager midiManager;

    private enum KeyType
    {
        White,
        Black,
        Missing
    }

    private bool IsBlack(byte idx) => (idx % 12) switch
    {
        1 or 3 or 6 or 8 or 10 => true,
        _ => false,
    };

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        midiManager.KeyPressed += SetKey;

        var whites = 61 - 25;

		var w = GetViewportRect().Size.X / whites;
        var bw = w / 2;

        noteSize = new (w-3, 250);
        blackNoteSize = new(noteSize.X / 2, noteSize.Y * 2/3);

        Vector2 left = new(-bw*2/3, 0);
        Vector2 mid = new(-bw/2, 0);
        Vector2 right = new(-bw*1/3, 0);

        Position = new(0, GetViewportRect().Size.Y);

        for (int i = 0; i < whites; i++)
        {
            var whiteRect = new ColorRect
            {
                Color = Colors.White,
                Position = new Vector2(w * i, -noteSize.Y),
                Size = new Vector2(noteSize.X, noteSize.Y),
                ZIndex = -1
            };
            AddChild(whiteRect);
            noteRects.Add(whiteRect);

            var pos = i % 7;

            var (blackExists, offset) = pos switch
            {
                0 or 3 => (true, left),
                1 or 5 => (true, right),
                4 => (true, mid),
                _ => (false, Vector2.Zero)
            };

            if (blackExists && i != whites - 1)
            {
                var rect = new ColorRect()
                {
                    Color = Colors.Black,
                    Position = new Vector2(w * i + noteSize.X + offset.X, -noteSize.Y),
                    Size = new Vector2(blackNoteSize.X, blackNoteSize.Y)
                };

                AddChild(rect);
                noteRects.Add(rect);
            }
        }
	}

    public void SetKey(byte key, bool state) => changes.Enqueue((key, state));


    public override void _Process(double delta)
    {
        while (changes.Count > 0)
        {
            var (key, state) = changes.Dequeue();

            byte k = (byte)(key - 36);
            noteRects[k].Color = state ? Colors.Red : (IsBlack(k) ? Colors.Black : Colors.White);
        }
    }
}
