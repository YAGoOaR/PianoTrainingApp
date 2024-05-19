
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PianoTrainer.Scripts.MIDI
{
    public class PianoKeyLighting : IDisposable
    {
        public event Action PreTick;
        private event Action<List<byte>> DesiredStateUpdate;

        public LightState LightsState { get; }

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

        private readonly KeyboardConnectionHolder keyLightsHolder;

        public int TickTime { get; } = 25;

        private int rollCycle = 0;

        private readonly Thread tickThread;

        public TaskCompletionSource StopSignal { get; }

        public PianoKeyLighting(KeyboardInterface lights)
        {
            LightsState = new(lights);
            StopSignal = new();

            var started = new TaskCompletionSource();

            keyLightsHolder = new KeyboardConnectionHolder(lights, started);

            started.Task.Wait();

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

            lock (LightsState)
            {
                foreach (byte key in keys)
                {
                    LightsState.SetLight(key);
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

                lock (LightsState)
                {
                    LightsState.SetMultipleLights(activeLights);
                }
                rollCycle = rollCycle >= finalState.Count ? 0 : rollCycle + 1;
            }
            else
            {
                LightsState.SetMultipleLights(finalState);
            }
        }

        public void AddBlink(byte key, int blinkTime = 50)
        {
            Blinks = [key, .. Blinks];

            Task.Run(async () =>
            {
                await Task.Delay(blinkTime);

                lock (LightsState)
                {
                    Blinks = Blinks.Where(x => x != key).ToList();
                }
            });
        }

        public void Dispose()
        {
            LightsState.Reset();
            StopSignal.TrySetResult();
            keyLightsHolder.Dispose();
            ClearKeys();
        }

        public void ClearKeys() => LightsState.ResetKeys();

        public void Reset()
        {
            LightsState.Reset();
            ActiveNotes = [];
            Blinks = [];
        }
    }
}
