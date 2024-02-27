
using Commons.Music.Midi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PianoTrainer.Scripts.MIDI;

public class PreBlinkPlayer(KeyState piano, KeyLightsManager lightsManager, int preBlinkCount = 1, bool showNotes = true)
{
    public event Action<byte, bool> OnPlayedNote;

    public DateTime LastTimeCheck { get => pressTime; }
    public int RelativeMessageTime { get => messageTimeAccumulator; }
    public int TotalTimeMilis { get; private set; } = 0;
    public int TimeToNextMsg { get; private set; } = 0;

    public KeyState keyLightState = lightsManager.LightsState;

    private const int keyTimeOffset = -400;
    private const int startOffset = 3000;
    const int blinkOffset = 900;
    const int blinkOutdatedOffset = 600;

    private readonly KeyState piano = piano;
    private HashSet<byte> nonreadyKeys = [];
    private MidiMusic music;
    private readonly KeyLightsManager lightsManager = lightsManager;

    private int currentTempo = 500000;
    private readonly double tempo_ratio = 1.0;

    private int currentMessageIndex = 0;
    private DateTime pressTime = DateTime.MinValue;
    
    private int blinkedMessageTimeAccumulator = 0;
    private int nextMsgRelTime = 0;
    private int skipped = 0;

    private int messageTimeAccumulator = 0;

    private List<SimpleTimedMsg> messageList = [];

    private void PreTick()
    {
        bool contCondition = true;
        bool lastTimeCompleted = false;

        var nextMsg2Time = int.MaxValue;

        while (contCondition && messageList.Count > 0 && skipped < currentMessageIndex + preBlinkCount || lastTimeCompleted && nextMsg2Time == 0)
        {
            var nextMsg = messageList.First();

            int currentRelativeTime = Math.Min(messageTimeAccumulator + (int)(DateTime.Now - pressTime).TotalMilliseconds, nextMsgRelTime);
            int selectedMessageTime = blinkedMessageTimeAccumulator + nextMsg.DeltaTime;

            bool completed = selectedMessageTime - currentRelativeTime < blinkOffset;
            bool outdated = selectedMessageTime - currentRelativeTime < blinkOutdatedOffset && showNotes;

            if (completed && !outdated)
            {
                var msg = messageList.First();
                lightsManager.AddBlink(msg.Key);
            }

            contCondition = completed || outdated;

            if (contCondition)
            {
                blinkedMessageTimeAccumulator += messageList.First().DeltaTime;
                messageList = messageList[1..];
                nextMsg2Time = messageList.Count > 0 ? messageList.First().DeltaTime : int.MaxValue;
                skipped++;
            }
            lastTimeCompleted = contCondition;
        }
    }

    public void Load(string filename)
    {
        music = MidiMusic.Read(File.OpenRead(filename));
    }

    private int GetContextDeltaTime(int deltaTimeSpec, int deltaTime)
    {
        return (int)(currentTempo / 1000 * deltaTime / deltaTimeSpec / tempo_ratio);
    }

    private void ProcessMetaMessages(IEnumerable<MidiMessage> messages)
    {
        foreach (var m in messages)
        {
            if (m.Event.StatusByte == byte.MaxValue && m.Event.Msb == 81)
            {
                currentTempo = MidiMetaType.GetTempo(m.Event.ExtraData, m.Event.ExtraDataOffset);
                Debug.WriteLine($"Set current tempo to {currentTempo}");
            }
        }
    }

    public void KeyUpdater(byte key, bool state) => nonreadyKeys = nonreadyKeys.Intersect(piano.State).ToHashSet();

    private IList<MidiMessage> MergeTracks(IList<MidiTrack> tracks)
    {
        var messageLists = tracks.Select(x => new Queue<MidiMessage>(x.Messages)).ToList();
        var counts = messageLists.Select(x => x.Count);

        var timelines = messageLists.Select(x => 0).ToList();
        var msgTimeline = 0;

        List<MidiMessage> result = [];

        while (counts.Any(x => x > 0))
        {
            var (idx, q) = messageLists.Select((x, i) => (i, x)).OrderBy(x => x.x.Count > 0 ? timelines[x.i] + x.x.First().DeltaTime : int.MaxValue).First();

            var msg = q.Dequeue();
            var dt = msg.DeltaTime + timelines[idx] - msgTimeline;
            var msgCorrected = new MidiMessage(dt, msg.Event);
            timelines[idx] += msg.DeltaTime;
            msgTimeline += dt;
            result.Add(msgCorrected);
            counts = messageLists.Select(x => x.Count);
        }

        return result;
    }

