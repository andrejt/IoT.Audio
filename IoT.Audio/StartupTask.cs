using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading;
using Windows.ApplicationModel.Background;
using Windows.Media.SpeechSynthesis;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace IoT.Audio
{
    public sealed class StartupTask : IBackgroundTask
    {
        private BackgroundTaskDeferral deferral;
        private Timer clockTimer;
        private SpeechService speechService;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();

            speechService = new SpeechService();

            var timeToFullHour = GetTimeSpanToNextFullHour();
            clockTimer = new Timer(OnClock, null, timeToFullHour, TimeSpan.FromHours(1));

            speechService.SayTime();
        }

        private static TimeSpan GetTimeSpanToNextFullHour()
        {
            var now = DateTime.Now;
            var nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
            return nextHour - now;
        }

        private async void OnClock(object state)
        {
            await speechService.SayTime();
        }
    }
}
