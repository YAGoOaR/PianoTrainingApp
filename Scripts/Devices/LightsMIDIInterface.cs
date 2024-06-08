
using Commons.Music.Midi;
using PianoTrainer.Scripts.MusicNotes;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace PianoTrainer.Scripts.Devices;

// Defines MIDI messages to work with the piano
public static class LightsMIDIInterface
{
    // Midi message to control CASIO LK-S250 key lights.
    public static bool SendNoteChange(IMidiOutput piano, NoteMessage msg)
    {
        try
        {
            piano.Send([MidiEvent.SysEx1, 68, 126, 126, 127, 2, 0, msg.Key, Convert.ToByte(msg.State), MidiEvent.EndSysEx], 0, 10, 0);
            return true;
        }
        catch (Win32Exception)
        {
            Debug.WriteLine("Device disconnected.");
            return false;
        }
    }

    // Midi message that informs piano that light messages are incoming. Must be sent periodically.
    public static bool SendHold(IMidiOutput piano)
    {
        try
        {
            piano.Send([MidiEvent.SysEx1, 68, 126, 126, 127, 0, 3, MidiEvent.EndSysEx], 0, 8, 0);
            return true;
        }
        catch (Win32Exception)
        {
            Debug.WriteLine("Device disconnected.");
            return false;
        }

    }
}
