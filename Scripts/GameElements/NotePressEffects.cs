
using Godot;
using System.Collections.Generic;

namespace PianoTrainer.Scripts.GameElements;

// Adds a sparkling effect to the piano keys
public partial class PianoEffects : PianoLayout
{
    protected GpuParticles2D[] effects = [];

    [Export] private PackedScene effect;

    public override void _Ready()
    {
        base._Ready();

        List<GpuParticles2D> effectsList = [];

        for (byte key = 0; key < KeyboardRange; key++)
        {
            var frame = NoteFrames[key];

            GpuParticles2D particles = (GpuParticles2D)effect.Instantiate();
            particles.Emitting = false;

            frame.AddChild(particles);

            effectsList.Add(particles);
        }

        effects = [.. effectsList];
    }

    protected override void Resize()
    {
        base.Resize();

        for (int i = 0; i < NoteFrames.Count; i++)
        {
            var effect = effects[i];
            var frame = NoteFrames[i];

            effect.Position = frame.Size.X / 2 * Vector2.Right;
        }
    }
}
