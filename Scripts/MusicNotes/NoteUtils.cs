
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PianoTrainer.Scripts.MusicNotes;

public class NoteUtils
{
    public static List<TimedNoteGroup> AddTimePadding(List<TimedNoteGroup> keyGroups, int startTimePadding)
    {
        if (keyGroups.Count > 0)
        {
            var (firstMsg, rest) = (keyGroups.First(), keyGroups[1..]);
            var endTimePadding = keyGroups.Last().Notes.Select(x => x.Duration).Max();
            return [
                new(0, []),
                new(startTimePadding, firstMsg.Notes, firstMsg.MaxDuration),
                .. rest,
                new(endTimePadding, [])
            ];
        }
        return keyGroups;
    }

    public static List<TimedNoteGroup> NoteGroupsToAbsoluteTime(List<TimedNoteGroup> keyGroups)
    {
        int timeAcc = 0;
        List<TimedNoteGroup> noteGroupsAbsTime = [];

        foreach (var group in keyGroups)
        {
            timeAcc += group.Time;
            noteGroupsAbsTime.Add(new(timeAcc, group.Notes, group.MaxDuration));
        }

        return noteGroupsAbsTime;
    }

    public static List<TimedNote> NoteMessageToPressData(List<TimedNoteMessage> keyMessages, Func<byte, bool> keyFitCriteria)
    {
        Dictionary<byte, (int idx, int relTime, int absTime, TimedNoteMessage msg)> pressedKeys = [];
        List<(int idx, TimedNote note)> releasedKeys = [];

        var absoluteTime = 0;
        var relativeTime = 0;

        for (int i = 0; i < keyMessages.Count; i++)
        {
            var msg = keyMessages[i];
            absoluteTime += msg.Time;
            relativeTime += msg.Time;

            if (keyFitCriteria(msg.Key))
            {
                if (msg.State)
                {
                    if (pressedKeys.ContainsKey(msg.Key)) continue;

                    pressedKeys[msg.Key] = (i, relativeTime, absoluteTime, msg);
                    relativeTime = 0;
                }
                else
                {
                    if (!pressedKeys.ContainsKey(msg.Key)) continue;

                    var (idx, startRelativeTime, absOpenTime, openedMsg) = pressedKeys[msg.Key];

                    var duration = absoluteTime - absOpenTime;

                    (int, TimedNote) closedNote = (idx, new(openedMsg.Key, startRelativeTime, duration));

                    pressedKeys.Remove(msg.Key);
                    releasedKeys.Add(closedNote);
                }
            }
        }

        return releasedKeys.OrderBy(x => x.idx).Select(x => x.note).ToList();
    }

    public static List<TimedNoteGroup> GroupNotes(List<TimedNote> keyMessages)
    {
        List<TimedNoteGroup> groups = [];

        int eventDelay = 0;
        HashSet<NotePress> currentGroup = [];
        int maxDuration = 0;

        for (int i = 0; i < keyMessages.Count; i++)
        {
            var msg = keyMessages[i];

            if (msg.Time == 0)
            {
                currentGroup.Add(msg);
                maxDuration = Mathf.Max(maxDuration, msg.Duration);
            }
            else
            {
                if (i != 0) groups.Add(new(eventDelay, currentGroup, maxDuration));
                currentGroup = [msg];
                maxDuration = msg.Duration;
                eventDelay = msg.Time;
            }
        }

        groups.Add(new(eventDelay, currentGroup, maxDuration));
        return groups;
    }
}
