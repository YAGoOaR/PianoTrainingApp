using System;

namespace PianoTrainer.Scripts.MIDI
{
    // TODO: CHECK GODOT CLASS UPDATE ORDER

    //TODO: ADD NOTE TIME SKIP OPTIONS
    public class TimelineManager()
    {
        public int CurrentTimeMilis { get; private set; } = 0;
        public int TimeToNextKey { get; private set; } = 0;

        public DateTime CheckPoint { get; set; } = DateTime.MinValue;

        private float timeFromCheckpointMilis = 0;

        private PlayManagerState state;

        public void Update(float dT)
        {
            timeFromCheckpointMilis += dT * 1000f;
            if (timeFromCheckpointMilis > state.MessageDelta)
            {
                timeFromCheckpointMilis = state.MessageDelta;
            }

            // TODO: FIX VARIABLES 1 TICK DELAY
            CurrentTimeMilis = state.TotalMessagesTime + (int)timeFromCheckpointMilis;
            TimeToNextKey = state.MessageDelta - (int)timeFromCheckpointMilis;
        }

        public void OnTargetChange(PlayManagerState state)
        {
            this.state = state;
            CheckPoint = DateTime.Now;
            timeFromCheckpointMilis = 0;

            CurrentTimeMilis = state.TotalMessagesTime + (int)timeFromCheckpointMilis;
            TimeToNextKey = state.MessageDelta - (int)timeFromCheckpointMilis;
        }
    }
}
