using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PianoTrainer.Scripts.MIDI;

namespace PianoTrainer.Scripts
{
    public struct PlayManagerState()
    {
        public HashSet<byte> DesiredKeys { get; set; } = [];
        public int NextMessageGroup { get; set; } = 0;
        public int CurrentGroup { get; set; } = -1;
        public int MessageDelta { get; set; } = 0;
        public int TotalMessagesTime { get; set; } = 0;
        public int startTime = 0;
    }

    public class MusicPlayer()
    {
        public List<SimpleTimedKeyGroup> EventGroups { get; private set; } = [];

        private HashSet<byte> nonreadyKeys = [];

        public PlayManagerState State { get; private set; } = new();

        public event Action<PlayManagerState> OnTargetChanged;
        public event Action OnComplete;
        public event Action OnStopped;

        private bool complete = false;

        public int TotalTimeMilis { get; private set; } = 0;

        public float TotalTimeSeconds { get => TotalTimeMilis * Utils.MilisToSecond; }

        public int TimeMilis { get => State.TotalMessagesTime + (int)TimeSinceLastKey; }
        public int TimeToNextKey { get => State.MessageDelta - (int)TimeSinceLastKey; }

        public float TimeSinceLastKey { get; private set; } = 0;

        public enum PlayState
        {
            Ready,
            Stopped,
        }
        public PlayState PlayingState { get; private set; } = PlayState.Stopped;

        public void Setup(List<SimpleTimedKeyGroup> keyMessages, int totalTime)
        {
            EventGroups = keyMessages;

            State = new();
            PlayingState = PlayState.Ready;

            TotalTimeMilis = totalTime;

            NextTarget();
        }

        public void Stop()
        {
            PlayingState = PlayState.Stopped;
            State = new();
            OnStopped?.Invoke();
        }

        public void NextTarget()
        {
            if (EventGroups.Count == 0) throw new Exception("PlayManager is not initialized");

            if (State.NextMessageGroup > EventGroups.Count - 1)
            {
                Stop();
                OnTargetChanged?.Invoke(State);
                return;
            }

            var prevGroup = State.CurrentGroup == -1
                ? new(State.startTime, [])
                : EventGroups[State.CurrentGroup];

            var group = EventGroups[State.NextMessageGroup];

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

            OnComplete?.Invoke();
        }

        public void Update(float dT)
        {
            TimeSinceLastKey = Math.Min(TimeSinceLastKey + dT * Utils.SecondToMilis, State.MessageDelta);
        }
    }
}
