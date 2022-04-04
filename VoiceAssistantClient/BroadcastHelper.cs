using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Win32;
using System;
using System.IO;
using System.Diagnostics;

namespace VoiceAssistantClient
{
    public static class BroadcastHelper
    {
        public static bool Broadcasting = false;

        public static SpeechSynthesizer Broadcastingspeaker = new SpeechSynthesizer(SpeechConfig.FromSubscription("9458ed386eb348cfb85afb8902749d9b", "eastus"));

        public static bool BroadcastInterrupted = false;

        public static bool BroadcastingResumed = false;

        public static System.Timers.Timer ReaptTimer = new System.Timers.Timer();

        public static string BroadcastScript;

        public static bool Abort = false;

        public static bool Suspended = false;

        public static void BroadcastResumeTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Start Broadcasting
            Broadcastingspeaker.StopSpeakingAsync();
            ReaptTimer.Stop();
            ResumeBroadcasting();
        }

        public static System.Timers.Timer BroadcastResumeTimer = new System.Timers.Timer();

        public static string Broadcasting_image;

        public static string Broadcasting_script;

        public static string information;

        public static bool InfoRetrieved = false;

        public static void GetBroadcastInfo()
        {
            // Broadcasting = true;

            var fileContent = string.Empty;
            var filePath = string.Empty;
            var openDlg = new Microsoft.Win32.OpenFileDialog();

            openDlg.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            bool? result = openDlg.ShowDialog();

            // Return if canceled.
            if (!(bool)result)
            {
                return;
            }

            filePath = openDlg.FileName;
            var fileStream = openDlg.OpenFile();

            using (StreamReader reader = new StreamReader(fileStream))
            {
                fileContent = reader.ReadToEnd();
                // Debug.WriteLine(fileContent);
                if (fileContent.Contains("+"))
                {
                    char[] separator = { '+' };
                    string[] arr = fileContent.Split(separator);
                    string data = arr[0];
                    GlobalData.BroadcastingImage = data;
                    BroadcastScript = arr[1];
                    Debug.WriteLine("InfoUpdated");
                    InfoRetrieved = true;
                }
            }
        }

        public static void StartBroadcasting(string script)
        {
            if (InfoRetrieved)
            {
                Broadcasting = true;

                BroadcastScript = script;

                Broadcastingspeaker.StopSpeakingAsync();
                Broadcastingspeaker.SpeakTextAsync(script).Wait();

                ReaptTimer.Interval = 2000;
                ReaptTimer.Elapsed += ReaptTimer_Elapsed;
                ReaptTimer.AutoReset = false;
                ReaptTimer.Start();
            }
        }

        public static void StartBroadcasting()
        {
            Abort = false;

            if (!Abort)
            {
                if (InfoRetrieved)
                {
                    Broadcasting = true;

                    Broadcastingspeaker.StopSpeakingAsync();
                    Broadcastingspeaker.SpeakTextAsync(BroadcastScript).Wait();

                    ReaptTimer.Interval = 3000;
                    ReaptTimer.Elapsed += ReaptTimer_Elapsed;
                    ReaptTimer.AutoReset = false;
                    ReaptTimer.Start();
                }
            }
        }

        public static void BroadcastingSuspend()
        {
            Suspended = true;
            //Broadcastingspeaker.StopSpeakingAsync();
            ReaptTimer.Stop();
            ResumeTimer_Stop();
        }

        public static void ResumeBroadcasting()
        {
            if (!Abort)
            {
                if (!Suspended)
                {
                    Broadcastingspeaker.StopSpeakingAsync();
                    Broadcastingspeaker.SpeakTextAsync(BroadcastScript).Wait();

                    ReaptTimer.Interval = 4000;
                    ReaptTimer.Elapsed += ReaptTimer_Elapsed;
                    ReaptTimer.AutoReset = false;
                    ReaptTimer.Start();
                }
            }
        }

        public static void Stop()
        {
            Abort = true;
            Broadcasting = false;
            InfoRetrieved = false;
            Broadcastingspeaker.StopSpeakingAsync();
            ReaptTimer.Stop();
            ResumeTimer_Stop();
        }

        private static void ReaptTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ReaptTimer.Stop();
            ResumeTimer_Stop();
            ResumeBroadcasting();
        }

        public static async Task SynthesizeAudioAsync(string content)
        {
            Broadcastingspeaker.StopSpeakingAsync();

            await Broadcastingspeaker.SpeakTextAsync(content);
        }

        public static void ResumeTimer_Start()
        {
            ReaptTimer.Stop();
            BroadcastResumeTimer.Interval = 6000;
            BroadcastResumeTimer.Elapsed += BroadcastResumeTimer_Elapsed;
            BroadcastResumeTimer.AutoReset = false;
            BroadcastResumeTimer.Start();
        }

        public static void ResumeTimer_Stop()
        {
            ReaptTimer.Stop();
            Broadcastingspeaker.StopSpeakingAsync();
            BroadcastResumeTimer.Stop();
        }
    }
}
