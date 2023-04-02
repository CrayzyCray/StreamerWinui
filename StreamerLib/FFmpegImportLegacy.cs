using System.Runtime.InteropServices;
using static StreamerLib.StreamWriter;

namespace StreamerLib;
public static unsafe class FFmpegImportLegacy
{
    private const string DllPath = "DLLs/Dll.dll";

    [DllImport(DllPath)]
    public static extern void Here();
    
    [DllImport(DllPath)]
    public static extern int AudioEncoder_Constructor(
        [MarshalAs(UnmanagedType.LPStr)] String encoderName,
        int _sampleRate,
        int channels,
        int* FrameSizeInSamples,
        nint* _codecContextOut,
        nint* _packetOut,
        nint* _timebaseOut,
        nint* _codecParametersOut,
        nint* _avFrameOut);

    [DllImport(DllPath)]
    public static extern int AudioEncoder_Dispose(
        nint packet,
        nint frame,
        nint codecContext,
        nint codecPatameters,
        nint timebase);

    [DllImport(DllPath)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AudioEncoder_EncodeAndWriteFrame(
        byte* buffer,
        int frameSizeInBytes,
        int channels,
        int streamIndex,
        long pts,
        nint codecContext,
        nint packet,
        nint frame);

    [DllImport(DllPath)]
    public static extern int StreamWriter_AddClient(
        [MarshalAs(UnmanagedType.LPStr)] String outputUrl,
        nint* formatContextOut,
        StreamParameters* streamParameters,
        int streamParametersSize);

    [DllImport(DllPath)]
    public static extern int StreamWriter_AddClientAsFile(
        [MarshalAs(UnmanagedType.LPStr)] String outputUrl,
        nint* formatContextOut,
        StreamParameters* streamParameters,
        int streamParametersSize);

    [DllImport(DllPath)]
    public static extern int StreamWriter_WriteFrame(
        nint packet,
        nint packetTimeBase,
        nint streamTimeBase,
        nint[] formatContexts,
        int formatContextsCount);

    [DllImport(DllPath)]
    public static extern int StreamWriter_CloseFormatContext(nint formatContext);
}