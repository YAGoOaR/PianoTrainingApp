
using CoreMidi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts.MIDI
{
    internal class KeylightHolder : IDisposable
    {
        private readonly TaskCompletionSource stopSignal;

        private const int period = 50;

        // TODO: Check if using TaskCompletionSource is appropriate
        static void LightLoop(KeyLights lights, TaskCompletionSource started, TaskCompletionSource stopSignal)
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

        public KeylightHolder(KeyLights lights, TaskCompletionSource started)
        {
            stopSignal = new();

            Thread lightLoop = new(() => LightLoop(lights, started, stopSignal));

            lightLoop.Start();
        }

        public void Dispose()
        {
            stopSignal.TrySetResult();
        }
    }
}
