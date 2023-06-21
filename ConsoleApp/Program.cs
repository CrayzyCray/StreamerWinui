using StreamerLib;
using System.Runtime.InteropServices;

partial class Program
{
    static void Main()
    {
        //var ptr = LibUtil.start_record_test();
        //Thread.Sleep(6000);
        //LibUtil.stop_record_test(ptr);
        LibUtil.start_chn_test();
        Console.ReadLine();
    }
}