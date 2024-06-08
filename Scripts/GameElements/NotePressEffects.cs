
using Godot;
using System.Collections.Generic;

namespace PianoTrainer.Scripts.GameElements;

// Adds a sparkling effect to the piano keys
public partial class PianoEffects : PianoLayout
{
    protected readonly List<GpuParticles2D> effects = [];

    [Export] private PackedScene effect;

    public override void _Ready()
    {
        base._Ready();

        for (byte key = 0; key < KeyboardRange; key++)
        {
            var frame = NoteFrames[key];

            GpuParticles2D particles = (GpuParticles2D)effect.Instantiate();

            particles.Position = frame.Size.X / 2 * Vector2.Right;
            particles.Emitting = false;

            frame.AddChild(particles);

            effects.Add(particles);
        }
    }
}
