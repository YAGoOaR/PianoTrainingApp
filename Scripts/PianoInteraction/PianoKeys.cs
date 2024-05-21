
using Godot;

namespace PianoTrainer.Scripts.PianoInteraction;

public static class PianoKeys
{
    public static Vector2 BlackNoteSizeRatio { get; } = new(1 / 2f, 2 / 3f);

    /// <summary>
    /// Key count on the entire keyboard.
    /// </summary>
    public static int KeyboardRange { get; } = 61;

    /// <summary>
    /// Black key count on the entire keyboard.
    /// </summary>
    public static int Blacks { get; } = 25;

    /// <summary>
    /// White key count on the entire keyboard.
    /// </summary>
    public static int Whites { get; } = KeyboardRange - Blacks;

    // Piano keyboard layout parameters
    public const byte octave = 12;
    public const byte octaveWhites = 7;
    public const byte MIDIIndexOffset = 36;
    public const byte defaultKeyCount = 61;

    public const float blackWidth = 1 / 2f;
    public const float leftKeyOffset = -blackWidth * 2 / 3;
    public const float midKeyOffset = -blackWidth / 2;
    public const float rightKeyOffset = -blackWidth * 1 / 3;

    /// <summary>
    /// Returns the offset of a black note on the piano keyboard. If key is not black, (false, 0) is returned.
    /// </summary>
    /// <param name="whitePos">Index of the closest white key position.</param>
    public static (bool, float) GetNoteOffset(byte whitePos)
    {
        var pos = whitePos % octaveWhites;
        return pos switch
        {
            0 or 3 => (true, leftKeyOffset),
            1 or 5 => (true, rightKeyOffset),
            4 => (true, midKeyOffset),
            _ => (false, 0)
        };
    }

    /// <summary>
    /// Indicates, whether a piano key is black.
    /// </summary>
    /// <param name="idx">Midi key index.</param>
    /// <returns>True if the piano key is black, otherwise false.</returns>
    public static bool IsBlack(byte idx) => (idx % octave) switch
    {
        1 or 3 or 6 or 8 or 10 => true,
        _ => false,
    };

    /// <summary>
    /// Returns the index of the closest key to the given one.
    /// </summary>
    /// /// <param name="key">Piano key index.</param>
    public static byte GetWhiteIndex(byte key)
    {
        var octavePos = key / octave;

        var isBlack = IsBlack(key);

        var whiteKeyInOctave = key % octave - (isBlack ? 1 : 0);

        int whiteKeyPosition = 0;
        for (byte i = 0; i < whiteKeyInOctave; i++)
        {
            if (!IsBlack(i))
            {
                whiteKeyPosition++;
            }
        }

        return (byte)(octavePos * octaveWhites + whiteKeyPosition);
    }

    /// <summary>
    /// Converts piano key indices from MIDI message numeration to keyboard numeration.
    /// </summary>
    /// /// <param name="key">Midi key index.</param>
    public static byte MIDIIndexToKey(byte key) => (byte)(key - MIDIIndexOffset);

}
