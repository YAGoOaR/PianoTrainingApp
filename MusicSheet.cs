using Godot;
using PianoTrainer.Scripts.MIDI;
using System;

public partial class MusicSheet : Node2D
{
	[Export]
	MIDIManager MIDIManager { get; set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var p = MIDIManager.Player;

        if (p != null && p.TotalTimeMilis != 0)
		{

		}
	}
}
