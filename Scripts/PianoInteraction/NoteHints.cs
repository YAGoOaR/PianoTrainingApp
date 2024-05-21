using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using PianoTrainer.Scripts.GameElements;

namespace PianoTrainer.Scripts.PianoInteraction;

public class NoteHints(PianoKeyLighting lightController)
{
    private static GameSettings.PlayerSettings PlayerSettings { get => GameSettings.Instance.Settings.PlayerSettings; }
    private readonly MusicPlayer musicPlayer = MusicPlayer.Instance;

    public void OnTargetCompleted(MusicPlayer.MusicPlayerState state)
    {
        lightController.Reset();

        List<byte> keys = new(state.DesiredKeys);

        Task lightupNotes = LateNotePressHint(keys, musicPlayer.TimeToNextKey);

        bool isLate() => lightupNotes.IsCompleted || musicPlayer.TimeToNextKey < PlayerSettings.LateHintOutdateTime;
        bool isStateChanged() => musicPlayer.State.CurrentGroup != state.CurrentGroup;

        EarlyNotePressHint(isLate, isStateChanged, keys);
    }

    // Permanent light hint
    private Task LateNotePressHint(List<byte> keys, int timeToPress) => Task.Run(async () =>
    {
        await Task.Delay(Math.Max(0, timeToPress - PlayerSettings.KeyTimeOffset));
        lightController.SetKeys(keys);
    });

    // Blinking light hint
    private Task EarlyNotePressHint(Func<bool> isLate, Func<bool> notesOutdated, List<byte> keys) => Task.Run(async () =>
    {
        await Task.Delay(Math.Max(0, musicPlayer.TimeToNextKey - PlayerSettings.BlinkStartOffset));

        while (!isLate() && !notesOutdated())
        {
            var interval = musicPlayer.TimeToNextKey > PlayerSettings.BlinkFastStartOffset
                ? PlayerSettings.BlinkSlowInterval
                : PlayerSettings.BlinkInterval;

            foreach (var k in keys)
            {
                lightController.AddBlink(k, interval);
            }
            await Task.Delay(PlayerSettings.BlinkInterval + interval + lightController.TickTime);
        }
    });
}