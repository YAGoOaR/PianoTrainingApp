
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using PianoTrainer.Scripts.PianoInteraction;

namespace PianoTrainer.Scripts.GameElements;
using static TimeUtils;

public enum PlayState
{
    Playing,
    Paused,
    Stopped,
}

public struct MusicPlayerState()
{
    public HashSet<byte> Target { get; set; } = [];
    public int NextGroup { get; set; } = 1;
    public int Group { get; set; } = 0;
    public int GroupDeltatime { get; set; } = 0;
    public int AccumulatedTime { get; set; } = 0;
}

// Handles the music flow and user guidance
public class MusicPlayer
{
    public static MusicPlayer Instance
    {
        get
        {
            instance ??= new();
            return instance;
        }
    }

    private static MusicPlayer instance;

    public event Action<MusicPlayerState> OnTargetChanged;
    public event Action OnStopped;
    public List<TimedNoteGroup> Notes { get; set; } = [];

    public float TotalSeconds { get => totalTimeMilis * MS_TO_SEC; }
    public int TimeMilis { get => State.AccumulatedTime + (int)TimeSinceLastKey; }

    public int TimeToNextKey { get => State.GroupDeltatime - (int)TimeSinceLastKey; }
    public float TimeSinceLastKey { get; private set; } = 0;

    public double Bpm { get; private set; } = 0;
    public double BeatTime { get; private set; } = 0;

    public MusicPlayerState State { get; private set; } = new();

    private int totalTimeMilis = 0;
    public HashSet<byte> NonreadyKeys { get; private set; } = [];
    private bool complete = false;

    public PlayState PlayingState { get; private set; } = PlayState.Stopped;

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
        BeatTime = music.BeatTime;
    }

    public void Play()
    {
        if (PlayingState == PlayState.Playing) return;
        NextTarget();
        PlayingState = PlayState.Playing;
    }

    public void Pause()
    {
        PlayingState = PlayState.Paused;
    }

    public void Stop()
    {
        PlayingState = PlayState.Stopped;
        State = new();
        OnStopped.Invoke();
    }

    public void SetCursor(int groupIndex)
    {
        var prevGroup = Notes[Mathf.Max(groupIndex - 1, 0)];
        var group = Notes[Mathf.Max(groupIndex, 0)];

        State = new()
        {
            AccumulatedTime = prevGroup.Time,
            Target = group.Notes.Select(x => x.Key).ToHashSet(),
            GroupDeltatime = group.Time - prevGroup.Time,

            Group = groupIndex,
            NextGroup = groupIndex + 1
        };

        TimeSinceLastKey = 0;
        OnTargetChanged?.Invoke(State);

        complete = false;
    }

    public void NextTarget()
    {
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
            AccumulatedTime = prevGroup.Time,
            Target = group.Notes.Select(x => x.Key).ToHashSet(),
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
        if (PlayingState != PlayState.Playing) return;
        NonreadyKeys = NonreadyKeys.Intersect(pressedKeys).ToHashSet();

        if (complete || State.Target.Except(pressedKeys.Except(NonreadyKeys)).Any()) return;

        complete = true;
        NonreadyKeys = new(pressedKeys);

        await Task.Delay(Math.Max(0, TimeToNextKey));
        NextTarget();
    }

    public void Update(float dT)
    {
        if (PlayingState != PlayState.Playing) return;
        TimeSinceLastKey = Math.Min(TimeSinceLastKey + dT * SEC_TO_MS, State.GroupDeltatime);
    }
}
