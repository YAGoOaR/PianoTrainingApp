
using Commons.Music.Midi;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts.Devices;

public abstract class IOPort<T>(string portName) where T : IMidiPort
{
    private const int waitTime = 1000;

    protected IMidiPort port = null;

    public bool IsConnected { get => port != null; }

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

            while (!GetPort(portName, out details))
                await Task.Delay(waitTime);
        }

        return details;
    }

    public async Task ClosePort()
    {
        Task closingTask = port?.CloseAsync();

        port = null;

        await closingTask;
    }
}

public class InputPort(string portName) : IOPort<IMidiInput>(portName)
{
    public async Task<IMidiInput> OpenPort()
    {
        var access = MidiAccessManager.Default;
        var details = await GetPortDetails();

        var port = await access.OpenInputAsync(details.Id);

        base.port = port;
        return port;
    }
}

public class OutputPort(string portName) : IOPort<IMidiOutput>(portName)
{
    public async Task<IMidiOutput> OpenPort()
    {
        var access = MidiAccessManager.Default;
        var details = await GetPortDetails();

        var port = await access.OpenOutputAsync(details.Id);

        base.port = port;
        return port;
    }
}
