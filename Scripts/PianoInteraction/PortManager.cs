using Commons.Music.Midi;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts.PianoInteraction;

internal abstract class PortManager<T>(string portName) where T : IMidiPort
{
    private const int waitTime = 1000;

    private static bool GetPort(string portName, out IMidiPortDetails port)
    {
        var access = MidiAccessManager.Default;
        var ports = typeof(T) == typeof(IMidiInput) ? access.Inputs : access.Outputs;
        var foundPorts = from deviceName in ports where deviceName.Name == portName select deviceName;

        bool found = foundPorts.Any();
        port = found ? foundPorts.First() : null;

        return found;
    }

    protected async Task<IMidiPortDetails> GetPortDetails()
    {
        if (!GetPort(portName, out var details))
        {
            Debug.WriteLine($"Port \"{portName}\" not found!");
            Debug.WriteLine($"Waiting for device to be available.");

            await Task.Run(async () =>
            {
                while (!GetPort(portName, out details)) 
                    await Task.Delay(waitTime);
            });
        }

        return details;
    }

    public abstract void ListDevices();
}

internal class InputPortManager(string portName) : PortManager<IMidiInput>(portName)
{
    public async Task<IMidiInput> OpenPort()
    {
        var access = MidiAccessManager.Default;
        var details = await GetPortDetails();

        return await access.OpenInputAsync(details.Id);
    }

    public override void ListDevices()
    {
        Debug.WriteLine("Available input devices:");
        MidiAccessManager.Default.Inputs.ToList().ForEach(x => Debug.WriteLine(x.Name));
        Debug.WriteLine("");
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

    public override void ListDevices()
    {
        Debug.WriteLine("Available output devices:");
        MidiAccessManager.Default.Outputs.ToList().ForEach(x => Debug.WriteLine(x.Name));
        Debug.WriteLine("");
    }
}
