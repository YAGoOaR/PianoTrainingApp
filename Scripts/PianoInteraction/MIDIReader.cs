
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Commons.Music.Midi;
using MidiMessage = Commons.Music.Midi.MidiMessage;
using PianoTrainer.Scripts.MIDI;
using PianoTrainer.Scripts.MusicNotes;

namespace PianoTrainer.Scripts.PianoInteraction;
using static TimeUtils;
using static MIDIUtils;

// Loads a MIDI file, divides keys to groups and parses them into convenient records
public partial class MIDIReader
{
    private static readonly GameSettings settings = GameSettings.Instance;

    public static ParsedMusic LoadSelectedMusic(Func<byte, bool> noteFilter)
    {
        var midiMusic = LoadMIDI(settings.Settings.MusicPath);

        return ParseMusic(midiMusic, noteFilter);
    }

    private static MidiMusic LoadMIDI(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"File {path} not found.");
        }

        GD.Print($"Reading {path}.");
        var music = MidiMusic.Read(File.OpenRead(path));
        GD.Print($"Tracks count: {music.Tracks.Count}");
        return music;
    }

    private static ParsedMusic ParseMusic(MidiMusic music, Func<byte, bool> keyAcceptCriteria)
    {
        var allMessages = MergeTracks(music.Tracks);

        var (keyMIDIMessages, tempo, bpm) = SetupMetadata(allMessages);
        var beatTime = BPM2BeatTime(bpm);
        var startOffset = Mathf.RoundToInt(beatTime * settings.PlayerSettings.StartBeatsOffset * SEC_TO_MS);

        var groups = keyMIDIMessages
            .Select(msg => ToTimedNotes(msg, tempo, music.DeltaTimeSpec))
            .ToList()
            .Pipe(messages => ToPressData(messages, keyAcceptCriteria))
            .Pipe(GroupNotes)
            .Pipe(groups => AddTimePadding(groups, startOffset))
            .Pipe(ToAbsoluteTime);

        var lastNotes = groups.Last();
        var totalTime = lastNotes.Time + lastNotes.MaxDuration;

        return new(groups, totalTime, bpm, beatTime);
    }

    private static List<TimedNoteGroup> GroupNotes(List<TimedNote> keyMessages)
    {
        List<TimedNoteGroup> groups = [];

        int eventDelay = 0;
        HashSet<NotePress> currentGroup = [];
        int maxDuration = 0;

        for (int i = 0; i < keyMessages.Count; i++)
        {
            var msg = keyMessages[i];

            if (msg.Time == 0)
            {
                currentGroup.Add(msg);
                maxDuration = Mathf.Max(maxDuration, msg.Duration);
            }
            else
            {
                if (i != 0) groups.Add(new(eventDelay, currentGroup, maxDuration));
                currentGroup = [msg];
                maxDuration = msg.Duration;
                eventDelay = msg.Time;
            }
        }

        groups.Add(new(eventDelay, currentGroup, maxDuration));
        return groups;
    }

    private static List<TimedNote> ToPressData(List<TimedNoteMessage> keyMessages, Func<byte, bool> keyFitCriteria)
    {
        Dictionary<byte, (int idx, int relTime, int absTime, TimedNoteMessage msg)> pressedKeys = [];
        List<(int idx, TimedNote note)> releasedKeys = [];

        var absoluteTime = 0;
        var relativeTime = 0;

        for (int i = 0; i < keyMessages.Count; i++)
        {
            var msg = keyMessages[i];
            absoluteTime += msg.Time;
            relativeTime += msg.Time;

            if (keyFitCriteria(msg.Key))
            {
                if (msg.State)
                {
                    if (pressedKeys.ContainsKey(msg.Key)) continue;

                    pressedKeys[msg.Key] = (i, relativeTime, absoluteTime, msg);
                    relativeTime = 0;
                }
                else
                {
                    if (!pressedKeys.ContainsKey(msg.Key)) continue;

                    var (idx, startRelativeTime, absOpenTime, openedMsg) = pressedKeys[msg.Key];

                    var duration = absoluteTime - absOpenTime;

                    (int, TimedNote) closedNote = (idx, new(openedMsg.Key, startRelativeTime, duration));

                    pressedKeys.Remove(msg.Key);
                    releasedKeys.Add(closedNote);
                }
            }
        }

        return releasedKeys.OrderBy(x => x.idx).Select(x => x.note).ToList();
    }

    private static List<MidiMessage> MergeTracks(IList<MidiTrack> tracks)
    {
        var messageQueues = tracks.Select(x => new Queue<MidiMessage>(x.Messages)).ToList();
        var timeAccumulators = messageQueues.Select(x => 0).ToArray();
        var mergedTimeAccumulator = 0;

        List<MidiMessage> allNotes = [];

        while (messageQueues.Any(x => x.Count > 0))
        {
            var (idx, queue) = messageQueues
                .Select((x, i) => (i, x))
                .OrderBy(
                    x => x.x.Count > 0
                        ? timeAccumulators[x.i] + x.x.First().DeltaTime
                        : int.MaxValue
                )
                .First();

            var msg = queue.Dequeue();

            var timeDelta = msg.DeltaTime + timeAccumulators[idx] - mergedTimeAccumulator;
            timeAccumulators[idx] += msg.DeltaTime;
            mergedTimeAccumulator += timeDelta;

            allNotes.Add(new(timeDelta, msg.Event));
        }

        return allNotes;
    }

    private static (List<MidiMessage>, int tempo, double bpm) SetupMetadata(IEnumerable<MidiMessage> messages)
    {
        List<MidiMessage> noteMessages = [];
        var tempo = settings.PlayerSettings.DefaultTempo;

        foreach (var msg in messages)
        {
            if (IsTempoMessage(msg))
            {
                tempo = MidiMetaType.GetTempo(msg.Event.ExtraData, msg.Event.ExtraDataOffset);
                GD.Print($"Set music tempo to {tempo}.");
            }
            else if (IsNoteMessage(msg))
            {
                noteMessages.Add(msg);
            }
        }

        double bpm = Tempo2BPM(tempo);

        return (noteMessages, tempo, bpm);
    }

    private static List<TimedNoteGroup> ToAbsoluteTime(List<TimedNoteGroup> notes)
    {
        int timeAcc = 0;
        List<TimedNoteGroup> notesAbsTime = [];

        foreach (var group in notes)
        {
            timeAcc += group.Time;
            notesAbsTime.Add(new(timeAcc, group.Notes, group.MaxDuration));
        }

        return notesAbsTime;
    }
}
