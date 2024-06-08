
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

    public static TimedNoteMessage ToTimedNotes(MidiMessage m, int tempo, int deltaTimeSpec) => new(
        Key: m.Event.Msb,
        State: IsNotePressed(m),
        Time: GetContextDeltaTime(tempo, deltaTimeSpec, m.DeltaTime)
    );

    public static List<TimedNoteGroup> AddTimePadding(List<TimedNoteGroup> keyGroups, int startTimePadding)
    {
        if (keyGroups.Count > 0)
        {
            var (firstMsg, rest) = (keyGroups.First(), keyGroups[1..]);
            var endTimePadding = keyGroups.Last().Notes.Select(x => x.Duration).Max();
            return [
                new(0, []),
                new(startTimePadding, firstMsg.Notes, firstMsg.MaxDuration),
                .. rest,
                new(endTimePadding, [])
            ];
        }
        return keyGroups;
    }
}
