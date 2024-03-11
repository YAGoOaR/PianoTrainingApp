using Godot;
using PianoTrainer.Scripts.MIDI;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public partial class MusicSheet : Node2D
{
	private MIDIManager midiManager;

    private Vector2 MusicSheetOffset = new(0, 250);
    private Vector2 MusicSheetSize;

    [Export]
    private CompressedTexture2D noteWhiteTexture;

    [Export]
    private CompressedTexture2D noteBlackTexture;

    private float timeSpan = 3;
	private int currentGroup = 0;

    private float noteGap = 8;

    [Export]
    private Piano piano;
    private Vector2 NoteSize;
    private Vector2 BlackNoteSize;


    private record Note(byte Key, Sprite2D rect);
    private record NoteGroup(int Time, List<Note> notes);

	private readonly Dictionary<int, NoteGroup> notes = [];

	public override void _Ready()
	{
        NoteSize = new(piano.NoteSize.X, 50);
        BlackNoteSize = new(piano.BlackNoteSize.X, 50);

        MusicSheetSize = GetViewportRect().Size - MusicSheetOffset - Vector2.Up * - 80;

        midiManager = MIDIManager.Instance;
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
            var isBlack = Piano.IsBlack(k);

            var rect = new Sprite2D()
            {
                Texture = isBlack ? noteBlackTexture : noteWhiteTexture,
                Position = new Vector2(0, 0),
                Scale = (isBlack ? BlackNoteSize: NoteSize) / 200,
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

			for (int i = currentGroup; i < selectedGroups.Count && selectedGroups[i].Time < pm.CurrentTimeMilis + timeSpan*1000; i++)
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
            var verticalPos = (v.Time - pm.CurrentTimeMilis) / 1000f / timeSpan * MusicSheetSize.Y;
            foreach (var n in v.notes)
            {
                var keyPos = (byte)(n.Key - 36);
                var whiteIndex = Piano.GetWhiteIndex(keyPos);

                var (_, noteOffset) = Piano.GetNoteOffset(whiteIndex);

                var totalOffset = Piano.IsBlack(n.Key) ? (noteOffset * piano.NoteGridSize.X + piano.NoteGridSize.X + BlackNoteSize.X/2) : (piano.NoteGap / 2 + piano.NoteGridSize.X / 2);

                n.rect.Position = new Vector2(whiteIndex / 36f * MusicSheetSize.X + totalOffset, 80 + MusicSheetSize.Y - verticalPos - NoteSize.Y / 2);
            }  
        }

	}
}
