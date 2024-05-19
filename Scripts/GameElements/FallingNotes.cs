using Godot;
using PianoTrainer.Scripts;
using PianoTrainer.Scripts.MIDI;
using System.Collections.Generic;
using System.Linq;
using static PianoKeyManager;
using static PianoTrainer.Scripts.Utils;

public partial class FallingNotes : Control
{
    [Export] private CompressedTexture2D whiteNoteTexture;
    [Export] private CompressedTexture2D blackNoteTexture;
    [Export] private PianoKeyboard piano;
    [Export] private ProgressBar progressBar;
    [Export] private float noteHeight = 50;
    [Export] private float timeSpan = 3;
    [Export] private float noteTextureScale = 200f;

    private GameManager gameManager;

    private record Note(byte Key, Sprite2D rect);
    private record NoteGroup(int Time, List<Note> notes);

    private readonly Dictionary<int, NoteGroup> currentNotes = [];

    public override void _Ready()
    {
        gameManager = GameManager.Instance;
    }

    public void Clear()
    {
        foreach (var (_, noteGroup) in currentNotes)
        {
            foreach (var note in noteGroup.notes)
            {
                note.rect.QueueFree();
            }
        }

        currentNotes.Clear();
    }

    public void AddNoteGroup(int groupIndex, SimpleTimedKeyGroup group)
    {
        if (currentNotes.ContainsKey(groupIndex))
        {
            throw new System.Exception("Group already exists!");
        }

        List<Note> newNotes = [];

        foreach (var k in group.Keys)
        {
            var isBlack = IsBlack(k);

            Vector2 WhiteNoteSize = new(piano.WhiteNoteSize.X, noteHeight);
            Vector2 BlackNoteSize = new(piano.BlackNoteSize.X, noteHeight);

            var rect = new Sprite2D()
            {
                Texture = isBlack ? blackNoteTexture : whiteNoteTexture,
                Position = new Vector2(0, 0),
                Scale = (isBlack ? BlackNoteSize : WhiteNoteSize) / noteTextureScale,
                ZIndex = -1
            };
            AddChild(rect);

            newNotes.Add(new(k, rect));
        }

        var time = group.Time;

        currentNotes[groupIndex] = new(time, newNotes);
    }

    public void RemoveNoteGroup(int groupIndex)
    {
        var noteGroup = currentNotes[groupIndex];

        foreach (var note in noteGroup.notes)
        {
            note.rect.QueueFree();
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
        var musicPlayer = gameManager.MusicPlayer;

        if (musicPlayer.PlayingState == MusicPlayer.PlayState.Stopped) return;

        var (allNoteGroups, timeline) = (musicPlayer.EventGroups, musicPlayer);

        var currentGroup = Mathf.Max(timeline.State.CurrentGroup, 0);

        var selectedGroups = allNoteGroups
            .Skip(currentGroup)
            .TakeWhile(g => g.Time < timeline.TimeMilis + timeSpan * SecondToMilis)
            .ToDictionary(el => el.Time, el => el);

        UpdateNotes(selectedGroups);
    }

    private void UpdateNotes(Dictionary<int, SimpleTimedKeyGroup> newNotes)
    {
        var notesToAdd = newNotes.Where(g => !currentNotes.ContainsKey(g.Key));
        foreach (var g in notesToAdd) AddNoteGroup(g.Key, g.Value);

        var notesToRemove = currentNotes.Where(ng => !newNotes.ContainsKey(ng.Key));
        foreach (var g in notesToRemove) RemoveNoteGroup(g.Key);
    }

    private void UpdateNotePositions()
    {
        var timeline = gameManager.MusicPlayer;

        foreach (var (_, noteGroup) in currentNotes)
        {
            var verticalPos = (noteGroup.Time - timeline.TimeMilis) * MilisToSecond / timeSpan * Size.Y;
            foreach (var note in noteGroup.notes)
            {
                var keyPos = MIDIIndexToKey(note.Key);
                var whiteIndex = GetWhiteIndex(keyPos);

                var (_, noteOffset) = GetNoteOffset(whiteIndex);

                var totalOffset = IsBlack(note.Key)
                    ? (noteOffset * piano.GridSize.X + piano.GridSize.X + piano.BlackNoteSize.X / 2)
                    : (piano.NoteGap / 2 + piano.GridSize.X / 2);

                note.rect.Position = new Vector2(whiteIndex * (Size.X / Whites) + totalOffset, Size.Y - verticalPos - noteHeight / 2);
            }
        }
    }
}
