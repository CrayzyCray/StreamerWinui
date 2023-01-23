using System.Net;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;

namespace StreamerWinui
{
    class Program
    {
        public static void Main()
        {
            Start();
        }

        public static void Start()
        {
            string ip = "192.168.0.115";
            
            StreamSession streamSession = new();
            streamSession.VideoRecording = false;
            streamSession.AudioRecording = true;
            streamSession.StartStream();
            //streamSession.AddClient(IPAddress.Parse(ip));
            streamSession.AddClientAsFile(@"D:\video\img\2.opus");
            Thread.Sleep(50000);
            streamSession.StopStream();
            //StreamSession.errStrPrint(-1313558101);
        }

        public void Test()
        {
            byte[] buf1 = new byte[11520];
            byte[] buf2 = new byte[7680];
            AudioBufferSlicer abs = new(960, 4, 2);

            Stopwatch sw = new();
            sw.Start();
            abs.SendBuffer(buf1, buf1.Length);
            for (int i = 0; i < 10000; i++)
                abs.SendBuffer(buf2, buf2.Length);
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
        }
    }
}

//-11 Resource temporarily unavailable
//-22 Invalid argument
//-40 Function not implemented
//-1313558101 Unknown error occurred