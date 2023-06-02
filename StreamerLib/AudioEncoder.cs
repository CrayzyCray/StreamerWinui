using System.Runtime.InteropServices;

namespace StreamerLib;

/*
public unsafe struct Ddagrab : IDisposable
{
    public AVInputFormat* inputFormat;
    public AVFormatContext* formatContext;
    public AVCodecParameters* codecParameters;
    public AVCodec* codec;
    public AVCodecContext* codecContext;
    public AVPacket* packet;
    public AVFrame* hwFrame;
    public AVRational timebaseMin;

    public Ddagrab(string ddagrabParameters = "")
    {
        ffmpeg.avdevice_register_all();
        inputFormat = ffmpeg.av_find_input_format("lavfi");
        formatContext = ffmpeg.avformat_alloc_context();
        codecParameters = null;
        codec = null;
        codecContext = null;
        packet = ffmpeg.av_packet_alloc();
        hwFrame = ffmpeg.av_frame_alloc();
        
        fixed(AVFormatContext** fc = &formatContext)
            ffmpeg.avformat_open_input(fc, $"ddagrab={ddagrabParameters}", inputFormat, null);
        ffmpeg.avformat_find_stream_info(formatContext, null);
        codecParameters = formatContext->streams[0]->codecpar;
        codec = ffmpeg.avcodec_find_decoder(codecParameters->codec_id);
        codecContext = ffmpeg.avcodec_alloc_context3(codec);
        ffmpeg.avcodec_parameters_to_context(codecContext, codecParameters);
        ffmpeg.avcodec_open2(codecContext, codec, null);

        ffmpeg.av_read_frame(formatContext, packet);
        ffmpeg.avcodec_send_packet(codecContext, packet);
        ffmpeg.avcodec_receive_frame(codecContext, hwFrame);
        timebaseMin.num = formatContext->streams[0]->avg_frame_rate.den;
        timebaseMin.den = formatContext->streams[0]->avg_frame_rate.num;
    }
    
    /// <summary>
    /// read, decode and write AVFrame to this.hwFrame
    /// </summary>
    /// <returns>this.hwFrame</returns>
    public AVFrame* ReadAvFrame()
    {
        ffmpeg.av_read_frame(formatContext, packet);
        ffmpeg.avcodec_send_packet(codecContext, packet);
        ffmpeg.avcodec_receive_frame(codecContext, hwFrame);
        
        return hwFrame;
    }

    public void Dispose()
    {
        if (formatContext != null)
            ffmpeg.avformat_free_context(formatContext);
        
        if (codecParameters != null)
            fixed(AVCodecParameters** p = &codecParameters)
                ffmpeg.avcodec_parameters_free(p);
        
        if (packet != null || codecContext != null)
        {
            ffmpeg.av_packet_unref(packet);
            ffmpeg.avcodec_send_packet(codecContext, packet);//flush codecContext
        }
        
        if (codecContext != null)
            fixed (AVCodecContext** p = &codecContext)
                ffmpeg.avcodec_free_context(p);
        
        if (packet != null)
            fixed(AVPacket** p = &packet)
                ffmpeg.av_packet_free(p);
        
        if (hwFrame != null)
            fixed(AVFrame** p = &hwFrame)
                ffmpeg.av_frame_free(p);
    }
}
*/

