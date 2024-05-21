using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using PianoTrainer.Scripts.PianoInteraction;

namespace PianoTrainer.Scripts.GameElements;

// Singleton class that handles music flow
public class MusicPlayer
{
    public struct MusicPlayerState()
    {
        public HashSet<byte> DesiredKeys { get; set; } = [];
        public int NextMessageGroup { get; set; } = 0;
        public int CurrentGroup { get; set; } = -1;
        public int MessageDelta { get; set; } = 0;
        public int TotalMessagesTime { get; set; } = 0;
        public int startTime = 0;
    }

    public List<SimpleTimedKeyGroup> Notes { get; set; } = [];

    public float TotalSeconds { get => totalTimeMilis * Utils.MsToSeconds; }
    public int TimeMilis { get => State.TotalMessagesTime + (int)TimeSinceLastKey; }

    public int TimeToNextKey { get => State.MessageDelta - (int)TimeSinceLastKey; }
    public float TimeSinceLastKey { get; private set; } = 0;

    public MusicPlayerState State { get; private set; } = new();

    public event Action<MusicPlayerState> OnTargetChanged;
    public event Action OnStopped;

    private int totalTimeMilis = 0;
    private HashSet<byte> nonreadyKeys = [];
    private bool complete = false;

    public enum PlayState
    {
        Playing,
        Stopped,
    }
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

        if (State.NextMessageGroup > Notes.Count - 1)
        {
            Stop();
            OnTargetChanged?.Invoke(State);
            return;
        }

        var prevGroup = State.CurrentGroup == -1
            ? new(State.startTime, [])
            : Notes[State.CurrentGroup];

        var group = Notes[State.NextMessageGroup];

        State = new()
        {
            TotalMessagesTime = prevGroup.Time,
            DesiredKeys = group.Keys,
            MessageDelta = group.Time - prevGroup.Time,

            CurrentGroup = State.NextMessageGroup,
            NextMessageGroup = State.NextMessageGroup + 1
        };

        TimeSinceLastKey = 0;
        OnTargetChanged?.Invoke(State);

        complete = false;
    }

    public void OnKeyChange(HashSet<byte> pressedKeys)
    {
        if (PlayingState == PlayState.Stopped) return;

        nonreadyKeys = nonreadyKeys.Intersect(pressedKeys).ToHashSet();

        if (complete || State.DesiredKeys.Except(pressedKeys.Except(nonreadyKeys)).Any()) return;

        complete = true;
        nonreadyKeys = new(pressedKeys);

        Task.Run(async () =>
        {
            await Task.Delay(Math.Max(0, TimeToNextKey));
            NextTarget();
        });
    }

    public void Update(float dT)
    {
        TimeSinceLastKey = Math.Min(TimeSinceLastKey + dT * Utils.SecondsToMs, State.MessageDelta);
    }
}
