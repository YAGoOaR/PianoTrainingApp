
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PianoTrainer.Scripts.PianoInteraction;

namespace PianoTrainer.Scripts.GameElements;
using static TimeUtils;

public enum PlayState
{
    Playing,
    Stopped,
}

public struct MusicPlayerState()
{
    public HashSet<byte> DesiredKeys { get; set; } = [];
    public int NextGroup { get; set; } = 1;
    public int Group { get; set; } = 0;
    public int GroupDeltatime { get; set; } = 0;
    public int AccumulatedGroupTime { get; set; } = 0;
    public int startTime = 0;
}

// Handles the music flow and user guidance
public class MusicPlayer
{
    public List<TimedNoteGroup> Notes { get; set; } = [];

    public float TotalSeconds { get => totalTimeMilis * MsToSeconds; }
    public int TimeMilis { get => State.AccumulatedGroupTime + (int)TimeSinceLastKey; }

    public int TimeToNextKey { get => State.GroupDeltatime - (int)TimeSinceLastKey; }
    public float TimeSinceLastKey { get; private set; } = 0;

    public double Bpm { get; private set; } = 0;

    public MusicPlayerState State { get; private set; } = new();

    public event Action<MusicPlayerState> OnTargetChanged;
    public event Action OnStopped;

    private int totalTimeMilis = 0;
    public HashSet<byte> NonreadyKeys { get; private set; } = [];
    private bool complete = false;

    public PlayState PlayingState { get; private set; } = PlayState.Stopped;

    private static MusicPlayer instance;
    public static MusicPlayer Instance
    {
        get
        {
            instance ??= new();
            return instance;
        }
    }

    private MusicPlayer() { }

    public void Setup(ParsedMusic music)
    {
        Notes = music.Notes;
        State = new();
        totalTimeMilis = music.TotalTime;

        complete = false;
        NonreadyKeys = [];
        PlayingState = PlayState.Stopped;
        TimeSinceLastKey = 0;

        Bpm = music.Bpm;
    }

    public void Play()
    {
        if (PlayingState != PlayState.Stopped) return;
        NextTarget();
        PlayingState = PlayState.Playing;
    }

    public void Stop()
    {
        PlayingState = PlayState.Stopped;
        State = new();
        OnStopped?.Invoke();
    }

    public void NextTarget()
    {
        if (Notes.Count == 0) throw new Exception("PlayManager is not initialized");

        if (State.NextGroup > Notes.Count - 1)
        {
            Stop();
            OnTargetChanged?.Invoke(State);
            return;
        }

        var prevGroup = Notes[State.Group];
        var group = Notes[State.NextGroup];

        State = new()
        {
            AccumulatedGroupTime = prevGroup.Time,
            DesiredKeys = group.Notes.Select(x => x.Key).ToHashSet(),
            GroupDeltatime = group.Time - prevGroup.Time,

            Group = State.NextGroup,
            NextGroup = State.NextGroup + 1
        };

        TimeSinceLastKey = 0;
        OnTargetChanged?.Invoke(State);

        complete = false;
    }

    public async void OnKeyChange(HashSet<byte> pressedKeys)
    {
        if (PlayingState == PlayState.Stopped) return;

        NonreadyKeys = NonreadyKeys.Intersect(pressedKeys).ToHashSet();

        if (complete || State.DesiredKeys.Except(pressedKeys.Except(NonreadyKeys)).Any()) return;

        complete = true;
        NonreadyKeys = new(pressedKeys);

        await Task.Delay(Math.Max(0, TimeToNextKey));
        NextTarget();
    }

    public void Update(float dT)
    {
        TimeSinceLastKey = Math.Min(TimeSinceLastKey + dT * SecondsToMs, State.GroupDeltatime);
    }
}
