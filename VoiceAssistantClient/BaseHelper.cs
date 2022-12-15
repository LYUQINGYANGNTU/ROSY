using System;
using System.Collections.Generic;
using Robot;
using ROS = Robot.Data.ROS;
using System.Windows.Forms;
using VoiceAssistantClient;
using System.Diagnostics;

namespace InHouseRobot_Body
{
    public static class BaseHelper
    {
        static System.Timers.Timer LockTimer = new System.Timers.Timer();

        private static Base rBase = new Base();
        private const string IP_ADDRESS = "192.168.31.200:9090";
        private static List<String> SavedLocations = rBase.GetLocations();

        static bool move_base_status_locked = false;

        static BaseHelper()
        {
        }
        public static void Connect(string ip = IP_ADDRESS)
        {
            try
            {
                if (!ROS.Connected)
                {
                    rBase.Connect(ip);
                    rBase.Initialise();
                    rBase.LinearSpeed = 1.0;
                    rBase.AngularSpeed = 1.0;
                    rBase.NavigationStatusChanged += RBase_NavigationStatusChanged;
                    rBase.MapStatusChanged += RBase_MapStatusChanged;
                }
            }
            catch
            {
            }
        }

        private static void LockTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            move_base_status_locked = false;
        }

        public static void Disconnect()
        {
            if (ROS.Connected)
            {
                rBase.CancelNavigation();
                rBase.Disconnect();
            }
        }
        private static void RBase_NavigationStatusChanged(object sender, NavigationStatusEventArgs e)
        {
            
            if (e.Status == "Goal reached.")
            {
                if (move_base_status_locked == false)
                {
                   // move_base_status_locked = true;

                    if (TourHelper.LocationName[GlobalData.LocationCount] == "Waypoint65")
                    {
                        TourHelper.currentmap = "A";
                        TourHelper.SwitchArea();
                    }
                    else if (TourHelper.LocationName[GlobalData.LocationCount] == "Waypoint106")
                    {
                        TourHelper.currentmap = "B";
                        TourHelper.SwitchArea();
                    }
                    else if (TourHelper.LocationName[GlobalData.LocationCount] == "Waypoint150")
                    {
                        TourHelper.currentmap = "C";
                        TourHelper.SwitchArea();
                    }
                    else
                    {
                        GlobalData.isNavigating = false;
                        GlobalData.isReached = true;
                    }
                    
                    LockTimer.Interval = 1000;
                    LockTimer.Elapsed += LockTimer_Elapsed;
                    LockTimer.AutoReset = false;
                    LockTimer.Start();
                }
            }
            else if(e.Status == "")
            {
                if (GlobalData.TourMode)
                {
                    GlobalData.LocationCount++;
                }
            }
            else if (e.Status == "")
            {
                //cancelled
                //MessageBox.Show("Navigation cancelled");
            }
        }

        private static void RBase_MapStatusChanged(object sender, MapStatusEventArgs e)
        {
            if (e.Status.ToLower() == "map changed")
            {
                
                if (GlobalData.TourMode)
                {
                    GlobalData.LocationCount ++;
                }
            }
        }
        public static void ChangeSpeed(double linearSpeed)
        {
            rBase.LinearSpeed = linearSpeed;
        }      
        public static void Go(string locationName)
        {
            if (SavedLocations.Contains(locationName)) rBase.Go(locationName);
        }
        public static void CancelNavigation()
        {
            rBase.CancelNavigation();
        }
        public static void Move(string direction)
        {
            switch(direction.ToLower())
            {
                case "forward":
                    rBase.Move(0.5,0);
                    break;
                case "backward":
                    rBase.Move(-0.5,0);
                    break;
                case "anticlockwise":
                    rBase.Move(0,0.5);
                    break;
                case "clockwise":
                    rBase.Move(0,-0.5);
                    break;
                default:
                    MessageBox.Show("Invalid direction specified: " + direction);
                    break;

            }
        }

        public static void Move(double LinSpeed, double AngSpeed)
        {

            rBase.Move(LinSpeed, AngSpeed);
        }

        public static void Stop()
        {
            rBase.Stop();
        }
        public static List<string> GetSavedLocations()
        {
            return rBase.GetLocations();
        }
        public static void SaveLocation(string locationName)
        {
            rBase.SaveLocation(locationName);
        }
        public static void DeleteLocation(string locationName)
        {
            rBase.DeleteLocation(locationName);
        }
        public static void DeleteAllLocations()
        {
            rBase.DeleteAllLocations();
        }

        public static void ChangeMap(string mapinfo)
        {
            rBase.Mapchange(mapinfo);
        }
    }
}