/*
public unsafe struct HardwareEncoder : IDisposable
{
    public AVCodec* codec;
    public AVCodecParameters* codecParameters;
    public AVCodecContext* codecContext;
    public AVPacket* packet;
    public int StreamIndex;
    private StreamWriter _streamWriter;
    private AVRational _timebase;

    public HardwareEncoder(AVFormatContext* formatContext, AVBufferRef* hwFramesContext, string hardwareEncoderName, StreamWriter streamWriter)
    {
        _streamWriter = streamWriter;
        codec = null;
        codecParameters = ffmpeg.avcodec_parameters_alloc();
        codecContext = null;
        packet = ffmpeg.av_packet_alloc();
        
        codec = ffmpeg.avcodec_find_encoder_by_name(hardwareEncoderName);
        codecContext = ffmpeg.avcodec_alloc_context3(codec);
        _timebase = formatContext->streams[0]->time_base;
        codecContext->time_base = _timebase;
        codecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_D3D11;
        codecContext->width = formatContext->streams[0]->codecpar->width;
        codecContext->height = formatContext->streams[0]->codecpar->height;
        codecContext->max_b_frames = 0;
        codecContext->framerate = ffmpeg.av_guess_frame_rate(null, formatContext->streams[0], null);
        codecContext->hw_frames_ctx = ffmpeg.av_buffer_ref(hwFramesContext);
        ffmpeg.avcodec_open2(codecContext, codec, null);
        ffmpeg.avcodec_parameters_from_context(codecParameters, codecContext);
    }

    public void EncodeAndWriteFrame(AVFrame* hwFrame)
    {
        ffmpeg.avcodec_send_frame(codecContext, hwFrame);
        if (ffmpeg.avcodec_receive_packet(codecContext, packet) != 0)
            return;
        ffmpeg.av_packet_rescale_ts(packet, codecContext->time_base, _streamWriter.GetStreamParameters(StreamIndex).Timebase);
        _streamWriter.WriteFrame(packet, _timebase);
        Console.WriteLine("frame " + codecContext->frame_number + " writed");
    }

    public void Dispose()
    {
        fixed(AVCodecParameters** p = &codecParameters)
            ffmpeg.avcodec_parameters_free(p);
        ffmpeg.av_packet_unref(packet);
        ffmpeg.avcodec_send_packet(codecContext, packet); //flush codecContext
        fixed (AVCodecContext** p = &codecContext)
            ffmpeg.avcodec_free_context(p);
        fixed(AVPacket** p = &packet)
            ffmpeg.av_packet_free(p);
    }
}
*/

public sealed class AudioEncoder : IDisposable
{
    public const int AV_SAMPLE_FMT_FLT = 3;
    public const int SampleSizeInBytes = 4;

    public int FrameSizeInSamples => _encoderParameters.FrameSizeInSamples;
    public int FrameSizeInBytes { get; }
    public int Channels => _encoderParameters.Channels;
    public int StreamIndex { get; private set; } = -1;
    public int SampleRate => _encoderParameters.SampleRate;
    public int SampleFormat => AV_SAMPLE_FMT_FLT;

    EncoderParameters _encoderParameters;
    private StreamWriter _streamWriter;
    private long _packetTimeStamp;

    public AudioEncoder(StreamWriter streamWriter, Codecs encoder, int channels = 2)
    {
        _streamWriter = streamWriter;
        string encoderName = encoder switch
        {
            Codecs.LibOpus => "libopus",
            _ => "libopus"
        };

        _encoderParameters = LibUtil.audio_encoder_constructor(encoderName, channels, 48000);

        FrameSizeInBytes = _encoderParameters.FrameSizeInSamples * channels * SampleSizeInBytes;
    }

    public void EncodeAndWriteFrame(byte[] buffer)
    {
        if (StreamIndex == -1)
            return;
        if (buffer.Length != FrameSizeInBytes)
            throw new ArgumentException();

        bool success;

        unsafe
        {
            fixed (byte* buf = buffer)
            {
                success = FFmpegImport.AudioEncoder_EncodeAndWriteFrame(
                    buf,
                    FrameSizeInBytes,
                    Channels,
                    StreamIndex,
                    _packetTimeStamp,
                    _encoderParameters.CodecContext,
                    _encoderParameters.Packet,
                    _encoderParameters.Frame);
                if (success)
                    _streamWriter.WriteFrame(_encoderParameters.Packet, _encoderParameters.Timebase, StreamIndex);
            }
        }

        _packetTimeStamp += FrameSizeInSamples;
    }

    public void Dispose()
    {
        FFmpegImport.AudioEncoder_Dispose(
            _encoderParameters.Packet,
            _encoderParameters.Frame,
            _encoderParameters.CodecContext,
            _encoderParameters.CodecParameters,
            _encoderParameters.Timebase);
    }

    public void Clean()
    {
        _packetTimeStamp = 0;
    }

    public void RegisterAVStream(StreamWriter streamWriter)
    {
        _streamWriter = streamWriter;
        StreamIndex = _streamWriter.AddAvStream(_encoderParameters.CodecParameters, _encoderParameters.Timebase);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct EncoderParameters
{
    public int SampleRate;
    public int Channels;
    public int FrameSizeInSamples;
    public nint CodecContext;
    public nint Packet;
    public nint Timebase;
    public nint CodecParameters;
    public nint Frame;
}