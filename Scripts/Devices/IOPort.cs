
using Commons.Music.Midi;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts.Devices;

// Base class for input and output MIDI ports
public abstract class IOPort<T>(string portName) where T : IMidiPort
{
    private const int PORT_UPDATE_TIME = 1000;

    protected static readonly IMidiAccess MIDIAccess = MidiAccessManager.Default;

    protected IMidiPort port = null;

    public bool IsConnected { get => port != null; }

    private static bool GetPort(string portName, out IMidiPortDetails port)
    {
        var ports = typeof(T) == typeof(IMidiInput) ? MIDIAccess.Inputs : MIDIAccess.Outputs;
        var foundPorts = from deviceName in ports where deviceName.Name == portName select deviceName;

        bool isFound = foundPorts.Any();
        port = isFound ? foundPorts.First() : null;

        return isFound;
    }

    protected async Task<IMidiPortDetails> WaitForPort()
    {
        if (!GetPort(portName, out var details))
        {
            Debug.WriteLine($"Port \"{portName}\" not found!");
            Debug.WriteLine($"Waiting for device to be available.");

            while (!GetPort(portName, out details))
                await Task.Delay(PORT_UPDATE_TIME);
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
        var details = await WaitForPort();

        var port = await MIDIAccess.OpenInputAsync(details.Id);

        base.port = port;
        return port;
    }
}

public class OutputPort(string portName) : IOPort<IMidiOutput>(portName)
{
    public async Task<IMidiOutput> OpenPort()
    {
        var details = await WaitForPort();

        var port = await MIDIAccess.OpenOutputAsync(details.Id);

        base.port = port;
        return port;
    }
}
