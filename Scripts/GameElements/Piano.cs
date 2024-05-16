using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public partial class Piano : Node2D
{
    public Vector2 NoteGridSize { get; private set; }
    public Vector2 NoteSize {  get; private set; }
    public Vector2 BlackNoteSize { get; private set; }

    public float NoteGap { get; private set; } = 4;
    public int Whites { get; } = 61 - 25;

    const float bw = 1 / 2f;
    const float left = -bw * 2 / 3;
    const float mid = -bw / 2;
    const float right = -bw * 1 / 3;

    readonly List<ColorRect> noteRects = [];

    readonly Queue<(byte, bool)> changes = [];

    [Export]
    MIDIManager midiManager;

    private enum KeyType
    {
        White,
        Black,
        Missing
    }

    public static bool IsBlack(byte idx) => (idx % 12) switch
    {
        1 or 3 or 6 or 8 or 10 => true,
        _ => false,
    };

    public static byte GetWhiteIndex(byte key)
    {
        var octave = key / 12;

        var isBlack = IsBlack(key);

        var whiteKeyInOctave = key % 12 - (isBlack ? 1: 0);

        int whiteKeyPosition = 0;
        for (byte i = 0; i < whiteKeyInOctave; i++)
        {
            if (!IsBlack(i))
            {
                whiteKeyPosition++;
            }
        }

        return (byte)(octave * 7 + whiteKeyPosition);
    }

    public static (bool, float) GetNoteOffset(byte whitePos)
    {
        var pos = whitePos % 7;
        return pos switch
        {
            0 or 3 => (true, left),
            1 or 5 => (true, right),
            4 => (true, mid),
            _ => (false, 0)
        };
        
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        midiManager.Piano.KeyChange += SetKey;

		var w = GetViewportRect().Size.X / Whites;

        NoteGridSize = new(w, 250);
        NoteSize = NoteGridSize - Vector2.Right * NoteGap;
        BlackNoteSize = new(NoteSize.X / 2, NoteSize.Y * 2/3);

        Position = new(0, GetViewportRect().Size.Y);

        for (byte i = 0; i < Whites; i++)
        {
            var whiteRect = new ColorRect
            {
                Color = Colors.White,
                Position = new Vector2(w * i + NoteGap/2, -NoteSize.Y),
                Size = new Vector2(NoteSize.X, NoteSize.Y),
                ZIndex = -1
            };
            AddChild(whiteRect);
            noteRects.Add(whiteRect);

            var (blackExists, noteOffset) = GetNoteOffset(i);

            if (blackExists && i != Whites - 1)
            {
                var rect = new ColorRect()
                {
                    Color = Colors.Black,
                    Position = new Vector2(w * i + w + noteOffset * w, -NoteSize.Y),
                    Size = new Vector2(BlackNoteSize.X, BlackNoteSize.Y)
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
