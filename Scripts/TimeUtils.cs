
namespace PianoTrainer.Scripts;

internal class TimeUtils
{
    public const float MsToSeconds = 1 / 1000f;
    public const float SecondsToMs = 1000f;

    // MIDI tempo to beats per minute
    public static double Tempo2BPM(double tempo) => 60.0 / tempo * 1_000_000.0;

    // Beats per minute to seconds per beat
    public static double BPS2BeatTime(double beatsPerMinute) => 1 / beatsPerMinute * 60d;
    public static float BPS2BeatTime(float beatsPerMinute) => 1 / beatsPerMinute * 60f;

    // Transforms MIDI message time component to seconds
    public static int GetContextDeltaTime(int tempo, int deltaTimeSpec, int deltaTime) => (int)(tempo * MsToSeconds * deltaTime / deltaTimeSpec);
}
