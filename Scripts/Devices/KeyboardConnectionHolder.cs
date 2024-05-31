
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts.Devices;

// Holds connection with the piano by sending corresponding message
internal class KeyboardConnectionHolder(LightsMIDIInterface lights, Action onDisconnected)
{
    private readonly TaskCompletionSource stopSignal = new();

    private const int period = 50;

    public void StartLoop()
    {
        Thread holdLoop = new(() => HoldLoop(lights, stopSignal));

        holdLoop.Start();
    }

    private void HoldLoop(LightsMIDIInterface lights, TaskCompletionSource stopSignal)
    {
        while (!stopSignal.Task.IsCompleted)
        {
            if (!lights.SendHold())
            {
                onDisconnected();
                return;
            }
            Thread.Sleep(period);
        }
    }

    public void Dispose()
    {
        stopSignal.TrySetResult();
    }
}
