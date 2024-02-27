
using CoreMidi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts.MIDI
{
    internal class KeylightHolder : IDisposable
    {
        private readonly TaskCompletionSource stopSignal;

        private const int period = 100;

        static void LightLoop(KeyLights lights, TaskCompletionSource started, TaskCompletionSource stopSignal)
        {
            try {
                lights.SendHold();
                Thread.Sleep(period);
                started.SetResult();

                while (!stopSignal.Task.IsCompleted)
                {
                    lights.SendHold();
                    Thread.Sleep(period);
                }
            }
            catch (MidiException){}
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
