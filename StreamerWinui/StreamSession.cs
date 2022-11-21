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
        //unsafe PixelFormatConverter pixFmtConv1;
        int response;

        public unsafe void startStream(string codecName, string format, string framerate = "30", string ipToStream = "localhost", bool showConsole = false)
        {
            ddagrab1 = new Ddagrab();
            encoder1 = new Encoder();
            //pixFmtConv1 = new PixelFormatConverter();

            fixed (Ddagrab* ddagrab = &ddagrab1)
            fixed (Encoder* encoder = &encoder1) 
            //fixed (PixelFormatConverter* pixFmtConv = &pixFmtConv1)
            {
                //инициализация ddagrab
                response = ffmpeg.avformat_open_input(&ddagrab->formatContext, "ddagrab", ddagrab->inputFormat, null);
                response = ffmpeg.avformat_find_stream_info(ddagrab->formatContext, null);
                ddagrab->codecParameters = ddagrab->formatContext->streams[0]->codecpar;
                ddagrab->codec = ffmpeg.avcodec_find_decoder(ddagrab->codecParameters->codec_id);
                ddagrab->codecContext = ffmpeg.avcodec_alloc_context3(ddagrab->codec);
                response = ffmpeg.avcodec_parameters_to_context(ddagrab->codecContext, ddagrab->codecParameters);
                response = ffmpeg.avcodec_open2(ddagrab->codecContext, ddagrab->codec, null);

                //инициализация выходного формата
                AVFormatContext* outputFormatContext = null;
                AVOutputFormat* outputFormat = null;
                outputFormat = ffmpeg.av_guess_format(format, null, null);
                debugLogUnmanagedPtr(outputFormat->long_name);
                Debug.WriteLine("fps = " + ddagrab->formatContext->streams[0]->avg_frame_rate.num + "/" + ddagrab->formatContext->streams[0]->avg_frame_rate.den);

                //encoder setup
                encoder->codec = ffmpeg.avcodec_find_encoder_by_name("hevc_nvenc");
                encoder->codecContext = ffmpeg.avcodec_alloc_context3(encoder->codec);
                encoder->codecContext->time_base = ddagrab->formatContext->streams[0]->time_base;
                encoder->codecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_D3D11;
                encoder->codecContext->width = ddagrab->codecContext->width;
                encoder->codecContext->height = ddagrab->codecContext->height;
                response = ffmpeg.avcodec_open2(encoder->codecContext, encoder->codec, null);

                AVCodecHWConfig* codecHWConfig;
                for (int i = 0;; i++)
                {
                    codecHWConfig = ffmpeg.avcodec_get_hw_config(encoder->codec, i);
                    if (codecHWConfig->device_type == AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA && codecHWConfig->pix_fmt == AVPixelFormat.AV_PIX_FMT_D3D11)
                        break;
                }
                if (codecHWConfig == null)
                    throw new Exception("Hw device not found");

                //AVHWDeviceContext* hwDeviceContext = ffmpeg.av_hwdevice_ctx_alloc(AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA);
                //AVHWFramesContext* frameContext = ffmpeg.av_hwframe_ctx_alloc(null);


                //filtering
                //string arg = $"video_size={ddagrab->codecContext->width}x{ddagrab->codecContext->height}:pix_fmt={(long)ddagrab->codecContext->pix_fmt}:time_base={ddagrab->formatContext->streams[0]->time_base.num}/{ddagrab->formatContext->streams[0]->time_base.den}:pixel_aspect={ddagrab->codecContext->sample_aspect_ratio.num}/{ddagrab->codecContext->sample_aspect_ratio.den}";
                //response = ffmpeg.avfilter_graph_create_filter(&pixFmtConv->bufferSrcContext, pixFmtConv->bufferSrc, "in", arg, null, pixFmtConv->filterGraph);
                //response = ffmpeg.avfilter_graph_create_filter(&pixFmtConv->bufferSinkContext, pixFmtConv->bufferSink, "out", null, null, pixFmtConv->filterGraph);
                //pixFmtConv->outputs->name = ffmpeg.av_strdup("in");
                //pixFmtConv->outputs->filter_ctx = pixFmtConv->bufferSrcContext;
                //pixFmtConv->outputs->pad_idx = 0;
                //pixFmtConv->outputs->next = null;
                //pixFmtConv->inputs->name = ffmpeg.av_strdup("out");
                //pixFmtConv->inputs->filter_ctx= pixFmtConv->bufferSinkContext;
                //pixFmtConv->inputs->pad_idx = 0;
                //pixFmtConv->inputs->next = null;
                //response = ffmpeg.av_opt_set_bin(pixFmtConv->bufferSinkContext, "pix_fmts", (byte*)&encoder->codecContext->pix_fmt, sizeof(AVPixelFormat), 1<<0);
                //response = ffmpeg.avfilter_graph_parse_ptr(pixFmtConv->filterGraph, "format=rgba", &pixFmtConv->inputs, &pixFmtConv->outputs, null);
                //response = ffmpeg.avfilter_graph_config(pixFmtConv->filterGraph, null);

                //получение и сохранение кадров
                response = ffmpeg.avformat_alloc_output_context2(&outputFormatContext, outputFormat, null, $"D:\\video\\img\\1.{format}");
                ffmpeg.avformat_new_stream(outputFormatContext, encoder->codec);
                for (int i = 0; i < 60; i++)
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
                ffmpeg.avformat_free_context(outputFormatContext);
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

        public StreamSession()
        {
            if (DynamicallyLoadedBindings.LibrariesPath == String.Empty)
                inicialize();
        }
    }
}
