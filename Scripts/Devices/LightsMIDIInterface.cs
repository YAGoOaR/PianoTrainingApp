using Commons.Music.Midi;
using PianoTrainer.Scripts.PianoInteraction;
using System;

namespace PianoTrainer.Scripts.Devices;

public class LightsMIDIInterface(IMidiOutput piano)
{
    private readonly IMidiOutput piano = piano;

    // Midi message to control CASIO LK-S250 key lights.
    public void SendProprietary(SimpleMsg msg)
    {
        piano.Send([MidiEvent.SysEx1, 68, 126, 126, 127, 2, 0, msg.Key, Convert.ToByte(msg.State), MidiEvent.EndSysEx], 0, 10, 0);
    }

    // Midi message that informs piano that light messages are incoming. Must be sent periodically.
    public void SendHold()
    {
        piano.Send([MidiEvent.SysEx1, 68, 126, 126, 127, 0, 3, MidiEvent.EndSysEx], 0, 8, 0);
    }
}
