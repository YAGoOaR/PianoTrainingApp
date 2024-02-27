
using Commons.Music.Midi;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PianoTrainer.Scripts.MIDI;

public record SimpleMsg(byte Key, bool State);
public record SimpleTimedMsg(byte Key, bool State, int DeltaTime) : SimpleMsg(Key, State);
public record SimpleTimedKey(byte Key, int DeltaTime);

public class MidiUtils()
{
    public static List<SimpleTimedKey> ChangeStartTime(List<SimpleTimedKey> keyOnMessages, int startOffset)
    {
        if (keyOnMessages.Count > 0)
        {
            var (firstMsg, rest) = (keyOnMessages.First(), keyOnMessages[1..]);
            return [new(firstMsg.Key, startOffset), .. rest];
        }
        return keyOnMessages;
    }

    public static int GetContextDeltaTime(int currentTempo, int deltaTimeSpec, int deltaTime, float tempo_ratio = 1f)
    {
        return (int)(currentTempo / 1000 * deltaTime / deltaTimeSpec / tempo_ratio);
    }

    // TODO: ADD CONTEXT VARIABLE

    public static SimpleTimedMsg MIDIMsgToSimpleMsg(MidiMessage m, int currentTempo, short deltaTimeSpec) =>
    (
        new(
            m.Event.Msb,
            m.Event.EventType == MidiEvent.NoteOn && m.Event.Lsb != 0,
            GetContextDeltaTime(currentTempo, deltaTimeSpec, m.DeltaTime)
        )
    );

    public static List<SimpleTimedKey> ExtractKeyOnMessages(List<SimpleTimedMsg> KeyMessages, Func<byte, bool> keyFitCriteria)
    {
        var t = 0;
        List<SimpleTimedKey> messagesOn = [];

        foreach (var msg in KeyMessages)
        {
            if (msg.State && keyFitCriteria(msg.Key))
            {
                var dT = t + msg.DeltaTime;

                messagesOn.Add(new(msg.Key, dT));
                t = 0;
            } else
            {
                t += msg.DeltaTime;
            }
        }

        return messagesOn;
    }

    public static List<MidiMessage> MergeTracks(IList<MidiTrack> tracks)
    {
        var messageLists = tracks.Select(x => new Queue<MidiMessage>(x.Messages)).ToList();
        var counts = messageLists.Select(x => x.Count);

        var timelines = messageLists.Select(x => 0).ToList();
        var msgTimeline = 0;

        List<MidiMessage> result = [];

        while (counts.Any(x => x > 0))
        {
            var (idx, q) = messageLists.Select((x, i) => (i, x)).OrderBy(x => x.x.Count > 0 ? timelines[x.i] + x.x.First().DeltaTime : int.MaxValue).First();

            var msg = q.Dequeue();
            var dt = msg.DeltaTime + timelines[idx] - msgTimeline;
            var msgCorrected = new MidiMessage(dt, msg.Event);
            timelines[idx] += msg.DeltaTime;
            msgTimeline += dt;
            result.Add(msgCorrected);
            counts = messageLists.Select(x => x.Count);
        }

        return result;
    }

}
