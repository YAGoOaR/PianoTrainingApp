
using System.Collections.Generic;
using System.Diagnostics;

namespace PianoTrainer.Scripts
{
    internal static class Utils
    {
        public static float SecondToMilis { get; } = 1 / 1000f;

        public static float MilisToSecond { get; } = 1000f;

        public static void DoNothing() { }

        public static string SJ<T>(IEnumerable<T> lst)
        {
            return $"[{string.Join(",", lst)}]";
        }

        public static void PSJ<T>(IEnumerable<T> lst)
        {
            Debug.WriteLine(SJ(lst));
        }
    }
}
