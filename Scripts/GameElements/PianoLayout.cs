using Godot;
using System.Collections.Generic;
using static PianoTrainer.Scripts.PianoInteraction.PianoKeys;

namespace PianoTrainer.Scripts.GameElements;

public abstract partial class PianoLayout : Control
{
    public static int KeyboardRange { get; } = 61;

    private static int BlackKeyCount { get; } = 25;
    private static int WhiteKeyCount { get; } = KeyboardRange - BlackKeyCount;

    public static Vector2 BlackRatio { get; } = new(1 / 2f, 2 / 3f);

    private static readonly float leftKeyOffset = 1 - BlackRatio.X * 2 / 3;
    private static readonly float midKeyOffset = 1 - BlackRatio.X / 2;
    private static readonly float rightKeyOffset = 1 - BlackRatio.X * 1 / 3;

    protected readonly List<Control> NoteFrames = [];

    public override void _Ready() 
    {
        Vector2 WhiteSize = new(Size.X / WhiteKeyCount, Size.Y);
        Vector2 BlackSize = WhiteSize * BlackRatio;

        for (byte key = 0; key < KeyboardRange; key++)
        {
            bool black = IsBlack(key);

            Control note = new()
            {
                Position = (GetWhiteIndex(key) + GetOffset(key)) * Vector2.Right * WhiteSize.X,
                Size = black ? BlackSize : WhiteSize,
                ZIndex = black ? ZIndex : ZIndex - 1,
            };
            AddChild(note);

            NoteFrames.Add(note);
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
