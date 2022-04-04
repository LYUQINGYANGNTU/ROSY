using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InHouseRobot_Body;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace VoiceAssistantClient
{
    public static class TourHelper
    {
        public static List<string> LocationName = new List<string>();// { "start", "forest", "toilet", "maindoor", "firexit1", "firexit2", "exit", "sumatra"};
        public static List<string> LocationSpeech = new List<string>();// { "0", "0", "0", "0", "0", "0", "0", "0"};
        public static List<int> LocationDelay = new List<int>();// { 0, 0, 0, 0, 0, 0, 0, 0 };

        public static string CurrentGpoalLocation;
        public static string StandByLocation = "start";

        public static bool TourisInterruptedbyNavi = false;

        public static System.Timers.Timer ResumeTimer = new System.Timers.Timer();

        public static System.Timers.Timer FaceFunctionDelayTimer = new System.Timers.Timer();

        public static System.Timers.Timer ReturnTimer = new System.Timers.Timer();

        public static bool BacktoStandyLocation = false;

        public static BackgroundWorker speechworker = new BackgroundWorker();

        public static string CurrentArea;


        public static void GetTourInfo()
        {
            LocationName = new List<string> { "start", "forest", "toilet", "maindoor" };
            LocationSpeech = new List<string> { "0", "0", "0", "0" };
            LocationDelay = new List<int> { 0, 0, 0, 0 };
        }

        public static void GetTourInfo(string Area)
        {
            if (Area == "outside")
            {
                LocationName = new List<string> { "Waypoint11", "Waypoint10", "Waypoint9", "Waypoint8", "greenwall", "femaletoilet", "entrance", "maletoilet", "reception", "window", "luncharea" }; //{ "start", "forest", "toilet", "maindoor", "firexit1", "firexit2", "exit", "sumatra"};
                LocationSpeech = new List<string> { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };//{ "0", "0", "0", "0", "0", "0", "0", "0"};
                LocationDelay = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };//{ 0, 0, 0, 0, 0, 0, 0, 0 };
            }
            else if (Area == "inside")
            {
                LocationName = new List<string> { "start", "forest", "toilet", "maindoor", "firexit1", "firexit2", "exit", "sumatra" };
                LocationSpeech = new List<string> { "0", "0", "0", "0", "0", "0", "0", "0" };
                LocationDelay = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0 };
            }
            else
            {
                Debug.WriteLine("Invalid Input");
            }

        }

        public static void GoFirstPoint()
        {
            if (LocationName.Count > 0)
            {
                CurrentGpoalLocation = LocationName[1];
                BaseHelper.Go(LocationName[0]);
            }

            else
            {
                GlobalData.TourMode = false;
            }
        }

        public static void GoNextPoint(int NextLocationIndex)
        {
            CurrentGpoalLocation = LocationName[NextLocationIndex];
            BaseHelper.Go(LocationName[NextLocationIndex]);
        }

        public static void TourCanceled()
        {
            BacktoStandyLocation = true;
            // GlobalData.TourMode = false;
            ResumeTimer.Stop();
            BaseHelper.Go(StandByLocation);
        }

        public static void TourCanceled(string Area)
        {
            if (Area == "Outside")
            {
                BacktoStandyLocation = true;
                // GlobalData.TourMode = false;
                ResumeTimer.Stop();
                BaseHelper.Go("Waypoint3");
            }
            else if (Area == "Inside")
            {
                BacktoStandyLocation = true;
                // GlobalData.TourMode = false;
                ResumeTimer.Stop();
                BaseHelper.Go(StandByLocation);
            }
            else
            {
                Debug.WriteLine("Invalid Input");
            }
        }

        public static void TourInterrupted()
        {
            BaseHelper.CancelNavigation();

            string Designation = "";

            if (GlobalData.userDesignation == "Male")
            {
                Designation = "Gentleman";
            }
            else if (GlobalData.userDesignation == "Female")
            {
                Designation = "Lady";
            }
            else if (GlobalData.userDesignation == "Group")
            {
                Designation = "Guys";
            }

            // MainWindow.ArmMotionWorker.RunWorkerAsync();
            // Task.Factory.StartNew(() => MainWindow.armmotionevent());

            MainWindow.SynthesizeAudioAsync("Hello" + Designation + ", Press the button or simply say, Hey PIXA to activate me. After you hear the remider sound, then we can start the conversation").Wait();

            ResumeTimer.Interval = 8000;
            ResumeTimer.Elapsed += ResumeTimer_Elapsed;
            ResumeTimer.AutoReset = false;
            ResumeTimer.Start();
        }

        public static void ReturnInterrupted()
        {
            BaseHelper.CancelNavigation();

            string Designation = "";

            if (GlobalData.userDesignation == "Male")
            {
                Designation = "Gentleman";
            }
            else if (GlobalData.userDesignation == "Female")
            {
                Designation = "Lady";
            }
            else if (GlobalData.userDesignation == "Group")
            {
                Designation = "Guys";
            }

            // MainWindow.ArmMotionWorker.RunWorkerAsync();
            // Task.Factory.StartNew(() => MainWindow.armmotionevent());

            MainWindow.SynthesizeAudioAsync("Hello" + Designation + ", Press the button or simply say, Hey PIXA to activate me. After you hear the remider sound, then we can start the conversation").Wait();

            ReturnTimer.Interval = 8000;
            ReturnTimer.Elapsed += ReturnTimer_Elapsed;
            ReturnTimer.AutoReset = false;
            ReturnTimer.Start();
        }

        public static void ReturnTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            BaseHelper.Go(StandByLocation);
            TourHelper.TourisInterruptedbyNavi = false;

            FaceFunctionDelayTimer.Interval = 3000;
            FaceFunctionDelayTimer.Elapsed += FaceFunctionDelayTimer_Elapsed;
            FaceFunctionDelayTimer.AutoReset = false;
            FaceFunctionDelayTimer.Start();
        }

        public static void ResumeTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            BaseHelper.Go(CurrentGpoalLocation);
            TourHelper.TourisInterruptedbyNavi = false;

            FaceFunctionDelayTimer.Interval = 3000;
            FaceFunctionDelayTimer.Elapsed += FaceFunctionDelayTimer_Elapsed;
            FaceFunctionDelayTimer.AutoReset = false;
            FaceFunctionDelayTimer.Start();
        }

        private static void FaceFunctionDelayTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            GlobalData.personinfront_tour = false;
        }
    }
}
