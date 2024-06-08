
using Godot;
using PianoTrainer.Scripts.Devices;
using PianoTrainer.Scripts.MusicNotes;
using System.Collections.Generic;
using static PianoTrainer.Scripts.MusicNotes.PianoKeys;

namespace PianoTrainer.Scripts.GameElements;

// Piano key setup and visualizing key state changes
public partial class PianoKeyboard : PianoEffects
{
    private static readonly MusicPlayer musicPlayer = MusicPlayer.Instance;

    public float NoteGap { get; private set; } = 0;

    [Export] private Theme[] themes;

    private readonly List<Panel> noteRects = [];
    private readonly Queue<NoteMessage> changes = [];

    public override void _Ready()
    {
        base._Ready();
        DeviceManager.Instance.DefaultPiano.Keys.KeyChange += SetKey;

        SetupKeys();
    }

    private void SetupKeys()
    {
        for (byte key = 0; key < KeyboardRange; key++)
        {
            var frame = NoteFrames[key];
            bool black = IsBlack(key);

            Panel noteRect = new()
            {
                Theme = GetNoteTheme(black, false),
            };
            frame.AddChild(noteRect);
            frame.MoveChild(noteRect, 0);

            noteRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

            noteRects.Add(noteRect);
        }
    }

    public void SetKey(NoteMessage msg) => changes.Enqueue(msg);

    public override void _Process(double delta)
    {
        while (changes.Count > 0)
        {
            var (midiIndex, activated) = changes.Dequeue();
            byte key = MIDIIndexToPianoKey(midiIndex);
            bool isBlack = IsBlack(key);

            bool keyHasEffect = (
                musicPlayer.State.Target.Contains(midiIndex) ||
                musicPlayer.NonreadyKeys.Contains(midiIndex)
            );

            effects[key].Emitting = activated && keyHasEffect;
            noteRects[key].Theme = GetNoteTheme(isBlack, activated);
        }
    }

    private Theme GetNoteTheme(bool isBlack, bool isActive)
    {
        var themeCode = (isBlack ? 0 : 1) + (isActive ? 1 : 0) * 2;
        return themes[themeCode];
    }
}
