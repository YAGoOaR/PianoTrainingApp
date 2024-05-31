
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Godot;
using Commons.Music.Midi;
using MidiMessage = Commons.Music.Midi.MidiMessage;

namespace PianoTrainer.Scripts.PianoInteraction;
using static TimeUtils;

public record MoteMessage(byte Key, bool State);
public record TimedNoteMessage(byte Key, bool State, int DeltaTime) : MoteMessage(Key, State);

public record NotePress(byte Key, int Duration);
public record TimedNote(byte Key, int DeltaTime, int Duration) : NotePress(Key, Duration);
public record TimedNoteGroup(int Time, HashSet<NotePress> Notes);
public record ParsedMusic(List<TimedNoteGroup> Notes, int TotalTime, double Bpm);

// Loads a MIDI file, divides keys to groups and parses them into convenient records
public partial class MIDIReader
{
    private static readonly GameSettings settings = GameSettings.Instance;

    public static ParsedMusic LoadSelectedMusic(Func<byte, bool> noteFilter)
    {
        var midiMusic = LoadMIDI(settings.Settings.MusicPath);

        return ParseMusic(midiMusic, noteFilter);
    }

    private static ParsedMusic ParseMusic(MidiMusic music, Func<byte, bool> keyAcceptCriteria)
    {
        var allMessages = MergeTracks(music.Tracks);

        var (keyMIDIMessages, tempo, bpm) = SetupMetadata(allMessages);
        var beatTime = BPM2BeatTime(bpm);
        var startOffset = Mathf.RoundToInt(beatTime * settings.PlayerSettings.StartBeatsOffset * SecondsToMs);

        Debug.WriteLine(music.DeltaTimeSpec);

        var groups = keyMIDIMessages
            .Select(msg => MidiMsg2TimedNoteMsg(msg, tempo, music.DeltaTimeSpec))
            .ToList()
            .Pipe((messages) => MessagesToPressData(messages, keyAcceptCriteria))
            .Pipe(GroupNotes)
            .Pipe((groups) => AddTimePadding(groups, startOffset))
            .Pipe(ToAbsoluteTime);

        return new(groups, groups.Last().Time, bpm);
    }

    private static TimedNoteMessage MidiMsg2TimedNoteMsg(MidiMessage m, int tempo, int deltaTimeSpec) => new(
        Key: m.Event.Msb,
        State: IsNotePressed(m),
        DeltaTime: GetContextDeltaTime(tempo, deltaTimeSpec, m.DeltaTime)
    );

    private static bool IsNotePressed(MidiMessage m) => m.Event.EventType == MidiEvent.NoteOn && m.Event.Lsb != 0;

    private static List<TimedNoteGroup> GroupNotes(List<TimedNote> keyMessages)
    {
        List<TimedNoteGroup> keyEvents = [];

        int eventDelay = 0;
        HashSet<NotePress> eventAccumulator = [];

        for (int i = 0; i < keyMessages.Count; i++)
        {
            var msg = keyMessages[i];

            if (msg.DeltaTime == 0)
            {
                eventAccumulator.Add(msg);
            }
            else
            {
                if (i != 0) keyEvents.Add(new(eventDelay, eventAccumulator));
                eventAccumulator = [msg];
                eventDelay = msg.DeltaTime;
            }
        }

        keyEvents.Add(new(eventDelay, eventAccumulator));
        return keyEvents;
    }

    private static List<TimedNote> MessagesToPressData(List<TimedNoteMessage> keyMessages, Func<byte, bool> keyFitCriteria)
    {
        Dictionary<byte, (int idx, int relTime, int absTime, TimedNoteMessage msg)> openedNotes = [];
        List<(int idx, TimedNote note)> closedNotes = [];

        var absoluteTime = 0;
        var relativeTime = 0;

        for (int i = 0; i < keyMessages.Count; i++)
        {
            var msg = keyMessages[i];
            absoluteTime += msg.DeltaTime;
            relativeTime += msg.DeltaTime;

            if (keyFitCriteria(msg.Key))
            {
                if (msg.State)
                {
                    if (openedNotes.ContainsKey(msg.Key)) continue;

                    openedNotes[msg.Key] = (i, relativeTime, absoluteTime, msg);
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

    private static (List<MidiMessage>, int tempo, double bpm) SetupMetadata(IEnumerable<MidiMessage> messages)
    {
        List<MidiMessage> rest = [];
        var tempo = settings.PlayerSettings.DefaultTempo;

        foreach (var msg in messages)
        {
            if (msg.Event.StatusByte == byte.MaxValue && msg.Event.Msb == 81)
            {
                tempo = MidiMetaType.GetTempo(msg.Event.ExtraData, msg.Event.ExtraDataOffset);
                Debug.WriteLine($"Set music tempo to {tempo}.");
            }
            else if (msg.Event.EventType == MidiEvent.NoteOn || msg.Event.EventType == MidiEvent.NoteOff)
            {
                rest.Add(msg);
            }
        }

        double bpm = Tempo2BPM(tempo);

        return (rest, tempo, bpm);
    }

    private static List<TimedNoteGroup> ToAbsoluteTime(List<TimedNoteGroup> notes)
    {
        int timeAcc = 0;
        List<TimedNoteGroup> notesAbsTime = [];

        foreach (var note in notes)
        {
            timeAcc += note.Time;
            notesAbsTime.Add(new(timeAcc, note.Notes));
        }

        return notesAbsTime;
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

    private static List<TimedNoteGroup> AddTimePadding(List<TimedNoteGroup> keyGroups, int startOffset)
    {
        if (keyGroups.Count > 0)
        {
            var (firstMsg, rest) = (keyGroups.First(), keyGroups[1..]);
            var lastOffset = keyGroups.Last().Notes.Select(x => x.Duration).Max();
            return [new(0, []), new(startOffset, firstMsg.Notes), .. rest, new(lastOffset, [])];
        }
        return keyGroups;
    }
}
