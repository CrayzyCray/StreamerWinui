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
    public unsafe class StreamSession
    {
        public bool StreamIsActive { get { return streamIsActive; } }

        bool streamIsActive = false;
        unsafe Ddagrab ddagrab1;
        unsafe Encoder encoder1;
        int response;

        public unsafe void startStream(string format, string codecName = "", string framerate = "30", string ipToStream = "localhost", bool showConsole = false)
        {
            ddagrab1 = new Ddagrab();
            encoder1 = new Encoder();

            fixed (Ddagrab* ddagrab = &ddagrab1)
            fixed (Encoder* encoder = &encoder1)
            {
                ddagrab->init("video_size=800x600");
                encoder->initHevcNvenc(ddagrab->formatContext);

                //инициализация выходного формата
                AVOutputFormat* outputFormat = ffmpeg.av_guess_format(format, null, null);
                debugLogUnmanagedPtr(outputFormat->long_name);
                Debug.WriteLine("fps = " + ddagrab->formatContext->streams[0]->avg_frame_rate.num + "/" + ddagrab->formatContext->streams[0]->avg_frame_rate.den);
                AVFormatContext* outputFormatContext = null;
                response = ffmpeg.avformat_alloc_output_context2(&outputFormatContext, outputFormat, null, null);
                ffmpeg.avformat_new_stream(outputFormatContext, encoder->codec);
                response = ffmpeg.avcodec_parameters_from_context(outputFormatContext->streams[0]->codecpar, encoder->codecContext);
                outputFormatContext->streams[0]->time_base = ddagrab->formatContext->streams[0]->time_base;
                
                if (format == "mpegts")
                    response = ffmpeg.avio_open(&outputFormatContext->pb, $"rist://localhost:10000", 2);
                else
                    response = ffmpeg.avio_open(&outputFormatContext->pb, $"D:\\video\\img\\%0000000000d.{format}", 2);

                response = ffmpeg.avformat_write_header(outputFormatContext, null);



                AVFrame* frame = ffmpeg.av_frame_alloc(); //frame for copyng hwframes from decoder to encoder

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                //получение и сохранение кадров
                for (int i = 0; i < 500; i++)
                {
                    response = ffmpeg.av_read_frame(ddagrab->formatContext, ddagrab->packet);
                    response = ffmpeg.avcodec_send_packet(ddagrab->codecContext, ddagrab->packet);
                    response = ffmpeg.avcodec_receive_frame(ddagrab->codecContext, ddagrab->frame);
                    response = ffmpeg.av_hwframe_transfer_data(frame, ddagrab->frame, 0);
                    response = ffmpeg.av_hwframe_transfer_data(encoder->hwFrame, frame, 0);
                    //response = ffmpeg.av_hwframe_transfer_data(hwFrame, ddagrab->frame, 0);
                    ffmpeg.av_frame_copy_props(encoder->hwFrame, ddagrab->frame);
                    ffmpeg.av_frame_unref(frame);
                    ffmpeg.av_frame_unref(ddagrab->frame);

                    response = ffmpeg.avcodec_send_frame(encoder->codecContext, encoder->hwFrame);
                    response = ffmpeg.avcodec_receive_packet(encoder->codecContext, encoder->packet);
                    if (response == 0) 
                    {
                        if (outputFormatContext->streams[0]->start_time < 0)
                        {
                            outputFormatContext->streams[0]->start_time = encoder->packet->pts;
                        }
                        //Debug.WriteLine($"packet pts {ddagrab->packet->pts} dts {ddagrab->packet->dts}");
                        Debug.WriteLine($"frame {encoder->codecContext->frame_number} pts {encoder->hwFrame->pts} dts {encoder->hwFrame->pkt_dts}");
                        response = ffmpeg.av_interleaved_write_frame(outputFormatContext, encoder->packet);
                    }
                    ffmpeg.av_packet_unref(ddagrab->packet);
                    Console.WriteLine(encoder->codecContext->frame_number);
                }
                stopwatch.Stop();
                Console.WriteLine(stopwatch.Elapsed.TotalMilliseconds);
                ffmpeg.avio_close(outputFormatContext->pb);
                ffmpeg.avformat_free_context(outputFormatContext);
            }
            streamIsActive = true;
        }

        public void stopStream()
        {
            streamIsActive = false;
        }

        public static void errStrPrint(int err)
        {
            byte[] str1 = new byte[64];
            fixed (byte* str2 = str1)
            {
                ffmpeg.av_make_error_string(str2, 64, err);
                debugLogUnmanagedPtr(str2);
            }
        }

        public static void debugLogUnmanagedPtr(byte* ptr)
        {
            Debug.WriteLine(Marshal.PtrToStringAnsi((IntPtr)ptr));
        }

        public static string unmanagedPtrToString(byte* ptr)
        {
            string? str = Marshal.PtrToStringAnsi((IntPtr)ptr);
            return str ?? "";
        }

        public static Codec[] supportedCodecs =
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

        public StreamSession()
        {
            if (DynamicallyLoadedBindings.LibrariesPath == String.Empty)
                inicialize();
        }
    }
}
