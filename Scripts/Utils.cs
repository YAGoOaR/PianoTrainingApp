
using System;

namespace PianoTrainer.Scripts;

internal static class Utils
{
    public static float MsToSeconds { get; } = 1 / 1000f;

    public static float SecondsToMs { get; } = 1000f;

    public static U Pipe<T, U>(this T input, Func<T, U> func)
    {
        return func(input);
    }

    public static string FixPath(string path)
    {
        return path.Replace("\\", "/");
    }
}
