using NAudio.CoreAudioApi;
using StreamerLib;
using System.Threading.Channels;

unsafe internal partial class ConsoleApp1
{
    public static void Main()
    {
        //AudioEncoder audioEncoder = new(new StreamerLib.StreamWriter(), Encoders.LibOpus);
        //audioEncoder.Test();
        if (File.Exists("Dlls/Dll.dll"))
        {
            FFmpegImport.Here();

        }

        MMDevice device = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        StreamController streamController = new StreamController();

        streamController.MMDevice = device;
        streamController.AudioCapturing = true;
        streamController.StartStream();
        streamController.AddClientAsFile(@"C:\Users\Cray\Desktop\St\1.opus");
        Thread.Sleep(20000);
        streamController.StopStream();
    }
}