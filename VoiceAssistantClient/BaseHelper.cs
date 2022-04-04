using System;
using System.Collections.Generic;
using Robot;
using ROS = Robot.Data.ROS;
using System.Windows.Forms;
using VoiceAssistantClient;

namespace InHouseRobot_Body
{
    public static class BaseHelper
    {
        private static Base rBase = new Base();
        private const string IP_ADDRESS = "192.168.31.200:9090";
        private static List<String> SavedLocations = rBase.GetLocations();

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
                }
            }
            catch
            {
            }
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
                GlobalData.isNavigating = false;
                GlobalData.isReached = true;
            }
            else if (e.Status == "")
            {
                //cancelled
                //MessageBox.Show("Navigation cancelled");
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
                    rBase.Move(ROS.BaseDirection.FORWARD);
                    break;
                case "backward":
                    rBase.Move(ROS.BaseDirection.BACKWARD);
                    break;
                case "anticlockwise":
                    rBase.Move(ROS.BaseDirection.ANTICLOCKWISE);
                    break;
                case "clockwise":
                    rBase.Move(ROS.BaseDirection.CLOCKWISE);
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
    }
}
