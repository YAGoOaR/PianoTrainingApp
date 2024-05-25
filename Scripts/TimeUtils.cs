
namespace PianoTrainer.Scripts;

internal class TimeUtils
{
    public const float MsToSeconds = 1 / 1000f;
    public const float SecondsToMs = 1000f;

    public static double Tempo2BPM(double tempo) => 60.0 / tempo * 1_000_000.0;
    public static double BPS2BeatTime(double beatsPerMinute) => 1 / beatsPerMinute * 60d;
    public static float BPS2BeatTime(float beatsPerMinute) => 1 / beatsPerMinute * 60f;

    public static int GetContextDeltaTime(int tempo, int deltaTimeSpec, int deltaTime) => (int)(tempo * MsToSeconds * deltaTime / deltaTimeSpec);
}
