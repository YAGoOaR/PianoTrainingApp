
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Commons.Music.Midi;

namespace PianoTrainer.Scripts.PianoInteraction;

public record SimpleMsg(byte Key, bool State);
public record SimpleTimedMsg(byte Key, bool State, int DeltaTime) : SimpleMsg(Key, State);
public record SimpleTimedKey(byte Key, int DeltaTime);
public record SimpleTimedKeyGroup(int Time, HashSet<byte> Keys);
public record ParsedMusic(List<SimpleTimedKeyGroup> Notes, int TotalTime);

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
            .Pipe(KeyGroupsToAbsTime)
            .Pipe((groups) => ChangeStartTime(groups, gameSettings.Settings.PlayerSettings.StartOffset));

        return new (groups, groups.Last().Time);
    }

    private static List<SimpleTimedKeyGroup> ExtractKeyGroups(List<SimpleTimedKey> keyOnMessages)
    {
        List<SimpleTimedKeyGroup> keyEvents = [];

        int eventDelay = 0;
        HashSet<byte> eventAccumulator = [];

        for (int i = 0; i < keyOnMessages.Count; i++)
        {
            var msg = keyOnMessages[i];

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

    private static List<SimpleTimedKey> ExtractKeyOnMessages(List<SimpleTimedMsg> KeyMessages, Func<byte, bool> keyFitCriteria)
    {
        var t = 0;
        List<SimpleTimedKey> messagesOn = [];

        foreach (var msg in KeyMessages)
        {
            if (msg.State && keyFitCriteria(msg.Key))
            {
                messagesOn.Add(new(msg.Key, t + msg.DeltaTime));
                t = 0;
            }
            else
            {
                t += msg.DeltaTime;
            }
        }

        return messagesOn;
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

    private static SimpleTimedMsg MIDIMsgToSimpleMsg(MidiMessage m, int currentTempo, short deltaTimeSpec, float tempoRatio = 1f) =>
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

    private static List<SimpleTimedKeyGroup> KeyGroupsToAbsTime(List<SimpleTimedKeyGroup> keyEvents)
    {
        int timeAcc = 0;
        List<SimpleTimedKeyGroup> eventsAbsTime = [];

        foreach (var msg in keyEvents)
        {
            timeAcc += msg.Time;
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

    private static List<SimpleTimedKeyGroup> ChangeStartTime(List<SimpleTimedKeyGroup> keyOnMessages, int startOffset)
    {
        if (keyOnMessages.Count > 0)
        {
            var (firstMsg, rest) = (keyOnMessages.First(), keyOnMessages[1..]);
            return [new(startOffset, firstMsg.Keys), .. rest];
        }
        return keyOnMessages;
    }

    private static List<SimpleTimedKeyGroup> FindKeyGroupSpan(List<SimpleTimedKeyGroup> groups, (float, float) timeRange)
    {
        return groups.SkipWhile(g => g.Time < timeRange.Item1 * 1000).TakeWhile(g => g.Time <= timeRange.Item2 * 1000).ToList();
    }
}
