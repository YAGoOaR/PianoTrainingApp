
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using Commons.Music.Midi;

namespace PianoTrainer.Scripts.PianoInteraction;

public record NoteMsg(byte Key, bool State);
public record TimedNoteMsg(byte Key, bool State, int DeltaTime) : NoteMsg(Key, State);

public record TimedNoteOn(byte Key, int DeltaTime);
public record TimedNote(byte Key, int DeltaTime, int Duration) : TimedNoteOn(Key, DeltaTime);
public record TimedNoteGroup(int Time, HashSet<byte> Keys);
public record ParsedMusic(List<TimedNoteGroup> Notes, int TotalTime);

public partial class MIDIReader
{
    const int defaultTempo = 500000;

    private static readonly GameSettings gameSettings = GameSettings.Instance;

    public static ParsedMusic LoadSelectedMusic(Func<byte, bool> noteFilter)
    {
        var midiMusic = LoadMIDI(gameSettings.Settings.MusicPath);

        return ParseMusic(midiMusic, noteFilter);
    }

    private static ParsedMusic ParseMusic(MidiMusic music, Func<byte, bool> keyAcceptCriteria)
    {
        var allMessages = MergeTracks(music.Tracks);

        var (keyMIDIMessages, currentTempo) = SetupMetadata(allMessages);

        var groups = keyMIDIMessages
            .Select(msg => MIDIMsgToSimpleMsg(msg, currentTempo, music.DeltaTimeSpec))
            .ToList()
            .Pipe((messages) => ExtractKeyOnMessages(messages, keyAcceptCriteria))
            .Pipe(ExtractKeyGroups)
            .Pipe((groups) => ChangeStartTime(groups, gameSettings.Settings.PlayerSettings.StartOffset))
            .Pipe(KeyGroupsToAbsTime);

        return new (groups, groups.Last().Time);
    }

    private static List<TimedNoteGroup> ExtractKeyGroups(List<TimedNote> keyMessages)
    {
        List<TimedNoteGroup> keyEvents = [];

        int eventDelay = 0;
        HashSet<byte> eventAccumulator = [];

        for (int i = 0; i < keyMessages.Count; i++)
        {
            var msg = keyMessages[i];

            if (msg.DeltaTime == 0)
            {
                eventAccumulator.Add(msg.Key);
            }
            else
            {
                if (i != 0) keyEvents.Add(new(eventDelay, eventAccumulator));
                eventAccumulator = [msg.Key];
                eventDelay = msg.DeltaTime;
            }
        }

        keyEvents.Add(new(eventDelay, eventAccumulator));
        return keyEvents;
    }

    private static List<TimedNote> ExtractKeyOnMessages(List<TimedNoteMsg> keyMessages, Func<byte, bool> keyFitCriteria)
    {
        Dictionary<byte, (int idx, int relTime, int absTime, TimedNoteMsg msg)> openedNotes = [];
        List<(int idx, TimedNote note)> closedNotes = [];

        var absoluteTime = 0;
        var relativeTime = 0;

        int counter = 0;

        foreach (var msg in keyMessages)
        {
            absoluteTime += msg.DeltaTime;
            relativeTime += msg.DeltaTime;

            if (keyFitCriteria(msg.Key))
            {
                if (msg.State)
                {
                    if (openedNotes.ContainsKey(msg.Key)) continue;

                    openedNotes[msg.Key] = (counter, relativeTime, absoluteTime, msg);
                    relativeTime = 0;
                }
                else
                {
                    if (!openedNotes.ContainsKey(msg.Key)) continue;

                    var (idx, startRelativeTime, absOpenTime, openedMsg) = openedNotes[msg.Key];

                    var duration = absoluteTime - absOpenTime;

                    (int, TimedNote) closedNote = (idx, new(openedMsg.Key, startRelativeTime, duration));

                    closedNotes.Add(closedNote);
                    openedNotes.Remove(msg.Key);
                }
            }
            counter += 1;
        }

        return closedNotes.OrderBy(x => x.idx).Select(x => x.note).ToList();
    }

