using Godot;
using PianoTrainer.Scripts.MIDI;

public partial class MusicSheet : Node2D
{
	private MIDIManager midiManager;
	private MIDIPlayer midiPlayer;

	public override void _Ready()
	{
		midiManager = MIDIManager.Instance;
    }

	public override void _Process(double delta)
	{
        if (midiPlayer != null && midiPlayer.TotalTimeMilis != 0)
		{

		}
	}
}
