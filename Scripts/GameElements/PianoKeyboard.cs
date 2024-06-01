
using Godot;
using PianoTrainer.Scripts.Devices;
using PianoTrainer.Scripts.PianoInteraction;
using System.Collections.Generic;
using static PianoTrainer.Scripts.PianoInteraction.PianoKeys;

namespace PianoTrainer.Scripts.GameElements;

// Piano key setup and visualizing key state changes
public partial class PianoKeyboard : PianoEffects
{
    [Export] private Theme[] themes;

    private readonly MusicPlayer musicPlayer = MusicPlayer.Instance;

    public float NoteGap { get; private set; } = 0;

    readonly List<Panel> noteRects = [];

    readonly Queue<NoteMessage> changes = [];

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
            var holder = NoteFrames[key];
            bool black = IsBlack(key);

            Panel noteRect = new()
            {
                Theme = GetNoteTheme(black, false),
            };
            holder.AddChild(noteRect);
            holder.MoveChild(noteRect, 0);

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
            byte key = MIDIIndexToKey(midiIndex);
            bool isBlack = IsBlack(key);

            bool keyHasEffect = (
                musicPlayer.State.DesiredKeys.Contains(midiIndex) ||
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
