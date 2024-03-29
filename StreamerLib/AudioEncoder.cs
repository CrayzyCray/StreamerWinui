﻿namespace StreamerLib;

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

public unsafe class AudioEncoder : IDisposable
{
    public const int AV_SAMPLE_FMT_FLT = 3;

    public int SampleSizeInBytes { get; } = 4;
    public int FrameSizeInSamples { get; }
    public int FrameSizeInBytes { get; }
    public int Channels { get; }
    public int StreamIndex { get; private set; } = -1;
    public int SampleRate => _sampleRate;
    public int SampleFormat => AV_SAMPLE_FMT_FLT;

    private IntPtr _codecContext;
    private IntPtr _avFrame;
    private IntPtr _packet;
    private StreamWriter? _streamWriter;
    private long _pts;
    private IntPtr _timebase;
    private IntPtr _codecParameters;
    private int _sampleRate;

    public AudioEncoder(StreamWriter streamWriter, Encoders encoder, int channels = 2)
    {
        string encoderName = encoder switch
        {
            Encoders.LibOpus => "libopus",
            _ => "libopus"
        };

        _sampleRate = 48000; //encoder specified
        Channels = channels;

        int frameSizeInSamples = -1;
        nint codecContext, packet, timebase, codecParameters, frame;

        FFmpegImport.AudioEncoder_Constructor(
            encoderName,
            48000,
            channels,
            &frameSizeInSamples,
            &codecContext,
            &packet,
            &timebase,
            &codecParameters,
            &frame);

        _codecContext = codecContext;
        _packet = packet;
        _timebase = timebase;
        _codecParameters = codecParameters;
        _avFrame = frame;
        
        FrameSizeInSamples = frameSizeInSamples;
        FrameSizeInBytes = frameSizeInSamples * channels * SampleSizeInBytes;
    }

    public void EncodeAndWriteFrame(byte[] buffer)
    {
        if (StreamIndex == -1)
            return;
        if (buffer.Length != FrameSizeInBytes)
            throw new ArgumentException();

        bool success;
        fixed (byte* buf = buffer)
        {
            success = FFmpegImport.AudioEncoder_EncodeAndWriteFrame(
                buf,
                FrameSizeInBytes,
                Channels,
                StreamIndex,
                _pts,
                _codecContext,
                _packet,
                _avFrame);
            if (success)
                _streamWriter.WriteFrame(_packet, _timebase, StreamIndex);
        }

        _pts += FrameSizeInSamples;
    }

    public void Dispose()
    {
        FFmpegImport.AudioEncoder_Dispose(
            _packet, 
            _avFrame, 
            _codecContext, 
            _codecParameters, 
            _timebase);
    }

    public void Clean()
    {
        _pts = 0;
    }

    public void RegisterAVStream(StreamWriter streamWriter)
    {
        _streamWriter = streamWriter;
        StreamIndex = _streamWriter.AddAvStream(_codecParameters, _timebase);
    }
}

/*
public static class FFmpegHelper
{
    public static unsafe void ErrStrPrint(int errNum)
    {
        int maxErrorStringSize = 64;
        byte[] str1 = new byte[maxErrorStringSize];
        fixed (byte* str2 = str1)
        {
            ffmpeg.av_make_error_string(str2, 64, errNum);
            DebugLogUnmanagedPtr(str2);
        }
    }
    
    public static void InicializeFFmpeg()
    {
        SetFfmpegBinaresPath(@"C:\Users\Cray\Desktop\Programs\ffmpeg");
        DynamicallyLoadedBindings.Initialize();
    }

    public static unsafe void DebugLogUnmanagedPtr(byte* ptr) =>
        Debug.WriteLine(Marshal.PtrToStringAnsi((IntPtr)ptr));

    public static unsafe string UnmanagedPtrToString(byte* ptr) =>
        Marshal.PtrToStringAnsi((IntPtr)ptr) ?? String.Empty;

    static void SetFfmpegBinaresPath()=>
        DynamicallyLoadedBindings.LibrariesPath = Path.Combine(Environment.CurrentDirectory, "ffmpeg", "bin");

    static void SetFfmpegBinaresPath(string path) =>
        DynamicallyLoadedBindings.LibrariesPath = path; 
}
*/


//hwframes
//AVBufferRef* hwDeviceContext = null;
//ffmpeg.av_hwdevice_ctx_create(&hwDeviceContext, AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA, null, null, 0);
//AVBufferRef* hwFramesRef = ffmpeg.av_hwframe_ctx_alloc(hwDeviceContext);
//AVHWFramesContext* hwFramesContext = (AVHWFramesContext*)(hwFramesRef->data);
//hwFramesContext->format = AVPixelFormat.AV_PIX_FMT_D3D11;
//hwFramesContext->sw_format = AVPixelFormat.AV_PIX_FMT_BGRA;
//hwFramesContext->width = codecContext->width;
//hwFramesContext->height = codecContext->height;
//ffmpeg.av_hwframe_ctx_init(hwFramesRef);