using StreamerLib;

internal class ConsoleApp1
{
    public static void Main()
    {
        var sc = new StreamController();

        sc.AudioCapturing = true;
        sc.StartStream();
        sc.AddClientAsFile(@"C:\Users\Cray\Desktop\St\1.opus");
        //Thread.Sleep(1000);
        Console.ReadKey();
        sc.StopStream();
    }
}