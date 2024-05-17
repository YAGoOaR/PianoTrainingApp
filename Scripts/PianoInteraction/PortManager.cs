using Commons.Music.Midi;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts.MIDI
{
    internal abstract class PortManager<T>(string portName) where T : IMidiPort
    {
        private static IMidiPortDetails GetPort(string portName)
        {
            var access = MidiAccessManager.Default;
            var ports = typeof(T) == typeof(IMidiInput) ? access.Inputs : access.Outputs;
            var found = from deviceName in ports where deviceName.Name == portName select deviceName;

            if (found.Any()) return found.First();
            return null;
        }

        protected async Task<IMidiPortDetails> GetPortDetails()
        {
            var details = GetPort(portName);

            if (details == null)
            {
                Debug.WriteLine($"Port \"{portName}\" not found!");
                Debug.WriteLine($"Waiting for device to be available.");

                return await Task.Run(async () =>
                {
                    while (details == null)
                    {
                        await Task.Delay(1000);
                        details = GetPort(portName);
                    }
                    return details;
                });
            }
            else
            {
                return details;
            }
        }
    }

    internal class InputPortManager(string portName) : PortManager<IMidiInput>(portName)
    {
        public async Task<IMidiInput> OpenPort()
        {
            var access = MidiAccessManager.Default;
            var details = await GetPortDetails();

            return await access.OpenInputAsync(details.Id);
        }
    }

    internal class OutputPortManager(string portName) : PortManager<IMidiOutput>(portName)
    {
        public async Task<IMidiOutput> OpenPort()
        {
            var access = MidiAccessManager.Default;
            var details = await GetPortDetails();

            return await access.OpenOutputAsync(details.Id);
        }
    }

}
