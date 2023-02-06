using System.Net;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StreamerLib
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
            
            StreamController streamController = new();
            streamController.VideoRecording = false;
            streamController.AudioRecording = true;
            streamController.StartStream();
            //streamSession.AddClient(IPAddress.Parse(ip));
            streamController.AddClientAsFile(@"D:\video\img\2.opus");
            Thread.Sleep(50000);
            streamController.StopStream();
            //StreamSession.errStrPrint(-1313558101);
        }
    }
}

//-11 Resource temporarily unavailable
//-22 Invalid argument
//-40 Function not implemented
//-1313558101 Unknown error occurred