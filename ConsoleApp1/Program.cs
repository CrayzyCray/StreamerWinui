using NAudio.CoreAudioApi;
using NAudio.Wave;
using StreamerLib;
using System.Runtime.CompilerServices;

internal class ConsoleApp1
{
    public static void Main()
    {
        RecordingTest();
        Console.WriteLine("Any key to exit"); 
        Console.ReadKey();
    }

    static void RecordingTest()
    {
        var sc = new StreamController();
        sc.AudioCapturing = true;
        var masterChannel = sc.MasterChannel;
        var mmDevice1 = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        var mmDevice2 = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Communications);
        masterChannel.AddChannel(mmDevice1);
        masterChannel.AddChannel(mmDevice2);
        sc.AddClientAsFile(@"C:\Users\Cray\Desktop\St\1.opus");

        Console.WriteLine("Press any key to start, then press any key to stop");

        Console.ReadKey();
        sc.StartStream();

        Console.ReadKey();
        sc.StopStream();
    }

    static void StartGC(int periodInSeconds)
    {
        void While()
        {
            while (true)
            {
                GC.Collect();
                Thread.Sleep(periodInSeconds * 1000);
            }
        }
        new Task(While).Start();
    }
}