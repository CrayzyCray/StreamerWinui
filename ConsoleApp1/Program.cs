using NAudio.CoreAudioApi;
using NAudio.Wave;
using StreamerLib;
using System.Diagnostics;
using System.Text.RegularExpressions;

internal class ConsoleApp1
{
    public static void Main()
    {
        //Test6();
        RecordingTest();
        Console.ReadKey();
    }

    static void Test6()
    {
        AudioEncoder encoder = new(new StreamerLib.StreamWriter(), Encoders.LibOpus);

        var device = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        WasapiAudioCapturingChannel capture = new(device, encoder.FrameSizeInBytes);
        int counter = 0;
        ManualResetEvent autoResetEvent = new(false);

        capture.DataAvailable += (s, e) =>
        {
            Console.WriteLine($"set {capture.Queue.Count}");
            autoResetEvent.Set();
        };

        Stopwatch stopwatch = Stopwatch.StartNew();

        new Task(() =>
        {
            while (true)
            {
                autoResetEvent.WaitOne();

                counter++;
                Console.WriteLine(counter);
                while (capture.BufferIsAvailable)
                {
                    //Console.WriteLine("perf1 " + stopwatch.ElapsedMilliseconds);
                    encoder.EncodeAndWriteFrame(capture.ReadNextBuffer());
                    //Console.WriteLine("perf2 " + stopwatch.ElapsedMilliseconds);
                    //Console.WriteLine("packet");
                }
                autoResetEvent.Reset();
            }
        }).Start();


        capture.StartRecording();

        //StartGC(8);

        Console.ReadKey();
        capture.StopRecording();
    }

    static void RecordingTest()
    {
        var sc = new StreamController();
        sc.AudioCapturing = true;
        var masterChannel = sc.MasterChannel;
        var mmDevice1 = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        masterChannel.AddChannel(mmDevice1);
        sc.StartStream();
        //sc.AddClientAsFile(@"C:\Users\Cray\Desktop\St\1.opus");
        //Thread.Sleep(3000);
        //Console.WriteLine("Press any key to stop");
        Console.ReadKey();
        sc.StopStream();
    }

    static void Test5()
    {
        AudioEncoder encoder = new(new StreamerLib.StreamWriter(), Encoders.LibOpus);

        var device = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        WasapiAudioCapturingChannel capture = new(device, encoder.FrameSizeInBytes);
        int counter = 0;
        capture.DataAvailable += (s, e) =>
        {
            counter++;
            Console.Write($"Counter:{counter}");
            var sender = s as WasapiAudioCapturingChannel;
            while (sender.BufferIsAvailable)
            {
                //break;
                encoder.EncodeAndWriteFrame(sender.ReadNextBuffer());
            }
            Console.WriteLine($" writed");
        };
        capture.StartRecording();

        Console.ReadKey();
        capture.StopRecording();
    }

    static void StartGC(int periodInSeconds)
    {
        new Task(() => { while (true) { GC.Collect(); Thread.Sleep(periodInSeconds * 1000); } })
            .Start();
    }
}