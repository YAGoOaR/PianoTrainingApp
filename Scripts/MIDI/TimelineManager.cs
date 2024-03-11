using System;
using static Godot.WebSocketPeer;
using System.Diagnostics;

namespace PianoTrainer.Scripts.MIDI
{
    // TODO: CHECK GODOT CLASS UPDATE ORDER

    //TODO: ADD NOTE TIME SKIP OPTIONS
    public class TimelineManager()
    {
        public int CurrentTimeMilis { get => state.TotalMessagesTime + (int)TimeSinceLastKey; }
        public int TimeToNextKey { get => state.MessageDelta - (int)TimeSinceLastKey; }

        public float TimeSinceLastKey { get; private set; } = 0;

        public DateTime CheckPoint { get; set; } = DateTime.MinValue;

        private PlayManagerState state;

        public void Update(float dT)
        {
            TimeSinceLastKey = Math.Min(TimeSinceLastKey + dT * 1000f, state.MessageDelta);
        }

        public void OnTargetChange(PlayManagerState state)
        {
            this.state = state; //TODO: REMOVE
            CheckPoint = DateTime.Now;
            TimeSinceLastKey = 0;
        }
    }
}
