
using Godot;
using PianoTrainer.Scripts.Devices;
using PianoTrainer.Scripts.PianoInteraction;
using System.Collections.Generic;

using static PianoTrainer.Scripts.PianoInteraction.PianoKeys;
using static System.Net.Mime.MediaTypeNames;

namespace PianoTrainer.Scripts.GameElements;

// Defines Piano key setup and layout
public partial class PianoKeyboard : PianoLayout
{
    [Export] Theme whiteTheme;
    [Export] Theme blackTheme;
    [Export] Theme whiteActiveTheme;
    [Export] Theme blackActiveTheme;

    public float NoteGap { get; private set; } = 0;

    readonly List<Panel> noteRects = [];

    readonly Queue<MoteMessage> changes = [];

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
                Theme = black ? blackTheme : whiteTheme,
            };
            holder.AddChild(noteRect);

            noteRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

            noteRects.Add(noteRect);
        }
    }

    public void SetKey(MoteMessage msg) => changes.Enqueue(msg);

    public override void _Process(double delta)
    {
        while (changes.Count > 0)
        {
            var (midiIndex, isActive) = changes.Dequeue();
            byte key = MIDIIndexToKey(midiIndex);
            bool isBlack = IsBlack(key);

            noteRects[key].Theme =
            (
                isActive
                    ? isBlack
                        ? blackActiveTheme
                        : whiteActiveTheme
                    : isBlack
                        ? blackTheme
                        : whiteTheme
            );
        }
    }
}
