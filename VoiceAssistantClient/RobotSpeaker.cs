using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using System.Threading;


namespace VoiceAssistantClient
{
    public static class RobotSpeaker
    {
        private static Mutex SpeechMutex = new Mutex();

        public static SpeechSynthesizer speaker_synthesizer = new SpeechSynthesizer(SpeechConfig.FromSubscription("9458ed386eb348cfb85afb8902749d9b", "eastus"));

        public static async Task SynthesizeAudioAsync(string content)
        {
            speaker_synthesizer.StopSpeakingAsync();

            await speaker_synthesizer.SpeakTextAsync(content);
        }
    }
}
