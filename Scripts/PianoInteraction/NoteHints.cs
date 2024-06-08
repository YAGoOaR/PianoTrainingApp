
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using PianoTrainer.Scripts.GameElements;

namespace PianoTrainer.Scripts.PianoInteraction;

// Light hints that help to guide the user
public class NoteHints
{
    private static readonly MusicPlayer musicPlayer = MusicPlayer.Instance;
    private static readonly PlayerSettings settings = GameSettings.Instance.PlayerSettings;

    private static NoteHints instance;
    public static NoteHints Instance
    {
        get
        {
            instance ??= new();
            return instance;
        }
    }

    private readonly PianoKeyLighting lights = new();

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

        bool isLate() => lightupNotes.IsCompleted || musicPlayer.TimeToNextKey < settings.LateHintOutdateTime;
        bool isStateChanged() => musicPlayer.State.Group != state.Group;

        Task.Run(() => EarlyNotePressHint(isLate, isStateChanged, keys));
    }

    // Permanent light hint
    private async Task LateNotePressHint(List<byte> keys, int timeToPress)
    {
        await Task.Delay(Math.Max(0, timeToPress - settings.KeyTimeOffset));
        lights.SetKeys(keys);
    }

    // Blinking light hint
    private async Task EarlyNotePressHint(Func<bool> isLate, Func<bool> notesOutdated, List<byte> keys)
    {
        await Task.Delay(Math.Max(0, musicPlayer.TimeToNextKey - settings.BlinkStartOffset));

        while (!isLate() && !notesOutdated())
        {
            var interval = musicPlayer.TimeToNextKey > settings.BlinkFastStartOffset
                ? settings.BlinkSlowInterval
                : settings.BlinkInterval;

            foreach (var k in keys)
            {
                lights.AddBlink(k, interval);
            }
            await Task.Delay(settings.BlinkInterval + interval + lights.TickTime);
        }
    }
}