using Godot;
using System.Collections.Generic;
using static PianoTrainer.Scripts.PianoInteraction.PianoKeys;

namespace PianoTrainer.Scripts.GameElements;

// Lays out frames for piano keys
public abstract partial class PianoLayout : Control
{
    private static readonly GSettings settings = GameSettings.Instance.Settings;

    public static int KeyboardRange { get => settings.PianoKeyCount; }
    private static int WhiteKeyCount { get => KeyboardRange / keysInOctave * octaveWhites + 1; }

    public static Vector2 BlackRatio { get; } = new(1 / 2f, 2 / 3f);

    private static readonly float leftKeyOffset = 1 - BlackRatio.X * 2 / 3;
    private static readonly float midKeyOffset = 1 - BlackRatio.X / 2;
    private static readonly float rightKeyOffset = 1 - BlackRatio.X * 1 / 3;

    protected readonly List<Control> NoteFrames = [];

    public override void _Ready()
    {
        for (byte key = 0; key < KeyboardRange; key++)
        {
            bool black = IsBlack(key);

            Control note = new() { ZIndex = black ? ZIndex : ZIndex - 1 };
            AddChild(note);

            NoteFrames.Add(note);
        }
        Resized += Resize;
    }

    private void Resize()
    {
        Vector2 WhiteSize = new(Size.X / WhiteKeyCount, Size.Y);
        Vector2 BlackSize = WhiteSize * BlackRatio;

        for (byte key = 0; key < KeyboardRange; key++)
        {
            bool black = IsBlack(key);

            var note = NoteFrames[key];

            note.Position = (GetWhiteIndex(key) + GetOffset(key)) * Vector2.Right * WhiteSize.X;
            note.Size = black ? BlackSize : WhiteSize;
        }
    }

    public static int GetWhiteIndex(byte key)
    {
        var keyInOctave = key % keysInOctave;
        var octave = (key - keyInOctave) / keysInOctave;

        return octave * octaveWhites + GetWhiteKeyIndex(keyInOctave);
    }

    private static float GetOffset(byte key) => (key % keysInOctave) switch
    {
        1 or 6 => leftKeyOffset,
        3 or 10 => rightKeyOffset,
        8 => midKeyOffset,
        _ => 0
    };
}
