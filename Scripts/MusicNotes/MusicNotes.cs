using System.Collections.Generic;

namespace PianoTrainer.Scripts.MusicNotes;

public record class NoteMessage(byte Key, bool State);
public record class TimedNoteMessage(byte Key, bool State, int Time) : NoteMessage(Key, State);
public record class NotePress(byte Key, int Duration);
public record class TimedNote(byte Key, int Time, int Duration) : NotePress(Key, Duration);
public record class TimedNoteGroup(int Time, HashSet<NotePress> Notes, int MaxDuration = 0);
public record class ParsedMusic(List<TimedNoteGroup> Notes, int TotalTime, double Bpm, double BeatTime);
