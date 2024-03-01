using System;

namespace PianoTrainer.Scripts.MIDI
{
    // TODO: CHECK GODOT CLASS UPDATE ORDER

    //TODO: ADD NOTE TIME SKIP OPTIONS
    public class TimelineManager()
    {
        public int CurrentTimeMilis { get; private set; } = 0;
        public int TimeToNextKey { get; private set; } = 0;

        public float TimeSinceLastKey { get; private set; } = 0;

        public DateTime CheckPoint { get; set; } = DateTime.MinValue;

        

        private PlayManagerState state;

        public void Update(float dT)
        {
            TimeSinceLastKey += dT * 1000f;
            if (TimeSinceLastKey > state.MessageDelta)
            {
                TimeSinceLastKey = state.MessageDelta;
            }

            // TODO: FIX VARIABLES 1 TICK DELAY
            CurrentTimeMilis = state.TotalMessagesTime + (int)TimeSinceLastKey;
            TimeToNextKey = state.MessageDelta - (int)TimeSinceLastKey;
        }

        public void OnTargetChange(PlayManagerState state)
        {
            this.state = state; //TODO: REMOVE
            CheckPoint = DateTime.Now;
            TimeSinceLastKey = 0;

            CurrentTimeMilis = state.TotalMessagesTime + (int)TimeSinceLastKey;
            TimeToNextKey = state.MessageDelta - (int)TimeSinceLastKey;
        }
    }
}
