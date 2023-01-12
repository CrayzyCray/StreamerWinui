using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;

namespace StreamerWinui
{
    class Program
    {
        public static void Main()
        {
            Stream();

            //Marshal.AllocHGlobal(IntPtr.Zero);
            //Marshal.FreeHGlobal(IntPtr.Zero);
        }

        public static void Stream()
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
    

    public class TheEasiestBenchmark
    {
        public byte[] array1 = new byte[7680];
        public byte[] array2 = new byte[7680 * 2];
        
        [Benchmark(Description = "Array.Copy(){}")]
        public void met1()
        {
            Array.Copy(array2, array1, 7680);
        }
        
        [Benchmark(Description = "For()")]
        public void met2()
        {
            for (int i = 0; i < array1.Length; i++)
            {
                array1[i] = array2[i];
            }
        }
    }
}
//    Thread.Sleep(400 * 1000); streamSession.stopStream();

//-11 Resource temporarily unavailable
//-22 Invalid argument
//-40 Function not implemented
//-1313558101 Unknown error occurred