    public void Play()
    {
        if (music == null)
        {
            Debug.WriteLine("Error: music is not loaded.");
            return;
        }

        messageTimeAccumulator = 0;
        blinkedMessageTimeAccumulator = 0;
        currentMessageIndex = 0;
        skipped = 0;

        piano.KeyChange += KeyUpdater;
        lightsManager.PreTick += PreTick;

        if (music == null)
        {
            throw new ArgumentException("MIDI file is not loaded.");
        }

        var tracks = music.Tracks;
        var deltaTimeSpec = music.DeltaTimeSpec;

        pressTime = DateTime.Now;

        Debug.WriteLine($"Tracks count: {tracks.Count}");

        IList<MidiMessage> trackMessages = MergeTracks(tracks);

        ProcessMetaMessages(trackMessages);

        // TODO: CHECK FOR OTHER POSSIBLE MESSAGES TO ADD TIME
        var messages =
            trackMessages
            .Where(m => m.Event.EventType == MidiEvent.NoteOn || m.Event.EventType == MidiEvent.NoteOff)
            .Select(m => new SimpleTimedMsg(m.Event.Msb, m.Event.EventType == MidiEvent.NoteOn && m.Event.Lsb != 0, m.DeltaTime))
            .ToList();

        List<SimpleTimedMsg> messagesOn = [];

        var endTime = messages.Aggregate(0, (t, msg) =>
        {
            if (msg.State && lightsManager.LightsState.IsAcceptable(msg.Key))
            {
                var dT = t + msg.DeltaTime;

                var delta = GetContextDeltaTime(deltaTimeSpec, dT);

                messagesOn.Add(new(msg.Key, true, delta));
                return 0;
            }
            else
            {
                return t + msg.DeltaTime;
            }
        });

        if (messagesOn.Count > 0)
        {
            var firstMsg = messagesOn.First();
            var firstTime = firstMsg.DeltaTime + startOffset;
            nextMsgRelTime = firstTime;
            messagesOn = messagesOn[1..].Prepend(new(firstMsg.Key, true, firstTime)).ToList();
        }

        messageList = new(messagesOn);

        TotalTimeMilis = messagesOn.Select(x => x.DeltaTime).Sum() + endTime;


        for (int i = 0; i < messagesOn.Count;)
        {
            currentMessageIndex = i;

            var selected = messagesOn.Skip(i).TakeWhile((m, j) => j == 0 || m.DeltaTime == 0);
            var selectedCount = selected.Count();

            var newKeys = selected.Where(x => x.State).Select(x => x.Key).ToList();

            var messageDelta = selected.First().DeltaTime;

            var timeFromLastMsg = DateTime.Now - pressTime;
            var rawDurationToNextEvent = messageDelta - (int)timeFromLastMsg.TotalMilliseconds;
            var durationToNextEvent = rawDurationToNextEvent + keyTimeOffset;

            TimeToNextMsg = rawDurationToNextEvent;

            Thread.Sleep(Math.Max(durationToNextEvent, 0));

            if (showNotes)
            {
                lightsManager.SetKeys(newKeys);
            }

            if (newKeys.Count > 0)
            {
                var proceed = new TaskCompletionSource();

                void callback(byte key, bool state)
                {
                    if (newKeys.Except(piano.State.Except(nonreadyKeys)).Any()) return;

                    pressTime = DateTime.Now;
                    messageTimeAccumulator += messageDelta;

                    if (messagesOn.Count > i + selectedCount)
                    {
                        nextMsgRelTime = messageTimeAccumulator + messagesOn[i + selectedCount].DeltaTime;
                    }

                    nonreadyKeys = new(piano.State);

                    lightsManager.Reset();

                    proceed.TrySetResult();
                    piano.KeyChange -= callback;
                }

                piano.KeyChange += callback;

                proceed.Task.Wait();
            }

            i += selectedCount;
        }
        Thread.Sleep(endTime);
        
        piano.KeyChange -= KeyUpdater;
        lightsManager.PreTick -= PreTick;
    }
}
