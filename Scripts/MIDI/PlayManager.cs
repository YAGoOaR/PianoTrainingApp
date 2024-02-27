using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static Godot.WebSocketPeer;

namespace PianoTrainer.Scripts.MIDI
{
    // TODO: CHANGE TO CLASS?
    public struct PlayManagerState()
    {
        public HashSet<byte> DesiredKeys { get; set; } = [];
        public int NextMessageGroup { get; set; } = 0;
        public int MessageDelta { get; set; } = 0;
        public int TotalMessagesTime { get; set; } = 0;
    }

    public class PlayManager()
    {
        public List<SimpleTimedKeyGroup> EventGroups { get; private set; } = [];

        private HashSet<byte> nonreadyKeys = [];

        private PlayManagerState state;

        public event Action<PlayManagerState> OnTargetChanged;
        public event Action OnStopped;

        public enum PlayState
        {
            Ready,
            Stopped,
        }
        PlayState playState = PlayState.Stopped;

        public void Setup(List<SimpleTimedKeyGroup> keyMessages)
        {
            EventGroups = keyMessages;
            state = new();
            playState = PlayState.Ready;

            NextTarget();
        }

        public void Stop()
        {
            playState = PlayState.Stopped;
            state = new();
            OnStopped?.Invoke();
        }

        public void NextTarget()
        {
            if (EventGroups.Count == 0) throw new Exception("PlayManager is not initialized");

            if (state.NextMessageGroup > EventGroups.Count - 1)
            {
                Stop();
                OnTargetChanged?.Invoke(state);
                return;
            }

            state.TotalMessagesTime += state.MessageDelta;

            var group = EventGroups[state.NextMessageGroup];

            state.DesiredKeys = group.Keys;
            state.MessageDelta = group.DeltaTime;

            OnTargetChanged?.Invoke(state);
            state.NextMessageGroup++;
        }

        public void OnKeyChange(HashSet<byte> pressedKeys)
        {
            if (playState == PlayState.Stopped) return;

            nonreadyKeys = nonreadyKeys.Intersect(pressedKeys).ToHashSet();

            if (state.DesiredKeys.Except(pressedKeys.Except(nonreadyKeys)).Any()) return;

            NextTarget();
            nonreadyKeys = new(pressedKeys);
        }
    }
}
