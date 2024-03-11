using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts.MIDI
{
    public struct PlayManagerState()
    {
        public HashSet<byte> DesiredKeys { get; set; } = [];
        public int NextMessageGroup { get; set; } = 0;
        public int CurrentMessageGroup { get; set; } = -1;
        public int MessageDelta { get; set; } = 0;
        public int TotalMessagesTime { get; set; } = 0;
    }

    public class PlayManager()
    {
        public List<SimpleTimedKeyGroup> EventGroups { get; private set; } = [];

        private HashSet<byte> nonreadyKeys = [];

        public PlayManagerState State { get; private set; } = new();

        public event Action<PlayManagerState> OnTargetChanged;
        public event Action OnComplete;
        public event Action OnStopped;

        private bool complete = false;

        public enum PlayState
        {
            Ready,
            Stopped,
        }
        PlayState playState = PlayState.Stopped;

        public void Setup(List<SimpleTimedKeyGroup> keyMessages)
        {
            EventGroups = keyMessages;
            playState = PlayState.Ready;

            NextTarget();
        }

        public void Stop()
        {
            playState = PlayState.Stopped;
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

            var pGroup = State.CurrentMessageGroup == -1 ? new(0, []) : EventGroups[State.CurrentMessageGroup];
            var group = EventGroups[State.NextMessageGroup];

            State = new()
            {
                TotalMessagesTime = pGroup.Time,
                DesiredKeys = group.Keys,
                MessageDelta = group.Time - pGroup.Time, //TODO: REFACTOR

                CurrentMessageGroup = State.NextMessageGroup,
                NextMessageGroup = State.NextMessageGroup + 1
            };

            OnTargetChanged?.Invoke(State);

            complete = false;
        }

        public void OnKeyChange(HashSet<byte> pressedKeys)
        {
            if (playState == PlayState.Stopped) return;

            nonreadyKeys = nonreadyKeys.Intersect(pressedKeys).ToHashSet();

            if (complete || State.DesiredKeys.Except(pressedKeys.Except(nonreadyKeys)).Any()) return;

            complete = true;
            nonreadyKeys = new(pressedKeys);

            OnComplete?.Invoke();
        }
    }
}
