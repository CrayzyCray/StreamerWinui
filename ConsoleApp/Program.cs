using StreamerLib;
using System.Runtime.InteropServices;

partial class Program
{
    void Main()
    {
        var ptr = LibUtil.start_record_test();
        Thread.Sleep(6000);
        LibUtil.stop_record_test(ptr);
    }
}