
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PianoTrainer.Scripts.MIDI
{
    public class KeyLightsManager : IDisposable
    {
        public event Action PreTick;
        private event Action<List<byte>> DesiredStateUpdate;

        public LightState LightsState { get; }

        public List<byte> _blinks = [];
        public List<byte> Blinks
        {
            get { return _blinks; }
            set
            {
                _blinks = value;
                DesiredStateUpdate?.Invoke(value);
            }
        }

        public List<byte> _activeNotes = [];

        public List<byte> ActiveNotes
        {
            get { return _activeNotes; }
            set
            {
                _activeNotes = value;
                DesiredStateUpdate?.Invoke(value);
            }
        }

        public HashSet<byte> tickDisplayed;
        private readonly KeylightHolder keyLightsHolder;

        private const int blinkTime = 100;
        private const int tickTime = 50;

        private int rollCycle = 0;

        private readonly Thread tickThread;

        public TaskCompletionSource StopSignal { get; }

        public KeyLightsManager(KeyLights lights)
        {
            
            LightsState = new(lights);
            tickDisplayed = new();
            StopSignal = new();

            var started = new TaskCompletionSource();

            keyLightsHolder = new KeylightHolder(lights, started);

            started.Task.Wait();

            tickThread = new Thread(() =>
            {
                while (!StopSignal.Task.IsCompleted)
                {
                    OnTick();
                    Thread.Sleep(tickTime);
                }
            });
            tickThread.Start();
            DesiredStateUpdate += OnDesiredStateUpdate;
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

            List<byte> finalState = [..ActiveNotes, ..Blinks];

            if (finalState.Count > 4)
            {
                List<byte> selected = [..ActiveNotes, ..Blinks.Take(Math.Max(0, 4 - ActiveNotes.Count))];

                var activeLights = Rotate(selected, rollCycle).Take(4).ToList();

                lock (LightsState)
                {
                    LightsState.Set4Lights(activeLights);
                }
                rollCycle = rollCycle >= finalState.Count ? 0 : rollCycle + 1;
            }
            else
            {
                LightsState.Set4Lights(finalState);
            }
        }

        public void AddBlink(byte key)
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
        }

        public void Reset()
        {
            LightsState.Reset();
            ActiveNotes = [];
            Blinks = [];
        }
    }
}
