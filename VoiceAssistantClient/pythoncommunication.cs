using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace VoiceAssistantClient
{
    internal static class pythoncommunication
    {
        public static void startcommunication()
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = "test.bat";    //填写exe的具体路径
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.CreateNoWindow = true;
                //p.StartInfo.Arguments = "abc 123";    //参数
                p.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
                p.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);
                p.Start();
                p.BeginOutputReadLine();
                p.WaitForExit();
                p.Close();
            }
            catch (Exception ex)
            { Console.WriteLine(ex.ToString()); }
        }

        static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            VisionDatabaseRecorder.AddData(outLine.ToString(), DateTime.Now.ToString("d"), DateTime.Now.ToString("t"), "Unknown", @"C:\Users\lyuqi\Desktop\4fc48a5b6a10536011a9c49cdf1e386a28037fa7.jpg");
        }
    }
}
