
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts.MIDI
{
    internal class KeyboardConnectionHolder : IDisposable
    {
        private readonly TaskCompletionSource stopSignal;

        private const int period = 50;

        static void HoldLoop(KeyboardInterface lights, TaskCompletionSource started, TaskCompletionSource stopSignal)
        {
            lights.SendHold();
            Thread.Sleep(period);
            started.SetResult();

            while (!stopSignal.Task.IsCompleted)
            {
                lights.SendHold();
                Thread.Sleep(period);
            }
        }

        public KeyboardConnectionHolder(KeyboardInterface lights, TaskCompletionSource started)
        {
            stopSignal = new();

            Thread holdLoop = new(() => HoldLoop(lights, started, stopSignal));

            holdLoop.Start();
        }

        public void Dispose()
        {
            stopSignal.TrySetResult();
        }
    }
}
