using StreamerLib;
using System.Threading.Channels;

unsafe internal partial class ConsoleApp1
{
    public static void Main()
    {
        //AudioEncoder audioEncoder = new(new StreamerLib.StreamWriter(), Encoders.LibOpus);
        //audioEncoder.Test();

        TestStruct s = new TestStruct();
        s.A = 145;
        s.B = 209;

        FFmpegImport.TestStructs(s);
    }
}