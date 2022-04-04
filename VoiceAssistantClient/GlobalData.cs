using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Synthesis;
using InHouseRobot_Body;
using System.Threading;
using System.Drawing;
using System.IO;
using System.Diagnostics;
//using ScsServoLib;

namespace VoiceAssistantClient
{
    public class GlobalData
    {
        public static System.Speech.Synthesis.SpeechSynthesizer mySpeechSyn = new System.Speech.Synthesis.SpeechSynthesizer();

        private static bool _isReached;

        public static System.Timers.Timer BackTimer = new System.Timers.Timer();
        public static System.Timers.Timer StatusCheckTimer = new System.Timers.Timer();
        public static System.Timers.Timer ResetTimer = new System.Timers.Timer();
        public static System.Timers.Timer TourDelayTimer = new System.Timers.Timer();

        public static string startlocation = "start";
        public static string goallocation;

        public static bool DisableFlag;
        public static bool EnableFlag;

        public static bool NaviIsCanceled;
        public static bool Navigating;
        public static bool RestartNavi;

        public static bool Departing;
        public static bool Arriving;

        public static Point facemidpoint;
        public static Point facesize;

        public static MemoryStream img;

        public static string TagName;
        public static double probability;

        public static string Questions;

        public static string userDesignation;

        public static bool RobotisReturning = false;

        public static bool waitingatthegoalposition = false;

        public static bool Navitothegoalposition = false;

        public static bool personinfront_standby = false;

        public static bool personinfront_tour = false;

        public static string BroadcastingImage;

        public static int facescount;

        public static bool counLock = false;

        public static bool DutyOff = false;

        public static ShutdownHelper shutdown = new ShutdownHelper();

        public static UpperBodyHelper _motor;

        public static bool isReached
        {
            get { return _isReached; }

            set
            {
                _isReached = value;

                if (isReached)
                {
                    isReached = false;

                    if (TourMode == false)
                    {
                        if (goallocation != startlocation) //去程
                        {
                            RobotisReturning = true;

                            MainWindow.SynthesizeAudioAsync("Your Destination is Reached. Have a nice day!");
                            //mySpeechSyn.Speak("Your Destination is Reached. Have a nice day");
                            Thread.Sleep(200);

                            waitingatthegoalposition = true;
                            BackTimer.Interval = 10000;
                            BackTimer.Elapsed += BackTimer_Elapsed;
                            BackTimer.AutoReset = false;
                            BackTimer.Start();
                        }

                        else if (goallocation == startlocation)
                        {
                            RobotisReturning = false;
                        }
                    }
                    else // Tourmode
                    {
                        if (TourHelper.BacktoStandyLocation == false)
                        {
                            if (TourHelper.TourisInterruptedbyNavi == false)
                            {
                                if (LocationCount < TourHelper.LocationName.Count - 1)
                                {
                                    LocationCount = LocationCount + 1;
                                }
                                else
                                {
                                    LocationCount = 0;
                                }
                            }
                            else if (TourHelper.TourisInterruptedbyNavi == true)
                            {
                                //TourHelper.ResumeTimer.Stop();
                                TourHelper.ResumeTimer.Interval = 8000;
                                TourHelper.ResumeTimer.Elapsed += TourHelper.ResumeTimer_Elapsed;
                                TourHelper.ResumeTimer.AutoReset = false;
                                TourHelper.ResumeTimer.Start();
                            }
                        }

                        else if (TourHelper.BacktoStandyLocation == true)
                        {
                            if (TourHelper.TourisInterruptedbyNavi == false)
                            {
                                TourMode = false;
                                TourHelper.BacktoStandyLocation = false;
                            }

                            else
                            {
                                TourHelper.ReturnTimer.Interval = 8000;
                                TourHelper.ReturnTimer.Elapsed += TourHelper.ReturnTimer_Elapsed;
                                TourHelper.ReturnTimer.AutoReset = false;
                                TourHelper.ReturnTimer.Start();
                            }
                        }
                    }

                    if(DutyOff)
                    {
                        //ShutdownHelper shutdown = new ShutdownHelper();
                        shutdown.Shutdown();
                    }
                }
            }
        }

        public static bool isNavigating;

        private static bool _iscancled;

        public static bool iscancled
        {
            get { return _iscancled; }

            set
            {
                _iscancled = value;

                if (iscancled)
                {
                    iscancled = false;

                    BackTimer.Interval = 10000;
                    BackTimer.Elapsed += BackTimer_Elapsed;
                    BackTimer.AutoReset = false;
                    BackTimer.Start();
                }
            }
        }

        private static void ResetTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            BackTimer.Stop();
            goallocation = startlocation;
            BaseHelper.Go(goallocation);
        }

        public static void BackTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            waitingatthegoalposition = false;

            BackTimer.Stop();

            goallocation = startlocation;
            isNavigating = true;
            RobotisReturning = true;
            BaseHelper.Go(goallocation);

        }

        public static bool TourMode = false;

        private static int _LocationCount;

        public static int LocationCount
        {
            get { return _LocationCount; }

            set
            {
                _LocationCount = value;

                Debug.Write(LocationCount.ToString());

                if (LocationCount > 0)
                {
                    if (!TourHelper.LocationSpeech[LocationCount - 1].Contains("0"))
                    {
                        MainWindow.SynthesizeAudioAsync(TourHelper.LocationSpeech[LocationCount -1]);
                    }

                    if (TourHelper.LocationDelay[LocationCount - 1] != 0)
                    {
                        TourDelayTimer.Interval = TourHelper.LocationDelay[LocationCount - 1];
                        TourDelayTimer.Elapsed += TourDelayTimer_Elapsed;
                        TourDelayTimer.AutoReset = false;
                        TourDelayTimer.Start();
                    }
                    else
                    {
                        TourHelper.GoNextPoint(LocationCount);
                    }
                }
                else
                {

                    if (!TourHelper.LocationSpeech[0].Contains("0"))
                    {
                        MainWindow.SynthesizeAudioAsync(TourHelper.LocationSpeech[LocationCount]);
                    }

                    if (TourHelper.LocationDelay[0] != 0)
                    {
                        TourDelayTimer.Interval = TourHelper.LocationDelay[LocationCount];
                        TourDelayTimer.Elapsed += TourDelayTimer_Elapsed;
                        TourDelayTimer.AutoReset = false;
                        TourDelayTimer.Start();
                    }
                    else
                    {
                        TourHelper.GoFirstPoint();
                    }
                }
            }
        }

        private static void TourDelayTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            TourDelayTimer.Stop();
            TourHelper.GoNextPoint(LocationCount);
        }
    }
}
