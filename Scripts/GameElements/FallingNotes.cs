
using Godot;
using PianoTrainer.Scripts.MusicNotes;
using System.Collections.Generic;
using System.Linq;
using static PianoTrainer.Scripts.MusicNotes.PianoKeys;

namespace PianoTrainer.Scripts.GameElements;

public record Note(byte Key, Panel Rect, int Duration, float Height);
public record NoteGroup(int Time, List<Note> Notes, int MaxDuration);

// Draws notes that user has to press
public partial class FallingNotes : PianoLayout
{
    private const int NOTE_BORDER = 8;

    private static readonly MusicPlayer musicPlayer = MusicPlayer.Instance;
    private static readonly PlayerSettings settings = GameSettings.Instance.PlayerSettings;

    [Export] private PianoKeyboard piano;
    [Export] private ProgressBar progressBar;

    [Export] private Theme fontTheme;
    [Export] private Theme themeWhiteKey;
    [Export] private Theme themeBlackKey;

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
        var noteSizeY = duration / (float)settings.TimeSpan * Size.Y;
        var black = IsBlack(key);

        var rect = new Panel()
        {
            ZIndex = black ? ZIndex - 1 : ZIndex,
            Theme = black ? themeBlackKey : themeWhiteKey,
        };

        var frame = NoteFrames[key];
        frame.AddChild(rect);

        rect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        rect.SetDeferred(Control.PropertyName.Size, new Vector2(frame.Size.X + NOTE_BORDER, noteSizeY));

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

    private void AddNotes(Dictionary<int, TimedNoteGroup> notes)
    {
        foreach (var (groupIndex, group) in notes)
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
    }

    private void HideNotes(IEnumerable<Note> notes)
    {
        foreach (var note in notes)
        {
            note.Rect.Modulate = transparentColor;
        }
    }

    private void CompleteNotes(IEnumerable<NoteGroup> groups)
    {
        foreach (var group in groups)
        {
            completedNotes[group.Time] = group;
            HideNotes(group.Notes);
            currentNotes.Remove(group.Time);
        }
    }

    private void DeleteNotes(IEnumerable<NoteGroup> groups)
    {
        foreach (var noteGroup in groups)
        {
            foreach (var note in noteGroup.Notes) note.Rect.QueueFree();
            completedNotes.Remove(noteGroup.Time);
        }
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

    private static Dictionary<int, TimedNoteGroup> UpdateTimeline()
    {
        var allNoteGroups = musicPlayer.Notes;

        var currentGroup = Mathf.Max(musicPlayer.State.Group, 0);
        var currentTime = musicPlayer.TimeMilis;

        var selectedGroups = allNoteGroups
            .SkipWhile(g => g.Time + g.MaxDuration < currentTime)
            .TakeWhile(g => g.Time < currentTime + settings.TimeSpan)
            .ToDictionary(el => el.Time, el => el);

        return selectedGroups;
    }

    private void UpdateNotes(Dictionary<int, TimedNoteGroup> newNotes)
    {
        var notesToAdd = newNotes.Where(g => !(currentNotes.ContainsKey(g.Key) || completedNotes.ContainsKey(g.Key))).ToDictionary();
        AddNotes(notesToAdd);

        var currentTime = musicPlayer.TimeMilis;

        var notesToComplete = currentNotes
            .Where(pair => !newNotes.ContainsKey(pair.Key) || pair.Value.Time < currentTime)
            .ToDictionary();

        CompleteNotes(notesToComplete.Values);

        var notesToDelete = completedNotes.Values
            .Where(g => IsNoteVisible(g.Time) || !IsNoteVisible(g.Time + g.MaxDuration));

        DeleteNotes(notesToDelete);
    }

    private void UpdateNotePositions()
    {
        foreach (var (_, noteGroup) in currentNotes.Concat(completedNotes))
        {
            var verticalPos = (noteGroup.Time - musicPlayer.TimeMilis) / settings.TimeSpan * Size.Y;
            foreach (var note in noteGroup.Notes)
            {
                note.Rect.Position = new Vector2(0, Size.Y - verticalPos - note.Height);
            }
        }
    }

    protected static bool IsNoteVisible(int timeMs)
    {
        var visionTimeStart = musicPlayer.TimeMilis;
        var visionTimeEnd = visionTimeStart + settings.TimeSpan;

        return visionTimeStart <= timeMs && timeMs <= visionTimeEnd;
    }
}
