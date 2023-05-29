using System.Runtime.InteropServices;

namespace StreamerLib;

unsafe public sealed partial class LibUtil
{
    const string LibPath = "libutil.dll";

    [LibraryImport(LibPath)]
    internal static partial float get_peak(void* array, int length);

    public static float get_peak_safe(byte[] array, int length)
    {
        fixed (byte* ptr = array)
            return get_peak(ptr, length / 4);
    }
}
