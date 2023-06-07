using System.Runtime.InteropServices;
using static StreamerLib.StreamWriter;

namespace StreamerLib;

unsafe public sealed partial class LibUtil
{
    const string LibPath = "DLLs/libutil.dll";

    [LibraryImport(LibPath)]
    public static partial nint start_record_test();
    [LibraryImport(LibPath)]
    public static partial void stop_record_test(nint ptr);

    [LibraryImport(LibPath)]
    internal static partial float get_peak(void* array, int length);
    [LibraryImport(LibPath)]
    internal static partial float get_peak_multichannel(void* array, int length, int channels, int channel_index);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="array">pointer to float array</param>
    /// <param name="length"> length of float array</param>
    /// <param name="volume">0..1</param>
    [LibraryImport(LibPath)]
    public static partial void apply_volume(void* array, int length, float volume);
    [LibraryImport(LibPath)]
    internal static partial EncoderParameters audio_encoder_constructor([MarshalAs(UnmanagedType.LPStr)] String name, int channels, int requiredSampleRate);
    /// <returns>< 0 if error</returns>
    [LibraryImport(LibPath)]
    internal static partial int audio_encoder_encode_buffer(EncoderParameters parameters, byte* buffer, Int64 pts, int streamIndex);
    [LibraryImport(LibPath)]
    internal static partial void stream_writer_close_format_context(nint formatContext);
    [LibraryImport(LibPath)]
    internal static partial void audio_encoder_dispose(EncoderParameters* parameters);
    [LibraryImport(LibPath)]
    internal static partial nint stream_writer_add_client([MarshalAs(UnmanagedType.LPStr)] String outputUrl, StreamParameters* array, int length);
    [LibraryImport(LibPath)]
    internal static partial nint stream_writer_add_client_as_file([MarshalAs(UnmanagedType.LPStr)] String path, StreamParameters* array, int length);
    [LibraryImport(LibPath)]
    internal static partial int stream_writer_write_packet(nint packet, nint timeBasePacket, StreamClient* clientsArray, int arrayLength);

    public static float GetPeak(byte[] array, int length)
    {
        if (array == null)
            throw new ArgumentNullException("array");
        if (length <= 0 || length > array.Length)
            throw new ArgumentOutOfRangeException("length");
        fixed (byte* ptr = array)
            return get_peak(ptr, length / 4);
    }

    public static float GetPeakMultichannel(byte[] array, int length, int channels, int channelIndex)
    {
        if(array == null)
            throw new ArgumentNullException("array");
        if(length <= 0 || length > array.Length)
            throw new ArgumentOutOfRangeException("length");
        if (channelIndex > channels)
            throw new ArgumentOutOfRangeException("channelIndex");
        fixed (byte* ptr = array)
            return get_peak_multichannel(ptr, length / 4, channels, channelIndex);
    }

    public static void ApplyVolume(byte[] array, int length, float volume)
    {
        if (length <= 0 || length > array.Length)
            throw new ArgumentOutOfRangeException("length");
        fixed (byte* ptr = array)
            apply_volume(ptr, length / 4, volume);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct EncoderParameters
{
    public int SampleRate;
    public int Channels;
    public int FrameSizeInSamples;
    public int FrameSizeInBytes;
    public nint CodecContext;
    public nint Packet;
    public nint Timebase;
    public nint CodecParameters;
    public nint Frame;
}
