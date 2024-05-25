
using Godot;
using PianoTrainer.Scripts.Devices;
using PianoTrainer.Scripts.PianoInteraction;
using System.Collections.Generic;

using static PianoTrainer.Scripts.PianoInteraction.PianoKeys;

namespace PianoTrainer.Scripts.GameElements;

// Defines Piano key setup and layout
public partial class PianoKeyboard : Control
{
    [Export] Theme whiteTheme;
    [Export] Theme blackTheme;
    [Export] Theme activeTheme;
    [Export] Theme blackActiveTheme;

    public Vector2 GridSize { get; private set; }
    public Vector2 WhiteNoteSize { get; private set; }
    public Vector2 BlackNoteSize { get; private set; }

    public float NoteGap { get; private set; } = 0;

    readonly List<Panel> noteRects = [];

    readonly Queue<NoteMsg> changes = [];

    public override void _Ready()
    {
        DeviceManager.Instance.DefaultPiano.Piano.KeyChange += SetKey;

        GridSize = new(Size.X / WhiteKeyCount, Size.Y);
        WhiteNoteSize = GridSize - Vector2.Right * NoteGap;
        BlackNoteSize = WhiteNoteSize * BlackNoteSizeRatio;

        Position = new(0, GetViewportRect().Size.Y);

        SetupKeys();
    }

    private void SetupKeys()
    {
        for (byte i = 0; i < WhiteKeyCount; i++)
        {
            var whiteRect = new Panel
            {
                Position = new Vector2(GridSize.X * i + NoteGap / 2, -WhiteNoteSize.Y),
                Size = new Vector2(WhiteNoteSize.X, WhiteNoteSize.Y),
                ZIndex = -2,
                Theme = whiteTheme,
            };
            AddChild(whiteRect);
            noteRects.Add(whiteRect);

            var (blackExists, noteOffset) = GetNoteOffset(i);

            if (blackExists && i != WhiteKeyCount - 1)
            {
                var rect = new Panel()
                {
                    Position = new Vector2(GridSize.X * (i + 1 + noteOffset), -WhiteNoteSize.Y),
                    Size = new Vector2(BlackNoteSize.X, BlackNoteSize.Y),
                    ZIndex = -1,
                    Theme = blackTheme,
                };

                AddChild(rect);
                noteRects.Add(rect);
            }
        }
    }

    public void SetKey(NoteMsg msg) => changes.Enqueue(msg);

    public override void _Process(double delta)
    {
        while (changes.Count > 0)
        {
            var (key, state) = changes.Dequeue();

            byte k = MIDIIndexToKey(key);

            noteRects[k].Theme = IsBlack(k) 
                ? state 
                    ? blackActiveTheme 
                    : blackTheme 
                : state 
                    ? activeTheme 
                    : whiteTheme;
        }
    }
}
