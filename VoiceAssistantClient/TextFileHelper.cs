using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;


namespace VoiceAssistantClient
{
    public static class TextFileHelper
    {
        static byte[] byData = new byte[100];
        static char[] charData = new char[1000];
        public static void Read()
        {
            try
            {
                FileStream file = new FileStream("", FileMode.Open);
                file.Seek(0, SeekOrigin.Begin);
                file.Read(byData, 0, 100);
                Decoder d = Encoding.Default.GetDecoder();
                d.GetChars(byData, 0, byData.Length, charData, 0);
                Console.WriteLine(charData);
                file.Close();
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
