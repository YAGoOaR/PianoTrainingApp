﻿
using Commons.Music.Midi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static PianoTrainer.Scripts.MIDI.MidiUtils;

namespace PianoTrainer.Scripts.MIDI;

// TODO: SPLIT CLASS INTO THREE PARTS
// TODO: MOVE FROM ASYNC TO GODOT UPDATE
// TODO: REFACTOR NOT TO USE THREAD SLEEP

public struct PlayerSettings()
{
    public int PreBlinkCount = 1;
    public bool ShowNotes = true;
    public int KeyTimeOffset = -400;
    public int StartOffset = 3000;
    public int BlinkOffset = 900;
    public int BlinkOutdatedOffset = 600;

}

public class MIDIPlayer(KeyState piano, KeyLightsManager lightsManager, PlayerSettings settings)
{
    public int TotalTimeMilis { get; private set; } = 0;

    public float CurrentTimeMilis {
        get {
            var newOffsetTime = Godot.Mathf.Min((float)(DateTime.Now - PressTime).TotalMilliseconds, timeToNextMsg);
            return newOffsetTime + MessageTimeAccumulator;
        }
    }

    public KeyState keyLightState = lightsManager.LightsState;

    private HashSet<byte> nonreadyKeys = [];
    private MidiMusic music;

    public int CurrentMessageIndex { get; private set; } = 0;

    public DateTime PressTime { get; private set; } = DateTime.MinValue;

    public int NextMsgRelTime { get; private set; } = 0;

    public int MessageTimeAccumulator { get; private set; } = 0;

    private int timeToNextMsg = 0;

    public MIDIPlayer(KeyState piano, KeyLightsManager lightsManager) : this(piano, lightsManager, new()) { }

    public void Load(string filename)
    {
        music = MidiMusic.Read(File.OpenRead(filename));
        Debug.WriteLine($"Tracks count: {music.Tracks.Count}");
    }

    public void KeyUpdater(byte key, bool state) => nonreadyKeys = nonreadyKeys.Intersect(piano.State).ToHashSet();

    public bool KeyFits(byte key) => lightsManager.LightsState.IsAcceptable(key);

    public void Play()
    {
        if (music == null)
            throw new ArgumentException("MIDI file is not loaded.");

        MessageTimeAccumulator = 0;
        
        CurrentMessageIndex = 0;
        PressTime = DateTime.Now;

        piano.KeyChange += KeyUpdater;

        var allMessages = MergeTracks(music.Tracks);

        var (keyMIDIMessages, currentTempo) = SetupMetadata(allMessages);

        var keyMessages = 
            keyMIDIMessages
            .Select(m => MIDIMsgToSimpleMsg(m, currentTempo, music.DeltaTimeSpec))
            .ToList();

        TotalTimeMilis = keyMessages.Select(x => x.DeltaTime).Sum();

        var keyOnMessages = ChangeStartTime(ExtractKeyOnMessages(keyMessages, KeyFits), settings.StartOffset);

        using var blinker = new Blinker(new(keyOnMessages), this, settings, lightsManager);

        NextMsgRelTime = keyOnMessages.First().DeltaTime;

        for (int i = 0; i < keyOnMessages.Count;)
        {
            CurrentMessageIndex = i;

            var selected = keyOnMessages.Skip(i).TakeWhile((m, j) => j == 0 || m.DeltaTime == 0);
            var selectedCount = selected.Count();

            var newKeys = selected.Select(x => x.Key).ToList();
            var messageDelta = selected.First().DeltaTime;

            var timeFromLastMsg = DateTime.Now - PressTime;
            var rawDurationToNextEvent = messageDelta - (int)timeFromLastMsg.TotalMilliseconds;
            var durationToNextEvent = rawDurationToNextEvent + settings.KeyTimeOffset;

            timeToNextMsg = rawDurationToNextEvent;

            Thread.Sleep(Math.Max(durationToNextEvent, 0));

            if (settings.ShowNotes)
                lightsManager.SetKeys(newKeys);

            if (newKeys.Count > 0)
            {
                var proceed = new TaskCompletionSource();

                void callback(byte key, bool state)
                {
                    if (newKeys.Except(piano.State.Except(nonreadyKeys)).Any()) return;

                    PressTime = DateTime.Now;
                    MessageTimeAccumulator += messageDelta;

                    if (keyOnMessages.Count > i + selectedCount)
                    {
                        NextMsgRelTime = MessageTimeAccumulator + keyOnMessages[i + selectedCount].DeltaTime;
                    }

                    nonreadyKeys = new(piano.State);

                    lightsManager.Reset();

                    proceed.TrySetResult();
                    piano.KeyChange -= callback;
                }

                piano.KeyChange += callback;

                proceed.Task.Wait();
            }

            i += selectedCount;
        }
        Thread.Sleep((int)CurrentTimeMilis - TotalTimeMilis);
        
        piano.KeyChange -= KeyUpdater;
    }
}
