using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts.MIDI
{
    public record SimpleMsg(byte Key, bool State);
    public record SimpleTimedMsg(byte Key, bool State, int DeltaTime) : SimpleMsg(Key, State);
}
