
using System;

namespace PianoTrainer.Scripts;

internal static class Utils
{
    public static U Pipe<T, U>(this T input, Func<T, U> func)
    {
        return func(input);
    }

    public static string FixPath(string path)
    {
        return path.Replace("\\", "/");
    }
}
