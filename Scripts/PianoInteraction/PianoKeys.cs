
namespace PianoTrainer.Scripts.PianoInteraction;

public static class PianoKeys
{
    private static readonly GSettings settings = GameSettings.Instance.Settings;

    public const byte keysInOctave = 12;
    public const byte octaveWhites = 7;

    public static readonly string[] KeyLabelsLatin = 
    [
        "C",
        "C#",
        "D",
        "D#",
        "E",
        "F",
        "F#",
        "G",
        "G#",
        "A",
        "A#",
        "B",
    ];

    public static readonly string[] KeyLabelsDo = 
    [
        "Do",
        "Di",
        "Re",
        "Ri",
        "Mi",
        "Fa",
        "Fi",
        "So",
        "Si",
        "La",
        "Li",
        "Ti",
    ];

    /// <summary>
    /// Indicates, whether a piano key is black.
    /// </summary>
    /// <param name="idx">Midi key index.</param>
    /// <returns>True if the piano key is black, otherwise false.</returns>
    public static bool IsBlack(byte idx) => (idx % keysInOctave) switch
    {
        1 or 3 or 6 or 8 or 10 => true,
        _ => false,
    };

    /// <summary>
    /// Converts piano key indices from MIDI message numeration to keyboard numeration.
    /// </summary>
    /// /// <param name="key">Midi key index.</param>
    public static byte MIDIIndexToKey(byte key) => (byte)(key - settings.PianoMinMIDIKey);

    /// <summary>
    /// Converts piano key indices from octave numeration to closest left white key numeration.
    /// </summary>
    /// /// <param name="keyInOctave">Key position in octave.</param>
    public static byte GetWhiteKeyIndex(int keyInOctave) => keyInOctave switch
    {
        0 or 1 => 0,
        2 or 3 => 1,
        4 => 2,
        5 or 6 => 3,
        7 or 8 => 4,
        9 or 10 => 5,
        11 => 6,
        _ => 0,
    };

}
