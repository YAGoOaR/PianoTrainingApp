
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts.Devices;

internal class KeyboardConnectionHolder
{
    private readonly TaskCompletionSource stopSignal;

    private const int period = 50;

    static void HoldLoop(KeyboardInterface lights, TaskCompletionSource stopSignal)
    {
        lights.SendHold();
        Thread.Sleep(period);

        while (!stopSignal.Task.IsCompleted)
        {
            lights.SendHold();
            Thread.Sleep(period);
        }
    }

    public KeyboardConnectionHolder(KeyboardInterface lights)
    {
        stopSignal = new();

        Thread holdLoop = new(() => HoldLoop(lights, stopSignal));

        holdLoop.Start();
    }

    public void Dispose()
    {
        stopSignal.TrySetResult();
    }
}
