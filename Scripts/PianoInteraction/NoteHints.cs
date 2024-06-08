
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using PianoTrainer.Scripts.GameElements;
using PianoTrainer.Scripts.Devices;

namespace PianoTrainer.Scripts.PianoInteraction;

// Light hints that help to guide the user
public class NoteHints
{
    private static readonly MusicPlayer musicPlayer = MusicPlayer.Instance;

    private static PlayerSettings PlayerSettings { get => GameSettings.Instance.PlayerSettings; }
    private readonly PianoKeyLighting lights = new();

    private static NoteHints instance;
    public static NoteHints Instance
    {
        get
        {
            instance ??= new();
            return instance;
        }
    }

    private NoteHints()
    {
        musicPlayer.OnTargetChanged += OnTargetCompleted;
        musicPlayer.OnStopped += lights.Reset;
    }

    public static void Init()
    {
        instance ??= new();
    }

    // When user pressed the correct keys
    public void OnTargetCompleted(MusicPlayerState state)
    {
        lights.Reset();

        List<byte> keys = new(state.Target);

        Task lightupNotes = LateNotePressHint(keys, musicPlayer.TimeToNextKey);

        bool isLate() => lightupNotes.IsCompleted || musicPlayer.TimeToNextKey < PlayerSettings.LateHintOutdateTime;
        bool isStateChanged() => musicPlayer.State.Group != state.Group;

        Task.Run(() => EarlyNotePressHint(isLate, isStateChanged, keys));
    }

    // Permanent light hint
    private async Task LateNotePressHint(List<byte> keys, int timeToPress)
    {
        await Task.Delay(Math.Max(0, timeToPress - PlayerSettings.KeyTimeOffset));
        lights.SetKeys(keys);
    }

    // Blinking light hint
    private async Task EarlyNotePressHint(Func<bool> isLate, Func<bool> notesOutdated, List<byte> keys)
    {
        await Task.Delay(Math.Max(0, musicPlayer.TimeToNextKey - PlayerSettings.BlinkStartOffset));

        while (!isLate() && !notesOutdated())
        {
            var interval = musicPlayer.TimeToNextKey > PlayerSettings.BlinkFastStartOffset
                ? PlayerSettings.BlinkSlowInterval
                : PlayerSettings.BlinkInterval;

            foreach (var k in keys)
            {
                lights.AddBlink(k, interval);
            }
            await Task.Delay(PlayerSettings.BlinkInterval + interval + lights.TickTime);
        }
    }
}