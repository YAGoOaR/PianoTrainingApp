
using Godot;
using System.Collections.Generic;
using System.Linq;
using PianoTrainer.Scripts.PianoInteraction;
using static PianoTrainer.Scripts.PianoInteraction.PianoKeys;

namespace PianoTrainer.Scripts.GameElements;
using static Utils;

public partial class FallingNotes : Control
{
    [Export] private PianoKeyboard piano;
    [Export] private ProgressBar progressBar;
    [Export] private float timeSpan = 3;

    [Export] private Theme fontTheme;
    [Export] private Theme themeWhiteKey;
    [Export] private Theme themeBlackKey;

    private const float noteHeight = 100;
    private Font textFont;

    private record Note(byte Key, Panel Rect);
    private record NoteGroup(int Time, List<Note> Notes);

    private readonly Dictionary<int, NoteGroup> currentNotes = [];

    private readonly MusicPlayer musicPlayer = MusicPlayer.Instance;

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

    private Panel CreateNote(byte key)
    {
        var isBlack = IsBlack(key);

        Vector2 WhiteNoteSize = new(piano.WhiteNoteSize.X, noteHeight);
        Vector2 BlackNoteSize = new(piano.BlackNoteSize.X, noteHeight);

        var rect = new Panel()
        {
            Size = (isBlack ? BlackNoteSize : WhiteNoteSize) + Vector2.Right * 8,
            ZIndex = -1,
            Theme = isBlack ? themeBlackKey : themeWhiteKey,
        };

        AddChild(rect);

        var txt = new Label()
        {
            Text = KeyLabelsLatin[key % octave],
            Theme = fontTheme,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            LayoutMode = 1,
            AutowrapMode = TextServer.AutowrapMode.Arbitrary,
            ClipText = true,
        };

        rect.AddChild(txt);
        txt.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        return rect;
    }

    private void AddNoteGroup(int groupIndex, TimedNoteGroup group)
    {
        if (currentNotes.ContainsKey(groupIndex))
        {
            throw new System.Exception("Group already exists!");
        }

        List<Note> newNotes = [];

        foreach (var k in group.Keys)
        {
            var rect = CreateNote(k);

            newNotes.Add(new Note(k, rect));
        }

        var time = group.Time;

        currentNotes[groupIndex] = new(time, newNotes);
    }

    public void RemoveNoteGroup(int groupIndex)
    {
        var noteGroup = currentNotes[groupIndex];

        foreach (var note in noteGroup.Notes)
        {
            note.Rect.QueueFree();
        }

        currentNotes.Remove(groupIndex);
    }

    public override void _Process(double delta)
    {
        UpdateTimeline();
        UpdateNotePositions();
    }

    private void UpdateTimeline()
    {
        if (musicPlayer.PlayingState == MusicPlayer.PlayState.Stopped) return;

        var (allNoteGroups, timeline) = (musicPlayer.Notes, musicPlayer);

        var currentGroup = Mathf.Max(timeline.State.CurrentGroup, 0);

        var selectedGroups = allNoteGroups
            .Skip(currentGroup)
            .TakeWhile(g => g.Time < timeline.TimeMilis + timeSpan * SecondsToMs)
            .ToDictionary(el => el.Time, el => el);

        UpdateNotes(selectedGroups);
    }

    private void UpdateNotes(Dictionary<int, TimedNoteGroup> newNotes)
    {
        var notesToAdd = newNotes.Where(g => !currentNotes.ContainsKey(g.Key));
        foreach (var g in notesToAdd) AddNoteGroup(g.Key, g.Value);

        var notesToRemove = currentNotes.Where(ng => !newNotes.ContainsKey(ng.Key));
        foreach (var g in notesToRemove) RemoveNoteGroup(g.Key);
    }

    private void UpdateNotePositions()
    {
        foreach (var (_, noteGroup) in currentNotes)
        {
            var verticalPos = (noteGroup.Time - musicPlayer.TimeMilis) * MsToSeconds / timeSpan * Size.Y;
            foreach (var note in noteGroup.Notes)
            {
                var keyPos = MIDIIndexToKey(note.Key);
                var whiteIndex = GetWhiteIndex(keyPos);

                var (_, noteOffset) = GetNoteOffset(whiteIndex);

                var totalOffset = IsBlack(note.Key)
                    ? (noteOffset * piano.GridSize.X + piano.GridSize.X + piano.BlackNoteSize.X / 2)
                    : (piano.NoteGap / 2 + piano.GridSize.X / 2);

                note.Rect.Position = new Vector2(whiteIndex * (Size.X / Whites) + totalOffset, Size.Y - verticalPos - noteHeight / 2) - note.Rect.Size/2;
            }
        }
    }
}
