using System.Drawing;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;

namespace StreamerWinui
{
    class Program
    {
        public static unsafe void Main()
        {
            if (DynamicallyLoadedBindings.LibrariesPath == string.Empty)
            {
                DynamicallyLoadedBindings.LibrariesPath = @"C:\Users\Cray\Desktop\Programs\ffmpeg";
                DynamicallyLoadedBindings.Initialize();
            }

            bool recordVideo = false;
            bool recordAudio = true;
            
            new StreamSession().StartStream(ipToStream:"localhost", framerate:15, cropResolution:new Size(800, 600), codecName:"hevc_nvenc", recordVideo:recordVideo, recordAudio:recordAudio);
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