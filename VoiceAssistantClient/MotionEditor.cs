using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InHouseRobot_Body;

namespace VoiceAssistantClient
{
    public class MotionEditor
    {
        public static void BodyMotion(Queue<string> motionname, Queue<int> delaytime)
        {
            if(motionname.Count > 0 && delaytime.Count > 0 && motionname.Count == delaytime.Count)
            {
                for(int n = 0; n < motionname.Count; n++)
                {
                    //UpperBodyHelper.Move(motionname.Dequeue());
                    Task.Delay(delaytime.Dequeue()).Wait();
                }
            }
            
        }
    }
}
