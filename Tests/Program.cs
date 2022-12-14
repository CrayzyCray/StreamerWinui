using Tests;

namespace StreamerWinui
{
    class Program
    {
        public static void Main()
        {
            AudioCapturer a = new AudioCapturer();
            StreamSession s = new StreamSession();
            //StreamSession.errStrPrint(-1313558101);
            s.startStream("mpegts");
            Thread.Sleep(10 * 1000);
            //s.stopStream();
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