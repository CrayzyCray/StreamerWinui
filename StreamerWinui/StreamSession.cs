using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using FFmpeg.AutoGen.Abstractions;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace StreamerWinui
{
    public unsafe struct Ddagrab
    {
        public AVInputFormat* inputFormat;
        public AVFormatContext* formatContext;
        public AVCodecParameters* codecParameters;
        public AVCodec* codec;
        public AVCodecContext* codecContext;
        public AVPacket* packet;
        public AVFrame* frame;

        public Ddagrab()
        {
            ffmpeg.avdevice_register_all();
            inputFormat = ffmpeg.av_find_input_format("lavfi");
            formatContext = ffmpeg.avformat_alloc_context();
            codecParameters = null;
            codec = null;
            codecContext = null;
            packet = ffmpeg.av_packet_alloc();
            frame = ffmpeg.av_frame_alloc();
        }
        public void freeContexts()
        {
            ffmpeg.avformat_free_context(formatContext);
        }
    }

    public unsafe struct Encoder
    {
        public AVCodec* codec;
        public AVCodecParameters* codecParameters;
        public AVCodecContext* codecContext;
        public AVPacket* packet;

        public Encoder()
        {
            codec = null;
            codecParameters = ffmpeg.avcodec_parameters_alloc();
            codecContext = null;
            packet = ffmpeg.av_packet_alloc();
        }
    }

    public unsafe class StreamSession
    {
        public bool StreamIsActive { get { return streamIsActive; } }

        bool streamIsActive = false;
        unsafe Ddagrab ddagrab1;
        unsafe Encoder encoder1;
        int response;

        public unsafe void startStream(string codecName, string framerate, string ipToStream, bool showConsole = false)
        {
            ddagrab1 = new Ddagrab();
            encoder1 = new Encoder();

            fixed (Ddagrab* ddagrab = &ddagrab1)
                fixed (Encoder* encoder = &encoder1) 
            {
                //инициализация ddagrab
                response = ffmpeg.avformat_open_input(&ddagrab->formatContext, "ddagrab=0,hwdownload,format=bgra", ddagrab->inputFormat, null);
                response = ffmpeg.avformat_find_stream_info(ddagrab->formatContext, null);
                ddagrab->codecParameters = ddagrab->formatContext->streams[0]->codecpar;
                ddagrab->codec = ffmpeg.avcodec_find_decoder(ddagrab->codecParameters->codec_id);
                ddagrab->codecContext = ffmpeg.avcodec_alloc_context3(ddagrab->codec);
                response = ffmpeg.avcodec_parameters_to_context(ddagrab->codecContext, ddagrab->codecParameters);
                response = ffmpeg.avcodec_open2(ddagrab->codecContext, ddagrab->codec, null);

                debugLogUnmanagedPtr(ddagrab->codec->long_name);
                debugLogUnmanagedPtr(ddagrab->codec->name);




                //инициализация выходного формата
                string outputFilePath = @"D:\video\img\%03d.bmp";
                AVFormatContext* outputFormatContext = null;
                AVOutputFormat* outputFormat = null;
                outputFormat = ffmpeg.av_guess_format(null, outputFilePath, null);
                debugLogUnmanagedPtr(outputFormat->long_name);
                Debug.WriteLine("fps = " + ddagrab->formatContext->streams[0]->avg_frame_rate.num + "/" + ddagrab->formatContext->streams[0]->avg_frame_rate.den);

                if (false)
                {
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    for (int i = 0; i < 50; i++)
                    {
                        long t = stopwatch.ElapsedMilliseconds;
                        response = ffmpeg.av_read_frame(ddagrab->formatContext, ddagrab->packet);
                        Debug.WriteLine($"frame number {i}, time to read {stopwatch.ElapsedMilliseconds - t}, response {response}");
                        ffmpeg.av_packet_unref(ddagrab->packet);
                    }

                    stopwatch.Stop();
                    Debug.WriteLine("total " + stopwatch.ElapsedMilliseconds);
                }



                encoder->codec = ffmpeg.avcodec_find_encoder(ffmpeg.av_guess_codec(outputFormat, null, outputFilePath, null, AVMediaType.AVMEDIA_TYPE_VIDEO));
                encoder->codecContext = ffmpeg.avcodec_alloc_context3(encoder->codec);

                encoder->codecContext->time_base = ddagrab->formatContext->streams[0]->time_base;
                //encoder->codecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_RGBA;
                encoder->codecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_BGRA;
                encoder->codecContext->width = ddagrab->codecContext->width;
                encoder->codecContext->height = ddagrab->codecContext->height;

                response = ffmpeg.avcodec_open2(encoder->codecContext, encoder->codec, null);
                    response = ffmpeg.avformat_alloc_output_context2(&outputFormatContext, outputFormat, null, $"D:\\video\\img\\%03d.bmp");
                    ffmpeg.avformat_new_stream(outputFormatContext, encoder->codec);





                //получение и сохранение кадров
                for (int i = 0; i < 10; i++)
                {
                    response = ffmpeg.av_read_frame(ddagrab->formatContext, ddagrab->packet);
                    response = ffmpeg.avcodec_send_packet(ddagrab->codecContext, ddagrab->packet);
                    response = ffmpeg.avcodec_receive_frame(ddagrab->codecContext, ddagrab->frame);

                    response = ffmpeg.avcodec_send_frame(encoder->codecContext, ddagrab->frame);
                    response = ffmpeg.avcodec_receive_packet(encoder->codecContext, encoder->packet);
                    



                    response = ffmpeg.avformat_write_header(outputFormatContext, null);
                    response = ffmpeg.av_write_frame(outputFormatContext, encoder->packet);
                    ffmpeg.av_packet_unref(encoder->packet);
                }

                //AVCodec* encoderCodec = null;
                //AVCodecContext* encoderCodecContext = null;
                //AVCodecParameters* encoderCodecParameters = null;
                //AVPacket* encoderPacket = null;
                //encoderCodec = ffmpeg.avcodec_find_encoder_by_name("png");
                //encoderCodecContext = ffmpeg.avcodec_alloc_context3(encoderCodec);
                //encoderCodecContext->height = gdigrab->codecContext->height;
                //encoderCodecContext->width = gdigrab->codecContext->width;
                //encoderCodecContext->time_base = gdigrab->codecContext->time_base;
                //encoderCodecContext->framerate = gdigrab->codecContext->framerate;
                //response = ffmpeg.avcodec_open2(encoderCodecContext, encoderCodec, null);
                //ffmpeg.avcodec_parameters_copy();

            }

            streamIsActive = true;
        }





        public void stopStream()
        {
            ddagrab1.freeContexts();
            streamIsActive = false;
        }

        public void debugLogUnmanagedPtr(byte* ptr)
        {
            Debug.WriteLine(Marshal.PtrToStringAnsi((IntPtr)ptr));
        }

        public string unmanagedPtrToString(byte* ptr)
        {
            return Marshal.PtrToStringAnsi((IntPtr)ptr);
        }

        public struct Codec
        {
            public Codec(string _userFriendlyName, string _name)
            {
                userFriendlyName = _userFriendlyName;
                name = _name;
            }
            public string userFriendlyName { get; }
            public string name { get; }
        }

        public Codec[] supportedCodecs =
        {
            new Codec("hevc Nvidia", "hevc_nvenc"),
            new Codec("hevc AMD", "hevc_amf"),
            new Codec("h264 Nvidia", "h264_nvenc"),
            new Codec("h264 AMD", "h264_amf"),
        };

        public static void inicialize()
        {
            setFFMpegBinaresPath(@"C:\Users\Cray\Desktop\Programs\ffmpeg");
            DynamicallyLoadedBindings.Initialize();
        }

        static void setFFMpegBinaresPath()
        {
            string ffmpegBinaresPath = Path.Combine(Environment.CurrentDirectory, "ffmpeg", "bin");
            DynamicallyLoadedBindings.LibrariesPath = ffmpegBinaresPath;
        }

        static void setFFMpegBinaresPath(string path) 
        { 
            DynamicallyLoadedBindings.LibrariesPath = path; 
        }
    }
}
