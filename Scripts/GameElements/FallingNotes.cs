
using Godot;
using System.Collections.Generic;
using System.Linq;
using PianoTrainer.Scripts.PianoInteraction;
using static PianoTrainer.Scripts.PianoInteraction.PianoKeys;

namespace PianoTrainer.Scripts.GameElements;
using static TimeUtils;

public partial class FallingNotes : PianoLayout
{
    private static PlayerSettings settings = GameSettings.Instance.PlayerSettings;

    [Export] private PianoKeyboard piano;
    [Export] private ProgressBar progressBar;

    [Export] private Theme fontTheme;
    [Export] private Theme themeWhiteKey;
    [Export] private Theme themeBlackKey;

    [Export] private Color transparentColor = new(1f, 1f, 1f, 0.6f);

    private Font textFont;

    private record Note(byte Key, Panel Rect, int Duration, float Height);
    private record NoteGroup(int Time, List<Note> Notes, float MaxDuration);

    private readonly Dictionary<int, NoteGroup> currentNotes = [];
    private readonly Dictionary<int, NoteGroup> completedNotes = [];

    private readonly MusicPlayer musicPlayer = MusicPlayer.Instance;

    private int timeSpan = 5;

    private int noteAdditionalWidth = 8;

    public override void _Ready()
    {
        base._Ready();
        timeSpan = settings.Timespan;
    }

    public void Clear()
    {
        foreach (var (_, noteGroup) in currentNotes)
        {
            foreach (var note in noteGroup.Notes)
            {
                note.Rect.QueueFree();
            }
        }

        currentNotes.Clear();
    }

    private Note CreateNote(NotePress note)
    {
        var (midiIndex, duration) = note;

        var key = MIDIIndexToKey(midiIndex);

        var black = IsBlack(key);

        var noteSizeY = duration * MsToSeconds / timeSpan * Size.Y;

        var holder = NoteFrames[key];

        var rect = new Panel()
        {
            ZIndex = black ? ZIndex - 1 : ZIndex,
            Theme = black ? themeBlackKey : themeWhiteKey,
        };

        holder.AddChild(rect);

        rect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        rect.Size = new(holder.Size.X + noteAdditionalWidth, noteSizeY);

        var txt = new Label()
        {
            Text = KeyLabelsLatin[key % keysInOctave],
            Theme = fontTheme,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            LayoutMode = 1,
            AutowrapMode = TextServer.AutowrapMode.Arbitrary,
            ClipText = true,
        };

        rect.AddChild(txt);
        txt.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        return new Note(key, rect, duration, noteSizeY);
    }

    private void AddNoteGroup(int groupIndex, TimedNoteGroup group)
    {
        if (currentNotes.ContainsKey(groupIndex))
        {
            throw new System.Exception("Group already exists!");
        }

        List<Note> newNotes = [];

        foreach (var k in group.Notes)
        {
            newNotes.Add(CreateNote(k));
        }

        var time = group.Time;

        currentNotes[groupIndex] = new(time, newNotes, newNotes.Select(x => x.Duration).DefaultIfEmpty(0).Max());
    }

    private void CompleteNoteGroup(int groupIndex)
    {
        var noteGroup = currentNotes[groupIndex];

        completedNotes[groupIndex] = noteGroup;

        foreach (var note in noteGroup.Notes)
        {
            note.Rect.Modulate = transparentColor;
        }

        currentNotes.Remove(groupIndex);
    }

    private void RemoveNoteGroup(int groupIndex)
    {
        var noteGroup = completedNotes[groupIndex];

        foreach (var note in noteGroup.Notes)
        {
            note.Rect.QueueFree();
        }
        completedNotes.Remove(groupIndex);
    }

    public override void _Process(double delta)
    {
        if (musicPlayer.PlayingState == PlayState.Stopped) return;

        var newGroups = UpdateTimeline();
        UpdateNotes(newGroups);
        UpdateNotePositions();
    }

    private Dictionary<int, TimedNoteGroup> UpdateTimeline()
    {
        var allNoteGroups = musicPlayer.Notes;

        var currentGroup = Mathf.Max(musicPlayer.State.CurrentGroup, 0);

        var selectedGroups = allNoteGroups
            .Skip(currentGroup)
            .TakeWhile(g => g.Time < musicPlayer.TimeMilis + timeSpan * SecondsToMs)
            .ToDictionary(el => el.Time, el => el);

        return selectedGroups;
    }

    private void UpdateNotes(Dictionary<int, TimedNoteGroup> newNotes)
    {
        var notesToAdd = newNotes.Where(g => !currentNotes.ContainsKey(g.Key));
        foreach (var group in notesToAdd) AddNoteGroup(group.Key, group.Value);

        var notesToRemove = currentNotes.Where(g => !newNotes.ContainsKey(g.Key));
        foreach (var group in notesToRemove) CompleteNoteGroup(group.Key);

        var notesToDelete = completedNotes.Values.Where(g => g.Time + g.MaxDuration <= musicPlayer.TimeMilis);
        foreach (var group in notesToDelete) RemoveNoteGroup(group.Time);
    }

    private void UpdateNotePositions()
    {
        foreach (var (_, noteGroup) in currentNotes.Concat(completedNotes))
        {
            var verticalPos = (noteGroup.Time - musicPlayer.TimeMilis) * MsToSeconds / timeSpan * Size.Y;
            foreach (var note in noteGroup.Notes)
            {
                note.Rect.Position = new Vector2(0, Size.Y - verticalPos - note.Height);
            }
        }
    }
}
