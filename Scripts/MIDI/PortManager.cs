using Commons.Music.Midi;
using System;
using System.Threading;
using System.Linq;
using System.Diagnostics;

namespace PianoTrainer.Scripts.MIDI
{

    internal abstract class PortManager<T> where T : IMidiPort
    {
        public IMidiPortDetails PortDetails { get; set; }
        protected readonly IMidiAccess access;

        private IMidiPortDetails GetPort(string portName)
        {
            var ports = typeof(T) == typeof(IMidiInput) ? access.Inputs : access.Outputs;
            var found = from deviceName in ports where deviceName.Name == portName select deviceName;

            if (found.Any())
            {
                return found.First();
            }
            return null;
        }

        public PortManager(string portName)
        {
            access = MidiAccessManager.Default;

            var details = GetPort(portName);

            if (details == null)
            {
                Debug.WriteLine($"Port \"{portName}\" not found!");
                Debug.WriteLine($"Waiting for device to be available.");

                while (details == null)
                {
                    Thread.Sleep(1000);
                    details = GetPort(portName);
                }
            }

            PortDetails = details;
        }

        public abstract T OpenPort();
    }

    internal class InputPortManager : PortManager<IMidiInput>
    {
        public InputPortManager(string portName) : base(portName) { }

        public override IMidiInput OpenPort()
        {
            return access.OpenInputAsync(PortDetails.Id).Result;
        }
    }

    internal class OutputPortManager : PortManager<IMidiOutput>
    {
        public OutputPortManager(string portName) : base(portName) { }

        public override IMidiOutput OpenPort()
        {

            return access.OpenOutputAsync(PortDetails.Id).Result;
        }
    }

}
