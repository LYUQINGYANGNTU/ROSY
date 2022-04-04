using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;

namespace VoiceAssistantClient
{
    public class ShutdownHelper
    {
        /// <summary>
        /// 到期时间
        /// </summary>
        public DateTime End { get; set; }

        public ShutdownHelper()
        {
            DateTime now = DateTime.Now;
            DateTime end = now.Date.AddDays(1).AddSeconds(-1);

            this.End = end;
        }
        public ShutdownHelper(DateTime time)
        {
            this.End = time;
        }


        //执行命令
        public void Exec(string str)
        {
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "cmd.exe";//调用cmd.exe程序
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardInput = true;//重定向标准输入
                    process.StartInfo.RedirectStandardOutput = true;//重定向标准输出
                    process.StartInfo.RedirectStandardError = true;//重定向标准出错
                    process.StartInfo.CreateNoWindow = true;//不显示黑窗口
                    process.Start();//开始调用执行
                    process.StandardInput.WriteLine(str + "&exit");//标准输入str + "&exit"，相等于在cmd黑窗口输入str + "&exit"
                    process.StandardInput.AutoFlush = true;//刷新缓冲流，执行缓冲区的命令，相当于输入命令之后回车执行
                    process.WaitForExit();//等待退出
                    process.Close();//关闭进程
                }
            }
            catch
            {
            }
        }
        //执行关机操作
        public void Shutdown()
        {
            this.Exec("shutdown -s -f -t 120");
        }

        public void CancleShutDown()
        {
            this.Exec("shutdown -a");
        }

        //执行重启操作
        public void Restart()
        {
            this.Exec("shutdown -r -f -t 0");
        }

        //取消任务
    }
}
