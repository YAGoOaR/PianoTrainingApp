
namespace PianoTrainer.Scripts;

internal class TimeUtils
{
    public const float MS_TO_SEC = 1 / 1000f;
    public const float SEC_TO_MS = 1000f;

    private const int MINUTE = 60;
    private const double MIDI_TEMPO_MULTIPLIER = 1_000_000.0;

    // MIDI tempo to beats per minute
    public static double Tempo2BPM(double tempo) => MINUTE / tempo * MIDI_TEMPO_MULTIPLIER;

    // Beats per minute to seconds per beat
    public static double BPM2BeatTime(double beatsPerMinute) => 1 / beatsPerMinute * MINUTE;
    public static float BPM2BeatTime(float beatsPerMinute) => 1 / beatsPerMinute * MINUTE;

    // Transforms MIDI message time component to seconds
    public static int GetContextDeltaTime(int tempo, int deltaTimeSpec, int deltaTime) => (int)(tempo * MS_TO_SEC * deltaTime / deltaTimeSpec);
}
