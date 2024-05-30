
using Godot;
using System.Collections.Generic;

namespace PianoTrainer.Scripts.GameElements;

public partial class PianoEffects : PianoLayout
{
    [Export] private PackedScene effect;

    protected readonly List<GpuParticles2D> effects = [];

    public override void _Ready()
    {
        base._Ready();

        for (byte key = 0; key < KeyboardRange; key++)
        {
            var holder = NoteFrames[key];

            GpuParticles2D particles = (GpuParticles2D)effect.Instantiate();

            particles.Position = holder.Size.X / 2 * Vector2.Right;
            particles.Emitting = false;

            holder.AddChild(particles);

            effects.Add(particles);
        }
    }
}
