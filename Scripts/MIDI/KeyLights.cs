using Commons.Music.Midi;
using CoreMidi;
using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace PianoTrainer.Scripts.MIDI
{
    public record SimpleMsg(byte Key, bool State);

    public class KeyLights
    {
        public Action OnError;

        private IMidiOutput piano;

        private bool closed = false;

        public KeyLights(IMidiOutput piano)
        {
            this.piano = piano;
        }

        public void SendProprietary(SimpleMsg msg)
        {
            if (closed) return;
            try
            {
                piano.Send([MidiEvent.SysEx1, 68, 126, 126, 127, 2, 0, msg.Key, Convert.ToByte(msg.State), MidiEvent.EndSysEx], 0, 10, 0);
            } catch
            {
                Debug.WriteLine("Throwing error from Send...");
                OnError?.Invoke();
                closed = true;
                throw new MidiException("Connection to MIDI device lost.");
            }
        }
        public void SendHold()
        {
            if (closed) return;
            try
            {
                piano.Send([MidiEvent.SysEx1, 68, 126, 126, 127, 0, 3, MidiEvent.EndSysEx], 0, 8, 0);
            }
            catch
            {
                Debug.WriteLine("Throwing error from Hold...");
                OnError?.Invoke();
                closed = true;
                throw new MidiException("Connection to MIDI device lost.");
            }
        }
    }
}
