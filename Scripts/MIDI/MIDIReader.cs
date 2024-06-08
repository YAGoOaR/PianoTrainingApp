
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Commons.Music.Midi;
using PianoTrainer.Scripts.MusicNotes;
using MidiMessage = Commons.Music.Midi.MidiMessage;

namespace PianoTrainer.Scripts.MIDI;
using static TimeUtils;
using static MIDIUtils;
using static NoteUtils;

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
            .Select(msg => MIDIMessagesToNotes(msg, tempo, music.DeltaTimeSpec))
            .ToList()
            .Pipe(messages => NoteMessageToPressData(messages, keyAcceptCriteria))
            .Pipe(GroupNotes)
            .Pipe(groups => AddTimePadding(groups, startOffset))
            .Pipe(NoteGroupsToAbsoluteTime);

        var lastNotes = groups.Last();
        var totalTime = lastNotes.Time + lastNotes.MaxDuration;

        return new(groups, totalTime, bpm, beatTime);
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
}
