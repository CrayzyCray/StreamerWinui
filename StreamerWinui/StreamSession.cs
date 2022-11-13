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

namespace StreamerWinui
{
    public unsafe struct Gdigrab
    {
        public AVInputFormat* inputFormat;
        public AVFormatContext* formatContext;
        public AVCodecParameters* codecParameters;
        public AVCodec* codec;
        public AVCodecContext* codecContext;
        public AVPacket* packet;
        public AVFrame* frame;

        public Gdigrab()
        {
            ffmpeg.avdevice_register_all();
            inputFormat = ffmpeg.av_find_input_format("gdigrab");
            formatContext = ffmpeg.avformat_alloc_context();
            //codecParameters = formatContext->streams[0]->codecpar;
            //codec = ffmpeg.avcodec_find_decoder(codecParameters->codec_id);
            //codecContext = ffmpeg.avcodec_alloc_context3(codec);
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

    public unsafe class StreamSession
    {
        public bool StreamIsActive { get { return streamIsActive; } }

        bool streamIsActive = false;
        unsafe Gdigrab gdigrab1;

        public unsafe void startStream(string codec, double framerate = 30, string ipToStream = "localhost", bool showConsole = false)
        {
            gdigrab1 = new Gdigrab();

            fixed(Gdigrab* gdigrab = &gdigrab1)
            {
                ffmpeg.avformat_open_input(&gdigrab->formatContext, "desktop", gdigrab->inputFormat, null);
                ffmpeg.avformat_find_stream_info(gdigrab->formatContext, null);
                gdigrab->codecParameters = gdigrab->formatContext->streams[0]->codecpar;
                gdigrab->codec = ffmpeg.avcodec_find_decoder(gdigrab->codecParameters->codec_id);
            }








            //AVInputFormat* gdigrabInputFormat = ffmpeg.av_find_input_format("gdigrab");
            //AVFormatContext* gdigrabFormatContext = ffmpeg.avformat_alloc_context();
            //ffmpeg.avformat_open_input(&gdigrabFormatContext, "desktop", gdigrabInputFormat, null);
            //ffmpeg.avformat_find_stream_info(gdigrabFormatContext, null);
            //AVCodecParameters* gdigrabCodecParameters = gdigrabFormatContext->streams[0]->codecpar;
            //AVCodec* gdigrabCodec = ffmpeg.avcodec_find_decoder(gdigrabCodecParameters->codec_id);
            //AVCodecContext* gdigrabCodecContext = ffmpeg.avcodec_alloc_context3(gdigrabCodec);

            //debugLogUnmanagedPtr((IntPtr)gdigrabCodec->long_name);

            //AVPacket* gdigrabPacket = ffmpeg.av_packet_alloc();
            //AVFrame* gdigrabFrame = ffmpeg.av_frame_alloc();
            //ffmpeg.av_read_frame(gdigrabFormatContext, gdigrabPacket); //read packet
            //ffmpeg.avcodec_send_packet(gdigrabCodecContext, gdigrabPacket);


            streamIsActive = true;
            //void* opaque = null;
            //AVInputFormat* f = null;
            //while (true)
            //{
            //    f = ffmpeg.av_demuxer_iterate(&opaque);
            //    if (f == null)
            //        break;
            //    Debug.WriteLine(Marshal.PtrToStringAnsi((IntPtr)f->name));
            //}
        }





        public void stopStream()
        {
            gdigrab1.freeContexts();
            streamIsActive = false;
        }

        public void debugLogUnmanagedPtr(IntPtr ptr)
        {
            Debug.WriteLine(Marshal.PtrToStringAnsi(ptr));
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
            new Codec("hevc Intel", "hevc_qsv"),
            new Codec("h264 Nvidia", "h264_nvenc"),
            new Codec("h264 AMD", "h264_amf"),
            new Codec("h264 Intel", "h264_qsv")
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
