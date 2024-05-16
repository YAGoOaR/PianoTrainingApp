using Godot;
using PianoTrainer.Scripts.MIDI;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public partial class FallingNotes : Control
{
    [Export] private CompressedTexture2D whiteNoteTexture;
    [Export] private CompressedTexture2D blackNoteTexture;
    [Export] private PianoKeyboard piano;
    [Export] private ProgressBar progressBar;
    [Export] private float NoteHeight = 50;
    [Export] private float timeSpan = 3;

    private MIDIManager midiManager;
    
    private int currentGroup = 0;

    private record Note(byte Key, Sprite2D rect);
    private record NoteGroup(int Time, List<Note> notes);

    private readonly Dictionary<int, NoteGroup> notes = [];

    public override void _Ready()
    {
        midiManager = MIDIManager.Instance;
    }

    public void Init()
    {
        foreach (var (k, noteGroup) in notes)
        {
            foreach (var note in noteGroup.notes)
            {
                note.rect.QueueFree();
            }
        }

        notes.Clear();
    }

    public void AddNoteGroup(int groupIndex, SimpleTimedKeyGroup group)
    {
        if (notes.ContainsKey(groupIndex))
        {
            throw new System.Exception("Group already exists!");
        }

        List<Note> newNotes = [];

        foreach (var k in group.Keys)
        {
            var isBlack = PianoKeyboard.IsBlack(k);

            Vector2 WhiteNoteSize = new(piano.WhiteNoteSize.X, NoteHeight);
            Vector2 BlackNoteSize = new(piano.BlackNoteSize.X, NoteHeight);

            var rect = new Sprite2D()
            {
                Texture = isBlack ? blackNoteTexture : whiteNoteTexture,
                Position = new Vector2(0, 0),
                Scale = (isBlack ? BlackNoteSize : WhiteNoteSize) / 200,
                ZIndex = -10
            };
            AddChild(rect);

            newNotes.Add(new(k, rect));
        }

        var time = group.Time;

        notes[groupIndex] = new(time, newNotes);
    }

    public void RemoveNoteGroup(int groupIndex)
    {
        var noteGroup = notes[groupIndex];

        foreach (var note in noteGroup.notes)
        {
            note.rect.QueueFree();
        }

        notes.Remove(groupIndex);
    }

    public override void _Process(double delta)
    {
        var midiPlayer = midiManager.Player;

        var pm = midiPlayer.PlayManager;

        if (midiPlayer != null && midiPlayer.TotalTimeMilis != 0)
        {
            currentGroup = Mathf.Max(0, pm.State.CurrentMessageGroup);

            var selectedGroups = midiPlayer.NoteListAbsTime;

            Dictionary<int, SimpleTimedKeyGroup> groupAcc = [];

            for (int i = currentGroup; i < selectedGroups.Count && selectedGroups[i].Time < pm.CurrentTimeMilis + timeSpan * 1000; i++)
            {
                groupAcc.Add(i, selectedGroups[i]);
            }

            var newGroups = groupAcc.Where(g => !notes.ContainsKey(g.Key));

            foreach (var g in newGroups)
            {
                AddNoteGroup(g.Key, g.Value);
            }

            var extraNotes = notes.Where(ng => !groupAcc.ContainsKey(ng.Key));

            foreach (var g in extraNotes)
            {
                RemoveNoteGroup(g.Key);
            }
        }

        foreach (var (k, v) in notes)
        {
            var verticalPos = (v.Time - pm.CurrentTimeMilis) / 1000f / timeSpan * Size.Y;
            foreach (var n in v.notes)
            {
                var keyPos = (byte)(n.Key - 36);
                var whiteIndex = PianoKeyboard.GetWhiteIndex(keyPos);

                var (_, noteOffset) = PianoKeyboard.GetNoteOffset(whiteIndex);

                var totalOffset = PianoKeyboard.IsBlack(n.Key) ? (noteOffset * piano.NoteGridSize.X + piano.NoteGridSize.X + piano.BlackNoteSize.X / 2) : (piano.NoteGap / 2 + piano.NoteGridSize.X / 2);

                n.rect.Position = new Vector2(whiteIndex / 36f * Size.X + totalOffset, Size.Y - verticalPos - NoteHeight / 2);
            }
        }

    }
}
