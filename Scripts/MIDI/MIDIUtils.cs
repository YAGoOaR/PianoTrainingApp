
using Commons.Music.Midi;
using PianoTrainer.Scripts.MusicNotes;
using System.Collections.Generic;
using System.Linq;

namespace PianoTrainer.Scripts.MIDI;
using static TimeUtils;

public static class MIDIUtils
{
    const int MIDI_SET_TEMPO_MESSAGE = 81;

    public static bool IsNotePressed(MidiMessage m) => m.Event.EventType == MidiEvent.NoteOn && m.Event.Lsb != 0;

    public static bool IsNoteMessage(MidiMessage msg)
    {
        return msg.Event.EventType == MidiEvent.NoteOn || msg.Event.EventType == MidiEvent.NoteOff;
    }

    public static bool IsTempoMessage(MidiMessage msg)
    {
        return msg.Event.StatusByte == byte.MaxValue && msg.Event.Msb == MIDI_SET_TEMPO_MESSAGE;
    }

    public static List<MidiMessage> MergeTracks(IList<MidiTrack> tracks)
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

    public static TimedNoteMessage MIDIMessagesToNotes(MidiMessage m, int tempo, int deltaTimeSpec) => new(
        Key: m.Event.Msb,
        State: IsNotePressed(m),
        Time: GetContextDeltaTime(tempo, deltaTimeSpec, m.DeltaTime)
    );
}
