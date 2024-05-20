using Commons.Music.Midi;
using PianoTrainer.MIDI;
using System;

namespace PianoTrainer.Scripts.MIDI;

public class KeyboardInterface(IMidiOutput piano)
{
    private readonly IMidiOutput piano = piano;

    public void SendProprietary(SimpleMsg msg)
    {
        piano.Send([MidiEvent.SysEx1, 68, 126, 126, 127, 2, 0, msg.Key, Convert.ToByte(msg.State), MidiEvent.EndSysEx], 0, 10, 0);
    }
    public void SendHold()
    {
        piano.Send([MidiEvent.SysEx1, 68, 126, 126, 127, 0, 3, MidiEvent.EndSysEx], 0, 8, 0);
    }
}
