
namespace PianoTrainer.Scripts.PianoInteraction;

public static class PianoKeys
{
    private static readonly GSettings settings = GameSettings.Instance.Settings;

    public const byte KEYS_IN_OCTAVE = 12;
    public const byte OCTAVE_WHITES = 7;

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


    public static bool IsBlack(byte key) => (key % KEYS_IN_OCTAVE) switch
    {
        1 or 3 or 6 or 8 or 10 => true,
        _ => false,
    };

    public static byte MIDIIndexToPianoKey(byte midiKeyByte) => (byte)(midiKeyByte - settings.PianoMinMIDIKey);

    public static byte GetClosestWhiteKey(int keyInOctave) => keyInOctave switch
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
