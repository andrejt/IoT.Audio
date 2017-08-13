using System;
using System.Threading;
using Windows.ApplicationModel.Background;
using Microsoft.Extensions.DependencyInjection;

namespace IoT.Audio
{
    public sealed class StartupTask : IBackgroundTask
    {
        private BackgroundTaskDeferral deferral;
        private Timer clockTimer;
        private SpeechService speechService;
        private Webserver webServer;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();

            var servicesBuilder = new ServiceCollection()
                .AddSingleton<SpeechService>()
                ;
            Services = servicesBuilder.BuildServiceProvider();

            speechService = Services.GetService<SpeechService>();

            var timeToFullHour = GetTimeSpanToNextFullHour();
            clockTimer = new Timer(OnClock, null, timeToFullHour, TimeSpan.FromHours(1));

            webServer = new Webserver();

            await webServer.StartAsync();
            await speechService.SayTime();
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

        internal static IServiceProvider Services { get; private set; }
    }
}
