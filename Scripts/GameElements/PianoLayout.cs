using Godot;
using System.Collections.Generic;
using static PianoTrainer.Scripts.MusicNotes.PianoKeys;

namespace PianoTrainer.Scripts.GameElements;

// Lays out frames for piano keys
public abstract partial class PianoLayout : Control
{
    private static readonly GSettings settings = GameSettings.Instance.Settings;

    public static int KeyboardRange { get => settings.PianoKeyCount; }
    private static int WhiteKeyCount { get => KeyboardRange / KEYS_IN_OCTAVE * OCTAVE_WHITES + 1; }

    private static Vector2 BlackRatio { get; } = new(1 / 2f, 2 / 3f);

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
        var keyInOctave = key % KEYS_IN_OCTAVE;
        var octave = (key - keyInOctave) / KEYS_IN_OCTAVE;

        return octave * OCTAVE_WHITES + GetClosestWhiteKey(keyInOctave);
    }

    private static float GetOffset(byte key) => (key % KEYS_IN_OCTAVE) switch
    {
        1 or 6 => leftKeyOffset,
        3 or 10 => rightKeyOffset,
        8 => midKeyOffset,
        _ => 0
    };
}
