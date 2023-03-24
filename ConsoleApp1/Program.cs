using NAudio.CoreAudioApi;
using StreamerLib;

internal class ConsoleApp1
{
    public static void Main()
    {
        var sc = new StreamController();
        sc.AudioCapturing = true;
        //var masterChannel = sc.MasterChannel;
        //var mmDevice1 = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        //var mmDevice2 = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Communications);
        //masterChannel.AddChannel(mmDevice1);
        //masterChannel.AddChannel(mmDevice2);
        sc.StartStream();
        sc.AddClientAsFile(@"C:\Users\Cray\Desktop\St\1.opus");
        Thread.Sleep(3000);
        //Console.WriteLine("Press any key to stop");
        //Console.ReadKey();
        sc.StopStream();
    }

    void Test()
    {
        EventWaitHandle eventWaitHandle = new(false, EventResetMode.AutoReset);
        int a = 0;
        var rnd = new Random();
        
        var EncoderTask = new Task(Encode);
        EncoderTask.Start();
        
        var RecorderTask = new Task(RecieveData);
        RecorderTask.Start();

        Console.ReadKey();
        
        void Encode()
        {
            while (true)
            {
                eventWaitHandle.WaitOne();
                Console.WriteLine($"Frame {a} encoded");
            }
        }

        void RecieveData()
        {
            while (true)
            {
                Thread.Sleep(rnd.Next(50, 500));
                a++;
                eventWaitHandle.Set();
            }
        }
    }
}