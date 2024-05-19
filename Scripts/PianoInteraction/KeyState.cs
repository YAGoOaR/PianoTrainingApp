
using System;
using System.Collections.Generic;
using System.Linq;

namespace PianoTrainer.Scripts.MIDI
{
    public class KeyState(byte minKey = 36, byte maxKey = 36 + 61)
    {
        public event Action<byte, bool> KeyChange;
        public byte MinKey { get; } = minKey;
        public byte MaxKey { get; } = maxKey;
        public HashSet<byte> State { get; } = [];

        public bool IsAcceptable(byte key) => key >= MinKey && key <= MaxKey;

        protected bool SilentSetKey(SimpleMsg keyChange)
        {
            if (!IsAcceptable(keyChange.Key))
                throw new ArgumentOutOfRangeException($"Key can't be {keyChange.Key}. Min value: {MinKey}; Max value: {MaxKey}.");

            if (keyChange.State)
                return State.Add(keyChange.Key);
            else
                return State.Remove(keyChange.Key);
        }

        public virtual bool SetKey(SimpleMsg keyChange)
        {
            if (SilentSetKey(keyChange))
            {
                KeyChange?.Invoke(keyChange.Key, keyChange.State);
                return true;
            }
            return false;
        }
    }

    public class LightState(KeyboardInterface lights) : KeyState
    {
        private readonly KeyboardInterface lights = lights;
        private Queue<byte> lightQueue = [];

        public bool UpdateNote(SimpleMsg msg)
        {
            if (base.SetKey(msg))
            {
                lights.SendProprietary(msg);
                return true;
            }
            return false;
        }

        public bool SetLight(byte keyOn)
        {
            lock (lightQueue)
            {
                while (lightQueue.Count > 4)
                {
                    var extra = lightQueue.Dequeue();
                    UpdateNote(new(extra, false));
                }
                if (lightQueue.ToHashSet().Contains(keyOn))
                {
                    return false;
                } else
                {
                    if (lightQueue.Count > 3)
                    {
                        var extra = lightQueue.Dequeue();
                        UpdateNote(new(extra, false));
                    }
                    lightQueue.Enqueue(keyOn);
                    return UpdateNote(new(keyOn, true));
                }
            }
        }

        public void Set4Lights(List<byte> keysOn)
        {
            if (keysOn.Count > 4)
            {
                throw new ArgumentException("Wrong method usage");
            }

            lock (lightQueue)
            {
                var off = lightQueue.Except(keysOn);
                var on = keysOn.Except(lightQueue);
                foreach (var key in off)
                {
                    UpdateNote(new(key, false));
                }
                foreach (var key in on)
                {
                    UpdateNote(new(key, true));
                }
                lightQueue = new(keysOn);
            }
        }

        public void RemoveKey(byte key)
        {
            if (UpdateNote(new(key, false)))
                lock (lightQueue)
                {
                    lightQueue = new(lightQueue.Where(x => x != key));
                }

        }

        public void ResetKeys()
        {
            Reset();
        }

        public void Reset()
        {
            lock (lightQueue)
            {
                while (lightQueue.Count > 0)
                {
                    var note = lightQueue.Dequeue();
                    UpdateNote(new(note, false));
                }
            }
        }
    }

}
