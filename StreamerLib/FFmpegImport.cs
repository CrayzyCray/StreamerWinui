using System.Runtime.InteropServices;

namespace StreamerLib
{
    static unsafe public partial class FFmpegImport
    {
        const string DllPath = "DLLs/Dll.dll";
        [LibraryImport(DllPath)]
        public static partial IntPtr AVCodecFindEncoderByName([MarshalAs(UnmanagedType.LPStr)] String encoderName);

        [LibraryImport(DllPath)]
        public static partial IntPtr GetNameOfEncoder();
    }
}
