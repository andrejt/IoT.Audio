using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace IoT.Audio
{
    internal class SpeechService
    {
        private readonly SpeechSynthesizer speechSynthesizer;
        private readonly MediaPlayer speechPlayer;

        public SpeechService()
        {
            speechSynthesizer = CreateSpeechSynthesizer();
            speechPlayer = new MediaPlayer();
        }

        private static SpeechSynthesizer CreateSpeechSynthesizer()
        {
            var synthesizer = new SpeechSynthesizer();
            var voice = SpeechSynthesizer.AllVoices.SingleOrDefault(i => i.Gender == VoiceGender.Female) ?? SpeechSynthesizer.DefaultVoice;
            synthesizer.Voice = voice;
            return synthesizer;
        }
    
        public async Task SayAsync(string text)
        {
            using (var stream = await speechSynthesizer.SynthesizeTextToStreamAsync(text))
            {
                speechPlayer.Source = MediaSource.CreateFromStream(stream, stream.ContentType);
            }
            speechPlayer.Play();
        }

        public bool IsQuietHoursTime()
        {
            var now = DateTime.Now;
            //TODO: quiet hours
            return now.Hour < 7 || now.Hour > 21;

        }

        public async Task SayTime()
        {
            var now = DateTime.Now;
            var hour = now.Hour;

            string timeOfDay;
            if (hour <= 12)
            {
                timeOfDay = "morning";
            }
            else if (hour <= 17)
            {
                timeOfDay = "afternoon";
            }
            else
            {
                timeOfDay = "evening";
            }

            if (hour > 12)
            {
                hour -= 12;
            }
            if (now.Minute == 0)
            {
                await SayAsync($"Good {timeOfDay}, it's {hour} o'clock.");
            }
            else
            {
                await SayAsync($"Good {timeOfDay}, it's {hour} {now.Minute}.");
            }
        }
    }
}