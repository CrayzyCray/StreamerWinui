﻿using System.Diagnostics;
using FFmpeg.AutoGen.Abstractions;

namespace StreamerWinui
{
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
    
    public struct BufferByte
    {
        public byte[] Buffer => _buffer;
        public int Count => _count;
        public int Size => _buffer.Length;
        public int SizeRemain => Size - Count;
        public bool IsEmpty => Count == 0;
        public bool NotEmpty => Count != 0;
        public void clear() => _count = 0;
            
        private byte[] _buffer;
        private int _count;
            
        public void Append(byte value)
        {
            if (_count >= _buffer.Length)
            {
                Debug.WriteLine("buffer overflowed");
                return;
            }
            _buffer[_count] = value;
            _count++;
        }
            
        public BufferByte(int size)
        {
            _buffer = new byte[size];
            _count = 0;
        }
    }

    public unsafe struct HardwareEncoder : IDisposable
    {
        public AVCodec* codec;
        public AVCodecParameters* codecParameters;
        public AVCodecContext* codecContext;
        public AVPacket* packet;
        public int StreamIndex;
        private Streamer _streamer;

        public HardwareEncoder(AVFormatContext* formatContext, AVBufferRef* hwFramesContext, string hardwareEncoderName, Streamer streamer)
        {
            _streamer = streamer;
            codec = null;
            codecParameters = ffmpeg.avcodec_parameters_alloc();
            codecContext = null;
            packet = ffmpeg.av_packet_alloc();
            
            codec = ffmpeg.avcodec_find_encoder_by_name(hardwareEncoderName);
            codecContext = ffmpeg.avcodec_alloc_context3(codec);
            codecContext->time_base = formatContext->streams[0]->time_base;
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
            
            if (ffmpeg.avcodec_receive_packet(codecContext, packet) == 0)
            {
                ffmpeg.av_packet_rescale_ts(packet, codecContext->time_base, _streamer.Timebases[StreamIndex]);
                _streamer.WriteFrame(packet);
                Console.WriteLine("frame " + codecContext->frame_number + " writed");
            }
        }

        public void Dispose()
        {
            fixed(AVCodecParameters** p = &codecParameters)
                ffmpeg.avcodec_parameters_free(p);
            
            ffmpeg.av_packet_unref(packet);
            ffmpeg.avcodec_send_packet(codecContext, packet);//flush codecContext
            fixed (AVCodecContext** p = &codecContext)
                ffmpeg.avcodec_free_context(p);
            
            fixed(AVPacket** p = &packet)
                ffmpeg.av_packet_free(p);
        }
    }

    public unsafe struct PixelFormatConverter
    {
        public AVFilterGraph* filterGraph;
        public AVFilterContext* bufferSrcContext;
        public AVFilterContext* bufferSinkContext;
        public AVFilter* bufferSrc;
        public AVFilter* bufferSink;
        public AVFilterInOut* outputs;
        public AVFilterInOut* inputs;

        public PixelFormatConverter()
        {
            filterGraph = ffmpeg.avfilter_graph_alloc();
            bufferSrcContext = null;
            bufferSinkContext = null;
            bufferSrc = ffmpeg.avfilter_get_by_name("buffer");
            bufferSink = ffmpeg.avfilter_get_by_name("buffersink");
            outputs = ffmpeg.avfilter_inout_alloc();
            inputs = ffmpeg.avfilter_inout_alloc();
        }

        /// <summary>
        /// не реализован
        /// </summary>
        public void Init()
        {
            //filtering
            //string arg = $"video_size={ddagrab->codecContext->width}x{ddagrab->codecContext->height}:pix_fmt={(long)ddagrab->codecContext->pix_fmt}:time_base={ddagrab->formatContext->streams[0]->time_base.num}/{ddagrab->formatContext->streams[0]->time_base.den}:pixel_aspect={ddagrab->codecContext->sample_aspect_ratio.num}/{ddagrab->codecContext->sample_aspect_ratio.den}";
            //response = ffmpeg.avfilter_graph_create_filter(&pixFmtConv->bufferSrcContext, pixFmtConv->bufferSrc, "in", arg, null, pixFmtConv->filterGraph);
            //response = ffmpeg.avfilter_graph_create_filter(&pixFmtConv->bufferSinkContext, pixFmtConv->bufferSink, "out", null, null, pixFmtConv->filterGraph);
            //pixFmtConv->outputs->name = ffmpeg.av_strdup("in");
            //pixFmtConv->outputs->filter_ctx = pixFmtConv->bufferSrcContext;
            //pixFmtConv->outputs->pad_idx = 0;
            //pixFmtConv->outputs->next = null;
            //pixFmtConv->inputs->name = ffmpeg.av_strdup("out");
            //pixFmtConv->inputs->filter_ctx = pixFmtConv->bufferSinkContext;
            //pixFmtConv->inputs->pad_idx = 0;
            //pixFmtConv->inputs->next = null;
            //response = ffmpeg.av_opt_set_bin(pixFmtConv->bufferSinkContext, "pix_fmts", (byte*)&encoder->codecContext->pix_fmt, sizeof(AVPixelFormat), 1 << 0);
            //response = ffmpeg.avfilter_graph_parse_ptr(pixFmtConv->filterGraph, "format=rgba", &pixFmtConv->inputs, &pixFmtConv->outputs, null);
            //response = ffmpeg.avfilter_graph_config(pixFmtConv->filterGraph, null);
        }
    }

    public struct Codec
    {
        public string UserFriendlyName { get; }
        public string Name { get; }

        public Codec(string userFriendlyName, string name)
        {
            UserFriendlyName = userFriendlyName;
            Name = name;
        }
    }

    public enum Encoders
    {
        HevcNvenc,
        HevcAmf,
        H264Nvenc,
        H264Amf,
        Av1Nvenc
    }
}

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