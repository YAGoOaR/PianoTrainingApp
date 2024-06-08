
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using PianoTrainer.Scripts.PianoInteraction;

namespace PianoTrainer.Scripts.Devices;

// Handles hint light blinking jobs
public class PianoKeyLighting
{
    public List<byte> Blinks
    {
        get => blinks;
        set
        {
            blinks = value;
            Update?.Invoke(value);
        }
    }
    public List<byte> ActiveNotes
    {
        get => activeNotes;
        set
        {
            activeNotes = value;
            Update?.Invoke(value);
        }
    }

    private event Action<List<byte>> Update;

    private readonly LightState lights = DeviceManager.Instance.DefaultLights.Ligths;
    private readonly Thread tickThread;

    private List<byte> blinks = [];
    private List<byte> activeNotes = [];

    public int TickTime { get; } = 25;

    private int rollCycle = 0;

    private bool stop = false;

    public PianoKeyLighting()
    {
        tickThread = new Thread(async () =>
        {
            while (!stop)
            {
                OnTick();
                await Task.Delay(TickTime);
            }
        });
        tickThread.Start();
        Update += OnUpdate;
        ClearKeys();
    }

    public static IEnumerable<T> Rotate<T>(IEnumerable<T> list, int offset)
    {
        return list.Skip(offset).Concat(list.Take(offset)).ToList();
    }

    public void SetKeys(List<byte> keys)
    {
        ActiveNotes = keys;

        lock (lights)
        {
            foreach (byte key in keys)
            {
                lights.SetLight(key);
            }
        }
    }

    private void OnUpdate(List<byte> state) => rollCycle = 0;

    public void OnTick()
    {
        List<byte> finalState = [.. ActiveNotes, .. Blinks];

        byte maxKeys = LightState.maxKeysDisplayed;

        if (finalState.Count > maxKeys)
        {
            List<byte> selected = [.. ActiveNotes, .. Blinks.Take(Math.Max(0, maxKeys - ActiveNotes.Count))];

            var activeLights = Rotate(selected, rollCycle).Take(maxKeys).ToList();

            lock (lights)
            {
                lights.SetMultipleLights(activeLights);
            }
            rollCycle = rollCycle >= finalState.Count ? 0 : rollCycle + 1;
        }
        else
        {
            lights.SetMultipleLights(finalState);
        }
    }

    public async void AddBlink(byte key, int blinkTime)
    {
        Blinks = [key, .. Blinks];

        await Task.Delay(blinkTime);

        lock (lights)
        {
            Blinks = Blinks.Where(x => x != key).ToList();
        }
    }

    public void Dispose()
    {
        lights.ResetKeys();
        stop = true;
        Reset();
    }

    public void ClearKeys() => lights.ResetKeys();

    public void Reset()
    {
        lights.ResetKeys();
        ActiveNotes = [];
        Blinks = [];
    }
}
