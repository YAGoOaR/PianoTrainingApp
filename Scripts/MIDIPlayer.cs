
using Commons.Music.Midi;
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
    public int KeyTimeOffset { get; } = 100;
    public int StartOffset { get; } = 2000;
    public int BlinkSlowOffset { get; } = 3000;
    public int BlinkInterval { get; } = 80;
    public int BlinkSlowInterval { get; } = 200;
    public int BlinkOffset { get; } = 1000;
    public int BlinkCount { get; } = 3;
    public int BlinkOutdatedOffset { get; } = 300;

    public bool HintOnlyMode = false;

    public float tempoRatio = 1.0f;

    public (float, float)? timeRange = null;
}

public partial class MIDIPlayer
{
    private PlayerSettings settings = new();

    public PlayerSettings Settings { get => settings; }

    private PianoKeyLighting keyLightsManager;
    private KeyState piano;

    public List<SimpleTimedKeyGroup> NoteListAbsTime { get; private set; }

    public int TotalTimeMilis { get; private set; } = 0;

    public float TotalTimeSeconds { get => TotalTimeMilis / 1000f; }

    public PlayManager PlayManager { get; private set; } = new();

    private MidiMusic music;

    public void Setup(MIDIManager manager)
    {
        keyLightsManager = manager.LightsManager;
        piano = manager.Piano;

        piano.KeyChange += (k, s) => PlayManager.OnKeyChange(piano.State);

        PlayManager.OnComplete += () =>
        {
            Task.Run(async () =>
            {
                await Task.Delay(Math.Max(0, PlayManager.TimeToNextKey));
                PlayManager.NextTarget();
            });
        };

        PlayManager.OnTargetChanged += (s) =>
        {
            keyLightsManager.Reset();

            List<byte> keys = new(s.DesiredKeys);

            bool lightsEnabled = false;

            Task.Run(async () =>
            {
                await Task.Delay(Math.Max(0, PlayManager.TimeToNextKey - settings.KeyTimeOffset));
                lightsEnabled = true;
                keyLightsManager.SetKeys(keys);
            });

            if (PlayManager.TimeToNextKey < settings.BlinkOutdatedOffset) return;

            Task.Run(async () =>
            {
                await Task.Delay(Math.Max(0, PlayManager.TimeToNextKey - settings.BlinkSlowOffset));

                while (!lightsEnabled && (PlayManager.State.CurrentGroup == s.CurrentGroup))
                {
                    var interval = PlayManager.TimeToNextKey > settings.BlinkOffset 
                        ? settings.BlinkSlowInterval 
                        : settings.BlinkInterval;

                    foreach (var k in keys)
                    {
                        keyLightsManager.AddBlink(k, interval);
                    }
                    await Task.Delay(settings.BlinkInterval + interval + keyLightsManager.TickTime);
                }
            });
        };
        PlayManager.OnStopped += () => keyLightsManager.Reset();
    }

    public void LoadMIDI(string filename)
    {
        music = MidiMusic.Read(File.OpenRead(filename));
        Debug.WriteLine($"Tracks count: {music.Tracks.Count}");
    }

    private static List<SimpleTimedKeyGroup> ExtractKeyGroups(List<SimpleTimedKey> keyOnMessages)
    {
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
        return keyEvents;
    }

    private static List<SimpleTimedKeyGroup> KeyGroupsToAbsTime(List<SimpleTimedKeyGroup> keyEvents)
    {
        int timeAcc = 0;
        List<SimpleTimedKeyGroup> eventsAbsTime = [];

        foreach (var m in keyEvents)
        {
            timeAcc += m.Time;
            eventsAbsTime.Add(new(timeAcc, m.Keys));
        }

        return eventsAbsTime;
    }

    private static List<SimpleTimedKeyGroup> FindKeyGroupSpan(List<SimpleTimedKeyGroup> groups, (float, float) timeRange)
    {
        return groups.SkipWhile(g => g.Time < timeRange.Item1 * 1000).TakeWhile(g => g.Time <= timeRange.Item2 * 1000).ToList();
    }

    public void Play(PianoKeyLighting keyLightsManager, (float, float)? range = null)
    {
        if (music == null)
            throw new ArgumentException("MIDI file is not loaded.");

        var allMessages = MergeTracks(music.Tracks);

        var (keyMIDIMessages, currentTempo) = SetupMetadata(allMessages);

        var keyMessages = ChangeStartTime(
             keyMIDIMessages
                .Select(m => MIDIMsgToSimpleMsg(m, currentTempo, music.DeltaTimeSpec, settings.tempoRatio))
                .ToList(),
             settings.StartOffset
        );

        var groups = KeyGroupsToAbsTime(ExtractKeyGroups(ExtractKeyOnMessages(keyMessages, keyLightsManager.LightsState.IsAcceptable)));

        TotalTimeMilis = groups.Last().Time;

        var groupSpan = range switch
        {
            null => groups,
            (float, float) r => FindKeyGroupSpan(groups, r),
        };

        settings.timeRange = range;
        NoteListAbsTime = groupSpan;
        PlayManager.Setup(NoteListAbsTime, range is (float s, float) ? (int)(s * 1000f) : 0);
    }

    public void Process(double delta)
    {
        PlayManager.Update((float)delta);
    }
}
