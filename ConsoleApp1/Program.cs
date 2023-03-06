using System.Runtime.InteropServices;
using StreamerLib;

unsafe internal partial class ConsoleApp1
{
    public static void Main()
    {
        FFmpegImport.AVCodecFindEncoderByName("libopus");
        FFmpegImport.GetNameOfEncoder();
    }
}