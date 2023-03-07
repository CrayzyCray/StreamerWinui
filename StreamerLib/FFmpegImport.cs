using FFmpeg.AutoGen.Abstractions;
using System.Runtime.InteropServices;
using static StreamerLib.StreamWriter;

namespace StreamerLib
{
    static unsafe public partial class FFmpegImport
    {
        const string DllPath = "DLLs/Dll.dll";

        [LibraryImport(DllPath)]
        public static partial IntPtr AVCodecFindEncoderByName([MarshalAs(UnmanagedType.LPStr)] String encoderName);

        [LibraryImport(DllPath)]
        public static partial IntPtr GetNameOfEncoder([MarshalAs(UnmanagedType.LPStr)] String encoderName);

        [LibraryImport(DllPath)]
        public static partial IntPtr AvCodecAllocContext3(IntPtr avCodec);

        [LibraryImport(DllPath)]
        public static partial int AudioEncoder_Constructor(
            [MarshalAs(UnmanagedType.LPStr)] String encoderName,
            int _sampleRate,
            int channels,
            int* FrameSizeInSamples,
            nint* _codecContextOut,
            nint* _packetOut,
            nint* _timebaseOut,
            nint* _codecParametersOut,
            nint* _avFrameOut);

        //[DllImport(DllPath, CallingConvention = CallingConvention.)]

        [LibraryImport(DllPath)]
        public static partial int AudioEncoder_Dispose(
            nint packet,
            nint frame,
            nint codecContext,
            nint codecPatameters,
            nint timebase);

        [LibraryImport(DllPath)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool AudioEncoder_EncodeAndWriteFrame(
            byte* buffer,
            int frameSizeInBytes,
            int channels,
            int streamIndex,
            long pts,
            nint codecContext,
            nint packet,
            nint frame);

        [LibraryImport(DllPath)]
        public static partial int StreamWriter_AddClient(
            [MarshalAs(UnmanagedType.LPStr)] String outputUrl,
            nint* formatContextOut,
            StreamParameters* streamParameters,
            int streamParametersSize);

        [LibraryImport(DllPath)]
        public static partial int StreamWriter_AddClientAsFile(
            [MarshalAs(UnmanagedType.LPStr)] String outputUrl,
            nint* formatContextOut,
            StreamParameters* streamParameters,
            int streamParametersSize);

        [LibraryImport(DllPath)]
        public static partial int StreamWriter_WriteFrame(
            nint packet,
            nint packetTimebase,
            nint streamTimebase,
            nint[] formatContexts);

        [LibraryImport(DllPath)]
        public static partial int StreamWriter_DeleteAllClients(nint formatContext);

        [LibraryImport(DllPath)]
        public static partial int Test(nint* codecContext);

        [LibraryImport(DllPath)]
        public static partial int TestStructs(TestStruct s);

        [LibraryImport(DllPath)]
        public static partial int PrintCodecLongName(nint codec);
    }

    public struct TestStruct
    {
        public int A;
        public int B;
    }

    public enum AVSampleFormat
    {
        AV_SAMPLE_FMT_NONE = -1,
        //
        // Summary:
        //     unsigned 8 bits
        AV_SAMPLE_FMT_U8,
        //
        // Summary:
        //     signed 16 bits
        AV_SAMPLE_FMT_S16,
        //
        // Summary:
        //     signed 32 bits
        AV_SAMPLE_FMT_S32,
        //
        // Summary:
        //     float
        AV_SAMPLE_FMT_FLT,
        //
        // Summary:
        //     double
        AV_SAMPLE_FMT_DBL,
        //
        // Summary:
        //     unsigned 8 bits, planar
        AV_SAMPLE_FMT_U8P,
        //
        // Summary:
        //     signed 16 bits, planar
        AV_SAMPLE_FMT_S16P,
        //
        // Summary:
        //     signed 32 bits, planar
        AV_SAMPLE_FMT_S32P,
        //
        // Summary:
        //     float, planar
        AV_SAMPLE_FMT_FLTP,
        //
        // Summary:
        //     double, planar
        AV_SAMPLE_FMT_DBLP,
        //
        // Summary:
        //     signed 64 bits
        AV_SAMPLE_FMT_S64,
        //
        // Summary:
        //     signed 64 bits, planar
        AV_SAMPLE_FMT_S64P,
        //
        // Summary:
        //     Number of sample formats. DO NOT USE if linking dynamically
        AV_SAMPLE_FMT_NB
    }
}
