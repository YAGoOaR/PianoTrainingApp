
namespace PianoTrainer.Scripts;

internal class TimeUtils
{
    public const float MsToSeconds = 1 / 1000f;
    public const float SecondsToMs = 1000f;

    private const int minute = 60;
    private const double MidiTempoMultiplier = 1_000_000.0;

    // MIDI tempo to beats per minute
    public static double Tempo2BPM(double tempo) => minute / tempo * MidiTempoMultiplier;

    // Beats per minute to seconds per beat
    public static double BPM2BeatTime(double beatsPerMinute) => 1 / beatsPerMinute * minute;
    public static float BPM2BeatTime(float beatsPerMinute) => 1 / beatsPerMinute * minute;

    // Transforms MIDI message time component to seconds
    public static int GetContextDeltaTime(int tempo, int deltaTimeSpec, int deltaTime) => (int)(tempo * MsToSeconds * deltaTime / deltaTimeSpec);
}
