
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using PianoTrainer.Scripts.PianoInteraction;

namespace PianoTrainer.Scripts.Devices;

public class PianoKeyLighting
{
    public event Action PreTick;
    private event Action<List<byte>> DesiredStateUpdate;

    private readonly LightState lightsState;

    public List<byte> _blinks = [];
    public List<byte> Blinks
    {
        get => _blinks;
        set
        {
            _blinks = value;
            DesiredStateUpdate?.Invoke(value);
        }
    }

    public List<byte> _activeNotes = [];

    public List<byte> ActiveNotes
    {
        get => _activeNotes;
        set
        {
            _activeNotes = value;
            DesiredStateUpdate?.Invoke(value);
        }
    }

    public int TickTime { get; } = 25;

    private int rollCycle = 0;

    private readonly Thread tickThread;

    public TaskCompletionSource StopSignal { get; }

    public PianoKeyLighting(LightState lights)
    {
        lightsState = lights;
        StopSignal = new();

        tickThread = new Thread(async () =>
        {
            while (!StopSignal.Task.IsCompleted)
            {
                OnTick();
                await Task.Delay(TickTime);
            }
        });
        tickThread.Start();
        DesiredStateUpdate += OnDesiredStateUpdate;
        ClearKeys();
    }

    public static IEnumerable<T> Rotate<T>(IEnumerable<T> list, int offset)
    {
        return list.Skip(offset).Concat(list.Take(offset)).ToList();
    }

    public void SetKeys(List<byte> keys)
    {
        ActiveNotes = keys;

        lock (lightsState)
        {
            foreach (byte key in keys)
            {
                lightsState.SetLight(key);
            }
        }
    }

    private void OnDesiredStateUpdate(List<byte> state) => rollCycle = 0;

    public void OnTick()
    {
        PreTick?.Invoke();

        List<byte> finalState = [.. ActiveNotes, .. Blinks];

        byte maxKeys = LightState.maxKeysDisplayed;

        if (finalState.Count > maxKeys)
        {
            List<byte> selected = [.. ActiveNotes, .. Blinks.Take(Math.Max(0, maxKeys - ActiveNotes.Count))];

            var activeLights = Rotate(selected, rollCycle).Take(maxKeys).ToList();

            lock (lightsState)
            {
                lightsState.SetMultipleLights(activeLights);
            }
            rollCycle = rollCycle >= finalState.Count ? 0 : rollCycle + 1;
        }
        else
        {
            lightsState.SetMultipleLights(finalState);
        }
    }

    public void AddBlink(byte key, int blinkTime = 50)
    {
        Blinks = [key, .. Blinks];

        Task.Run(async () =>
        {
            await Task.Delay(blinkTime);

            lock (lightsState)
            {
                Blinks = Blinks.Where(x => x != key).ToList();
            }
        });
    }

    public void Dispose()
    {
        lightsState.ResetKeys();
        StopSignal.TrySetResult();
        Reset();
    }

    public void ClearKeys() => lightsState.ResetKeys();

    public void Reset()
    {
        lightsState.ResetKeys();
        ActiveNotes = [];
        Blinks = [];
    }
}
