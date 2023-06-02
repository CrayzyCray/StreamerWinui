using System.Runtime.InteropServices;
using static StreamerLib.StreamWriter;

namespace StreamerLib;
public static unsafe partial class FFmpegImport
{
    private const string DllPath = "DLLs/Dll.dll";

    [LibraryImport(DllPath)]
    internal static partial int AudioEncoder_Dispose(
        nint packet,
        nint frame,
        nint codecContext,
        nint codecPatameters,
        nint timebase);

    [LibraryImport(DllPath)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AudioEncoder_EncodeAndWriteFrame(
        byte* buffer,
        int frameSizeInBytes,
        int channels,
        int streamIndex,
        long pts,
        nint codecContext,
        nint packet,
        nint frame);

    [LibraryImport(DllPath)]
    internal static partial int StreamWriter_AddClient(
        [MarshalAs(UnmanagedType.LPStr)] String outputUrl,
        nint* formatContextOut,
        StreamParameters* streamParameters,
        int streamParametersSize);

    [LibraryImport(DllPath)]
    internal static partial int StreamWriter_AddClientAsFile(
        [MarshalAs(UnmanagedType.LPStr)] String outputUrl,
        nint* formatContextOut,
        StreamParameters* streamParameters,
        int streamParametersSize);

    [LibraryImport(DllPath)]
    internal static partial int StreamWriter_WriteFrame(
        nint packet,
        nint packetTimeBase,
        nint streamTimeBase,
        nint[] formatContexts,
        int formatContextsCount);

    [LibraryImport(DllPath)]
    internal static partial int StreamWriter_CloseFormatContext(nint formatContext);
}