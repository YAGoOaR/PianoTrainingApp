using Godot;

public partial class PlayScene : Node2D
{
    [Export]
	private GameSettings gameSettings;

    [Export]
	public MIDIManager MIDIManager { get; private set; }

    public void OnSettingsLoaded()
	{
		MIDIManager.PlayMIDI(gameSettings.Settings.MusicPath);
    }
}
