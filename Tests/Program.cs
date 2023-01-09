using System.Drawing;
using System.Net;
using FFmpeg.AutoGen.Abstractions;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;

namespace StreamerWinui
{
    class Program
    {
        public static unsafe void Main()
        {
            string ip = "192.168.0.115";
            
            StreamSession streamSession = new();
            streamSession.VideoRecording = false;
            streamSession.AudioRecording = true;
            streamSession.StartStream();
            streamSession.AddClient(IPAddress.Parse(ip));
            //streamSession.AddClientAsFile(@"D:\video\img\2.opus");
            Thread.Sleep(int.MaxValue);
            streamSession.StopStream();
            //StreamSession.errStrPrint(-1313558101);
            //Thread.Sleep(15 * 1000);
            //s.stopStream();

            //AudioRecorder.Start3();
        }
    }
}


//StreamerWinui.StreamSession streamSession = new StreamerWinui.StreamSession();
//StreamerWinui.StreamSession.errStrPrint(0);
//streamSession.startStream("mpegts");


//    Thread.Sleep(400 * 1000); streamSession.stopStream();

//-11 Resource temporarily unavailable
//-22 Invalid argument
//-40 Function not implemented
//-1313558101 Unknown error occurred