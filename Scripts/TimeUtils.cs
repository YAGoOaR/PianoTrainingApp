
namespace PianoTrainer.Scripts;

internal class TimeUtils
{
    public static float MsToSeconds { get; } = 1 / 1000f;
    public static float SecondsToMs { get; } = 1000f;

    public static double GetBeatTime(double beatsPerMinute) => 1 / beatsPerMinute * 60d;
    public static float GetBeatTime(float beatsPerMinute) => 1 / beatsPerMinute * 60f;
}
