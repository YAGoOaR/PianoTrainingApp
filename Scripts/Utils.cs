
using System;

namespace PianoTrainer.Scripts;

internal static class Utils
{
    // Lets the user chain functions to map a value
    public static U Pipe<T, U>(this T input, Func<T, U> func)
    {
        return func(input);
    }

    // Makes path separators consistent
    public static string FixPath(string path)
    {
        return path.Replace("\\", "/");
    }
}
