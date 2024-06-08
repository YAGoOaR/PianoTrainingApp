
using PianoTrainer.Scripts.MusicNotes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PianoTrainer.Scripts.PianoInteraction;

// Manages the pressed keys
public class KeyState(byte minKey, byte maxKey)
{
    public event Action<NoteMessage> KeyChange;
    public HashSet<byte> State { get; } = [];

    public bool HasKey(byte key) => key >= minKey && key <= maxKey;

    protected bool UpdateState(NoteMessage keyChange)
    {
        if (!HasKey(keyChange.Key)) return false;

        return keyChange.State
            ? State.Add(keyChange.Key)
            : State.Remove(keyChange.Key);
    }

    public virtual bool SetKey(NoteMessage keyChange)
    {
        if (UpdateState(keyChange))
        {
            KeyChange?.Invoke(keyChange);
            return true;
        }
        return false;
    }
}

public class LightState(byte minKey, byte maxKey) : KeyState(minKey, maxKey)
{
    private Queue<byte> lightQueue = [];
    public const byte maxKeysDisplayed = 4;

    public bool UpdateNote(NoteMessage msg) => base.SetKey(msg);

    public bool SetLight(byte keyOn)
    {
        lock (lightQueue)
        {
            while (lightQueue.Count > maxKeysDisplayed)
            {
                var extra = lightQueue.Dequeue();
                UpdateNote(new(extra, false));
            }

            if (lightQueue.ToHashSet().Contains(keyOn)) return false;

            if (lightQueue.Count >= maxKeysDisplayed)
            {
                var extra = lightQueue.Dequeue();
                UpdateNote(new(extra, false));
            }
            lightQueue.Enqueue(keyOn);

            return UpdateNote(new(keyOn, true));
        }
    }

    public void SetMultipleLights(List<byte> keysOn)
    {
        lock (lightQueue)
        {
            var extraKeys = lightQueue.Except(keysOn);
            var newKeys = keysOn.Except(lightQueue);

            foreach (var key in extraKeys) UpdateNote(new(key, false));
            foreach (var key in newKeys) UpdateNote(new(key, true));

            lightQueue = new(keysOn);
        }
    }

    public void RemoveKey(byte key)
    {
        if (!UpdateNote(new(key, false))) return;

        lock (lightQueue)
        {
            lightQueue = new(lightQueue.Where(x => x != key));
        }
    }

    public void ResetKeys()
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
