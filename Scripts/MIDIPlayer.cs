
using Commons.Music.Midi;
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static PianoTrainer.Scripts.MIDI.MidiUtils;

namespace PianoTrainer.Scripts.MIDI;

public struct PlayerSettings()
{
    public int PreBlinkCount { get; } = 1;
    public bool ShowNotes { get; } = true;
    public int KeyTimeOffset { get; } = 300;
    public int StartOffset { get; } = 3000;
    public int BlinkAnyOffset { get; } = 3000;
    public int BlinkInterval { get; } = 80;
    public int BlinkSlowInterval { get; } = 200;
    public int BlinkSlowOffset { get; } = 1000;
    public int BlinkCount { get; } = 3;
    public int BlinkOutdatedOffset { get; } = 600;

    public bool HintOnlyMode = false;

}

public partial class MIDIPlayer: Node2D
{
    private PlayerSettings settings;
    private KeyLightsManager keyLightsManager;
    private KeyState piano;

    public List<SimpleTimedKeyGroup> NoteListAbsTime { get; private set; }

    public List<SimpleTimedKeyGroup> NoteList { get; private set; }

    public int TotalTimeMilis { get; private set; } = 0;

    public PlayManager PlayManager { get; private set; }
    public TimelineManager TimelineManager { get; private set; }

    private MidiMusic music;

    public void Load(string filename)
    {
        music = MidiMusic.Read(File.OpenRead(filename));
        Debug.WriteLine($"Tracks count: {music.Tracks.Count}");
    }

    public override void _Ready()
    {
        settings = new();
        keyLightsManager = MIDIManager.Instance.LightsManager;
        piano = MIDIManager.Instance.Piano;

        PlayManager = new PlayManager();
        TimelineManager = new TimelineManager();
        piano.KeyChange += (k, s) => PlayManager.OnKeyChange(piano.State);

        PlayManager.OnComplete += () =>
        {
            Task.Run(async () =>
            {
                await Task.Delay(Math.Max(0, TimelineManager.TimeToNextKey));
                PlayManager.NextTarget();
            });
        };

        PlayManager.OnTargetChanged += (s) =>
        {
            TimelineManager.OnTargetChange(s);

            keyLightsManager.Reset();

            List<byte> keys = new(s.DesiredKeys);

            bool lightsEnabled = false;

            Task.Run(async () =>
            {
                await Task.Delay(Math.Max(0, TimelineManager.TimeToNextKey - settings.KeyTimeOffset));
                lightsEnabled = true;
                keyLightsManager.SetKeys(keys);
            });

            if (TimelineManager.TimeToNextKey < settings.BlinkOutdatedOffset) return;

            Task.Run(async () =>
            {
                await Task.Delay(Math.Max(0, TimelineManager.TimeToNextKey - settings.BlinkAnyOffset));

                while (!lightsEnabled && (PlayManager.State.CurrentMessageGroup == s.CurrentMessageGroup))
                {
                    var interval = TimelineManager.TimeToNextKey > settings.BlinkSlowOffset ? settings.BlinkSlowInterval : settings.BlinkInterval;
                    foreach (var k in keys)
                    {
                        keyLightsManager.AddBlink(k, interval);
                    }
                    await Task.Delay(settings.BlinkInterval + interval + keyLightsManager.TickTime);
                }
            });
        };

    }

    public void Play(KeyLightsManager keyLightsManager)
    {
        if (music == null)
            throw new ArgumentException("MIDI file is not loaded.");

        var allMessages = MergeTracks(music.Tracks);

        var (keyMIDIMessages, currentTempo) = SetupMetadata(allMessages);

        var keyMessages =
            keyMIDIMessages
            .Select(m => MIDIMsgToSimpleMsg(m, currentTempo, music.DeltaTimeSpec))
            .ToList();

        var keyMessages2 = ChangeStartTime(keyMessages, settings.StartOffset);

        TotalTimeMilis = keyMessages2.Select(x => x.DeltaTime).Sum();
        var keyOnMessages = ExtractKeyOnMessages(keyMessages2, keyLightsManager.LightsState.IsAcceptable);

        List<SimpleTimedKeyGroup> keyEvents = [];

        int eventDelay = 0;
        HashSet<byte> eventAccumulator = [];

        for (int i = 0; i < keyOnMessages.Count; i++)
        {
            var m = keyOnMessages[i];

            if (m.DeltaTime == 0)
            {
                eventAccumulator.Add(m.Key);
            }
            else
            {
                if (i != 0) keyEvents.Add(new(eventDelay, eventAccumulator));
                eventAccumulator = [m.Key];
                eventDelay = m.DeltaTime;
            }
        }

        keyEvents.Add(new(eventDelay, eventAccumulator));

        NoteList = keyEvents;

        int timeAcc = 0;
        List<SimpleTimedKeyGroup> eventsAbsTime = [];

        foreach (var m in keyEvents)
        {
            timeAcc += m.DeltaTime;
            eventsAbsTime.Add(new(timeAcc,m.Keys));
        }

        NoteListAbsTime = eventsAbsTime;

        PlayManager.Setup(keyEvents);
    }

    public override void _Process(double delta)
    {
        TimelineManager.Update((float)delta);
    }
}
