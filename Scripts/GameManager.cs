using Commons.Music.Midi;
using Godot;
using PianoTrainer.Scripts;
using PianoTrainer.Scripts.MIDI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using static GameSettings;
using static PianoTrainer.Scripts.MIDI.MidiUtils;

public partial class GameManager : Node2D
{
    public static GameManager Instance { get; private set; }

    public KeyState Piano { get; private set; }

    [Export]
    public ProgressBar PBar { get; private set; }

    [Export]
    public FallingNotes FallingNotes { get; private set; }

    public PianoKeyLighting LightsManager { get; private set; }

    public GameSettings Settings { get; private set; }

    public MusicPlayer MusicPlayer { get; private set; } = new();

    private IMidiOutput output;
    private IMidiInput input;

    private PlayerSettings PlayerSettings { get => Settings.Settings.playerSettings; }

    public enum GameState
    {
        Preparing,
        Ready,
        Playing,
        Stopped,
        Exited,
    }

    public GameState State { get; private set; } = GameState.Preparing;

    private MidiMusic music;

    public void SetState(GameState state)
    {
        State = state;
    }

    public override void _Ready()
    {
        ListDevices();
        Instance = this;
        Piano = new KeyState();
        Settings = GameSettings.Instance;

        music = LoadMIDI(Settings.Settings.MusicPath);

        SetupDevice();
    }

    public Task SetupDevice() => Task.Run(async () =>
    {
        var device = GameSettings.Instance.Settings.PianoDeviceName;

        output = await new OutputPortManager(device).OpenPort();
        input = await new InputPortManager(device).OpenPort();

        var lights = new KeyboardInterface(output);
        LightsManager = new PianoKeyLighting(lights);

        input.MessageReceived += OnMessage;

        Piano.KeyChange += (_, _) => MusicPlayer.OnKeyChange(Piano.State);

        MusicPlayer.OnTargetChanged += OnTargetChange;

        MusicPlayer.OnStopped += () => LightsManager.Reset();

        SetState(GameState.Ready);
    });

    public void Play()
    {
        FallingNotes.Clear();

        var (notelist, totalTime) = ParseMusic(music, LightsManager.LightsState.IsAcceptable);
        MusicPlayer.Setup(notelist, totalTime);
    }

    public void OnMessage(object input, MidiReceivedEventArgs message)
    {
        var msgType = message.Data[0];

        bool isKeyData = msgType == MidiEvent.NoteOn || msgType == MidiEvent.NoteOff;

        if (isKeyData)
        {
            byte note = message.Data[1];
            Piano.SetKey(new(note, msgType == MidiEvent.NoteOn));
        }
    }

    public override void _Process(double delta)
    {
        if (State == GameState.Ready)
        {
            Play();
            SetState(GameState.Playing);
        }
        else if (State == GameState.Playing)
        {
            MusicPlayer.Update((float)delta);
        }

        if (State != GameState.Stopped) return;

        Debug.WriteLine("Returned to menu.");

        GetTree().ChangeSceneToFile(SceneManager.MenuScene);
        SetState(GameState.Exited);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            SetState(GameState.Stopped);
        }
    }

    public void OnTargetChange(PlayManagerState state)
    {
        LightsManager.Reset();

        List<byte> keys = new(state.DesiredKeys);

        Task lightupNotes = LightupNotes(keys);

        bool tooLateToHint = MusicPlayer.TimeToNextKey < PlayerSettings.BlinkOutdateTime;
        if (tooLateToHint) return;

        bool isStateOutdated() => MusicPlayer.State.CurrentGroup != state.CurrentGroup;

        NoteHints(() => lightupNotes.IsCompleted, isStateOutdated, keys);
    }

    private Task LightupNotes(List<byte> keys)
    {
        return Task.Run(async () =>
        {
            await Task.Delay(Math.Max(0, MusicPlayer.TimeToNextKey - PlayerSettings.KeyTimeOffset));
            LightsManager.SetKeys(keys);
        });
    }

    private Task NoteHints(Func<bool> hintOutdated, Func<bool> stateOutdated, List<byte> keys)
    {
        return Task.Run(async () =>
        {
            await Task.Delay(Math.Max(0, MusicPlayer.TimeToNextKey - PlayerSettings.BlinkStartOffset));

            while (!hintOutdated() && !stateOutdated())
            {
                var interval = MusicPlayer.TimeToNextKey > PlayerSettings.BlinkFastStartOffset
                    ? PlayerSettings.BlinkSlowInterval
                    : PlayerSettings.BlinkInterval;

                foreach (var k in keys)
                {
                    LightsManager.AddBlink(k, interval);
                }
                await Task.Delay(PlayerSettings.BlinkInterval + interval + LightsManager.TickTime);
            }
        });
    }

    public void Update(double delta) => MusicPlayer.Update((float)delta);

    public override void _ExitTree()
    {
        LightsManager.Dispose();
        output.Dispose();
        input.Dispose();
    }
}
