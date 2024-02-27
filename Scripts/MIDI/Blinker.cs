using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PianoTrainer.Scripts.MIDI.MIDIPlayer;

namespace PianoTrainer.Scripts.MIDI
{
    public class Blinker : IDisposable
    {
        const int blinkOffset = 900;
        const int blinkOutdatedOffset = 600;

        private int blinkedMessageTimeAccumulator = 0;
        private int skipped = 0;

        List<SimpleTimedKey> messageList;
        readonly MIDIPlayer player;
        PlayerSettings settings;
        readonly KeyLightsManager lightsManager;

        public Blinker(List<SimpleTimedKey> messageList, MIDIPlayer player, PlayerSettings settings, KeyLightsManager lightsManager)
        {
            this.player = player;
            this.messageList = messageList;
            this.settings = settings;
            this.lightsManager = lightsManager;

            lightsManager.PreTick += OnPreTick;
        }

        public void OnPreTick()
        {
            bool contCondition = true;
            bool lastTimeCompleted = false;

            var nextMsg2Time = int.MaxValue;

            while (contCondition && messageList.Count > 0 && skipped < player.CurrentMessageIndex + settings.PreBlinkCount || lastTimeCompleted && nextMsg2Time == 0)
            {
                var nextMsg = messageList.First();

                int currentRelativeTime = Math.Min(player.MessageTimeAccumulator + (int)(DateTime.Now - player.PressTime).TotalMilliseconds, player.NextMsgRelTime);
                int selectedMessageTime = blinkedMessageTimeAccumulator + nextMsg.DeltaTime;

                bool completed = selectedMessageTime - currentRelativeTime < blinkOffset;
                bool outdated = selectedMessageTime - currentRelativeTime < blinkOutdatedOffset && settings.ShowNotes;

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

        public void Dispose()
        {
            lightsManager.PreTick -= OnPreTick;
            GC.SuppressFinalize(this);
        }

    }
}
