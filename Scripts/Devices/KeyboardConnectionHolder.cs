
using Commons.Music.Midi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts.Devices;

// Holds connection with the piano by sending corresponding message
internal class KeyboardConnectionHolder(IMidiOutput lights, Action onDisconnected)
{
    private const int UPDATE_PERIOD = 50;

    private readonly TaskCompletionSource stopSignal = new();

    public void StartLoop()
    {
        Thread holdLoop = new(() => HoldLoop(lights, stopSignal));

        holdLoop.Start();
    }

    private void HoldLoop(IMidiOutput lights, TaskCompletionSource stopSignal)
    {
        while (!stopSignal.Task.IsCompleted)
        {
            if (!LightsMIDIInterface.SendHold(lights))
            {
                onDisconnected();
                return;
            }
            Thread.Sleep(UPDATE_PERIOD);
        }
    }

    public void Dispose()
    {
        stopSignal.TrySetResult();
    }
}
