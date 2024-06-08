
using Godot;
using System.Collections.Generic;
using System.Linq;
using PianoTrainer.Scripts.PianoInteraction;
using static PianoTrainer.Scripts.PianoInteraction.PianoKeys;

namespace PianoTrainer.Scripts.GameElements;

public record Note(byte Key, Panel Rect, int Duration, float Height);
public record NoteGroup(int Time, List<Note> Notes, int MaxDuration);

// Draws notes that user has to press
public partial class FallingNotes : PianoLayout
{
    private const int NOTE_BORDER = 8;

    private static readonly MusicPlayer musicPlayer = MusicPlayer.Instance;

    [Export] private PianoKeyboard piano;
    [Export] private ProgressBar progressBar;

    [Export] private Theme fontTheme;
    [Export] private Theme themeWhiteKey;
    [Export] private Theme themeBlackKey;
    [Export] private Scroll scroll;

    [Export] private Color transparentColor = new(1f, 1f, 1f, 0.6f);

    private readonly Dictionary<int, NoteGroup> currentNotes = [];
    private readonly Dictionary<int, NoteGroup> completedNotes = [];

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

        var key = MIDIIndexToPianoKey(midiIndex);

        var black = IsBlack(key);

        var noteSizeY = duration / scroll.TimeSpan * Size.Y;

        var holder = NoteFrames[key];

        var rect = new Panel()
        {
            ZIndex = black ? ZIndex - 1 : ZIndex,
            Theme = black ? themeBlackKey : themeWhiteKey,
        };

        holder.AddChild(rect);

        rect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        rect.SetDeferred(Control.PropertyName.Size, new Vector2(holder.Size.X + NOTE_BORDER, noteSizeY));

        var txt = new Label()
        {
            Text = KeyLabelsLatin[key % KEYS_IN_OCTAVE],
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

        currentNotes[groupIndex] = new(time, newNotes, group.MaxDuration);
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

    private void DeleteNoteGroup(int groupIndex)
    {
        var noteGroup = completedNotes[groupIndex];

        foreach (var note in noteGroup.Notes)
        {
            note.Rect.QueueFree();
        }
        completedNotes.Remove(groupIndex);
    }

    private void ResetCompletedNotes()
    {
        foreach (var (key, group) in completedNotes)
        {
            foreach (var note in group.Notes)
            {
                note.Rect.QueueFree();
            }
        }
        completedNotes.Clear();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (musicPlayer.PlayingState == PlayState.Stopped) return;

        var newGroups = UpdateTimeline();
        UpdateNotes(newGroups);
        UpdateNotePositions();
    }

    private Dictionary<int, TimedNoteGroup> UpdateTimeline()
    {
        var allNoteGroups = musicPlayer.Notes;

        var currentGroup = Mathf.Max(musicPlayer.State.Group, 0);
        var currentTime = musicPlayer.TimeMilis + scroll.TimeMs;

        var selectedGroups = allNoteGroups
            .SkipWhile(g => g.Time + g.MaxDuration < currentTime)
            .TakeWhile(g => g.Time < currentTime + scroll.TimeSpan)
            .ToDictionary(el => el.Time, el => el);

        return selectedGroups;
    }

    private void UpdateNotes(Dictionary<int, TimedNoteGroup> newNotes)
    {
        var notesToAdd = newNotes.Where(g => !(currentNotes.ContainsKey(g.Key) || completedNotes.ContainsKey(g.Key)));
        foreach (var group in notesToAdd) AddNoteGroup(group.Key, group.Value);

        var currentTime = musicPlayer.TimeMilis + scroll.TimeMs;

        var notesToComplete = currentNotes.Where(pair => !newNotes.ContainsKey(pair.Key) || pair.Value.Time < currentTime);
        foreach (var group in notesToComplete) CompleteNoteGroup(group.Key);

        var notesToDelete = completedNotes.Values.Where(g => IsNoteVisible(g.Time) || !IsNoteVisible(g.Time + g.MaxDuration));
        foreach (var group in notesToDelete) DeleteNoteGroup(group.Time);
    }

    private void UpdateNotePositions()
    {
        foreach (var (_, noteGroup) in currentNotes.Concat(completedNotes))
        {
            var verticalPos = (noteGroup.Time - musicPlayer.TimeMilis - scroll.TimeMs) / scroll.TimeSpan * Size.Y;
            foreach (var note in noteGroup.Notes)
            {
                note.Rect.Position = new Vector2(0, Size.Y - verticalPos - note.Height);
            }
        }
    }

    protected bool IsNoteVisible(int timeMs)
    {
        var visionTimeStart = musicPlayer.TimeMilis + scroll.TimeMs;
        var visionTimeEnd = visionTimeStart + scroll.TimeSpan;

        return visionTimeStart <= timeMs && timeMs <= visionTimeEnd;
    }
}