    private static List<MidiMessage> MergeTracks(IList<MidiTrack> tracks)
    {
        var messageLists = tracks.Select(x => new Queue<MidiMessage>(x.Messages)).ToList();
        var counts = messageLists.Select(x => x.Count);

        var timelines = messageLists.Select(x => 0).ToList();
        var msgTimeline = 0;

        List<MidiMessage> result = [];

        while (counts.Any(x => x > 0))
        {
            var (idx, queue) = messageLists.Select((x, i) => (i, x)).OrderBy(x => x.x.Count > 0 ? timelines[x.i] + x.x.First().DeltaTime : int.MaxValue).First();

            var msg = queue.Dequeue();
            var dt = msg.DeltaTime + timelines[idx] - msgTimeline;
            var msgCorrected = new MidiMessage(dt, msg.Event);
            timelines[idx] += msg.DeltaTime;
            msgTimeline += dt;
            result.Add(msgCorrected);
            counts = messageLists.Select(x => x.Count);
        }

        return result;
    }

    private static (List<MidiMessage>, int) SetupMetadata(IEnumerable<MidiMessage> messages)
    {
        List<MidiMessage> rest = [];
        var currentTempo = defaultTempo;

        foreach (var msg in messages)
        {
            if (msg.Event.StatusByte == byte.MaxValue && msg.Event.Msb == 81)
            {
                currentTempo = MidiMetaType.GetTempo(msg.Event.ExtraData, msg.Event.ExtraDataOffset);
                Debug.WriteLine($"Set current tempo to {currentTempo}");
            }
            else if (msg.Event.EventType == MidiEvent.NoteOn || msg.Event.EventType == MidiEvent.NoteOff)
            {
                rest.Add(msg);
            }
        }
        return (rest, currentTempo);
    }

    private static TimedNoteMsg MIDIMsgToSimpleMsg(MidiMessage m, int currentTempo, short deltaTimeSpec, float tempoRatio = 1f) =>
    (
        new(
            m.Event.Msb,
            m.Event.EventType == MidiEvent.NoteOn && m.Event.Lsb != 0,
            GetContextDeltaTime(currentTempo, deltaTimeSpec, m.DeltaTime, tempoRatio)
        )
    );

    private static int GetContextDeltaTime(int currentTempo, int deltaTimeSpec, int deltaTime, float tempo_ratio = 1f)
    {
        return (int)(currentTempo * Utils.MsToSeconds * deltaTime / deltaTimeSpec / tempo_ratio);
    }

    private static List<TimedNoteGroup> KeyGroupsToAbsTime(List<TimedNoteGroup> keyEvents)
    {
        int timeAcc = 0;
        List<TimedNoteGroup> eventsAbsTime = [];

        foreach (var msg in keyEvents)
        {
            timeAcc += msg.Time;
            if (msg.Keys.Count == 0) continue;
            eventsAbsTime.Add(new(timeAcc, msg.Keys));
        }

        return eventsAbsTime;
    }

    private static MidiMusic LoadMIDI(string filename)
    {
        if (!File.Exists(filename))
        {
            throw new FileNotFoundException($"File {filename} not found.");
        }

        var music = MidiMusic.Read(File.OpenRead(filename));
        Debug.WriteLine($"Tracks count: {music.Tracks.Count}");
        return music;
    }

    private static List<TimedNoteGroup> ChangeStartTime(List<TimedNoteGroup> keyGroups, int startOffset)
    {
        if (keyGroups.Count > 0)
        {
            var (firstMsg, rest) = (keyGroups.First(), keyGroups[1..]);
            return [new(0, []), new(startOffset, firstMsg.Keys), .. rest];
        }
        return keyGroups;
    }

    private static List<TimedNoteGroup> FindKeyGroupSpan(List<TimedNoteGroup> groups, (float, float) timeRange)
    {
        return groups.SkipWhile(g => g.Time < timeRange.Item1 * Utils.SecondsToMs).TakeWhile(g => g.Time <= timeRange.Item2 * Utils.SecondsToMs).ToList();
    }
}
