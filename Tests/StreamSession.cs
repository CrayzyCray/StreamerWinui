using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using FFmpeg.AutoGen.Abstractions;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace StreamerWinui
{
    public class StreamSession
    {
        public bool StreamIsActive { get { return streamIsActive; } }
        public Thread thread { get;}

        bool streamIsActive = false;
        unsafe Ddagrab ddagrab;
        unsafe Encoder encoder;
        bool stopStreamFlag = false;
        int response;
        string format = "";
        string codecName = "";
        string framerate = "";
        string ipToStream = "";
        string outputUrl = "";

        public unsafe void startStream(string formatStream, string codecNameStream = "", string framerateStream = "30", string ipToStreamStream = "localhost", bool showConsoleStream = false)
        {
            format = formatStream;
            codecName = codecNameStream;
            framerate = framerateStream;
            ipToStream = ipToStreamStream;
            thread.Start();
            streamIsActive = true;
        }

        public unsafe void startProcess()
        {
            ddagrab = new Ddagrab();
            encoder = new Encoder();

            //ddagrab.init();
            ddagrab.init("video_size=800x600");
            encoder.initHevcNvenc(ddagrab.formatContext, ddagrab.hwFrame->hw_frames_ctx);

            if (format == "mpegts")
                outputUrl = $"rist://localhost:10000";
            else
                outputUrl = $"D:\\video\\img\\1.{format}";

            //инициализация выходного формата
            AVOutputFormat* outputFormat = ffmpeg.av_guess_format(format, null, null);
            debugLogUnmanagedPtr(outputFormat->long_name);
            AVFormatContext* outputFormatContext = null;
            response = ffmpeg.avformat_alloc_output_context2(&outputFormatContext, outputFormat, null, outputUrl);
            var s = ffmpeg.avformat_new_stream(outputFormatContext, encoder.codec);
            response = ffmpeg.avcodec_parameters_from_context(outputFormatContext->streams[0]->codecpar, encoder.codecContext);
            outputFormatContext->streams[0]->time_base.num = ddagrab.formatContext->streams[0]->avg_frame_rate.den;
            outputFormatContext->streams[0]->time_base.den = ddagrab.formatContext->streams[0]->avg_frame_rate.num;
            response = ffmpeg.avio_open(&outputFormatContext->pb, outputUrl, 2);
            response = ffmpeg.avformat_write_header(outputFormatContext, null);


            //main loop
            while (true)
            {
                if (stopStreamFlag)
                    break;

                response = ffmpeg.av_read_frame(ddagrab.formatContext, ddagrab.packet);
                response = ffmpeg.avcodec_send_packet(ddagrab.codecContext, ddagrab.packet);
                response = ffmpeg.avcodec_receive_frame(ddagrab.codecContext, ddagrab.hwFrame);
                response = ffmpeg.avcodec_send_frame(encoder.codecContext, ddagrab.hwFrame);
                response = ffmpeg.avcodec_receive_packet(encoder.codecContext, encoder.packet);

                ffmpeg.av_packet_rescale_ts(encoder.packet, encoder.codecContext->time_base, outputFormatContext->streams[0]->time_base);
                if (response == 0) 
                    response = ffmpeg.av_write_frame(outputFormatContext, encoder.packet);
                ffmpeg.av_packet_unref(ddagrab.packet);
                Console.WriteLine($"frame {encoder.codecContext->frame_number} writed");
            }

            response = ffmpeg.av_write_trailer(outputFormatContext);
            ffmpeg.avio_close(outputFormatContext->pb);
            ffmpeg.avformat_free_context(outputFormatContext);

            stopStreamFlag = false;
            streamIsActive = false;
        }

        public void stopStream()
        {
            stopStreamFlag = true;
        }

        public StreamSession()
        {
            if (DynamicallyLoadedBindings.LibrariesPath == String.Empty)
                inicialize();
            thread = new Thread(startProcess);
        }

        public static Codec[] supportedCodecs =
        {
            new Codec("hevc Nvidia", "hevc_nvenc"),
            new Codec("hevc AMD", "hevc_amf"),
            new Codec("h264 Nvidia", "h264_nvenc"),
            new Codec("h264 AMD", "h264_amf"),
        };

        public unsafe static void errStrPrint(int errNum)
        {
            byte[] str1 = new byte[64];
            fixed (byte* str2 = str1)
            {
                ffmpeg.av_make_error_string(str2, 64, errNum);
                debugLogUnmanagedPtr(str2);
            }
        }

        public unsafe static void debugLogUnmanagedPtr(byte* ptr)
        {
            Debug.WriteLine(Marshal.PtrToStringAnsi((IntPtr)ptr));
        }

        public unsafe static string unmanagedPtrToString(byte* ptr)
        {
            return Marshal.PtrToStringAnsi((IntPtr)ptr) ?? "";
        }

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
