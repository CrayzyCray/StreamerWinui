using FFmpeg.AutoGen.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
