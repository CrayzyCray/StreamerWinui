using FFmpeg.AutoGen.Abstractions;

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

        public void free()
        {
            ffmpeg.av_free(inputFormat);
            fixed(AVCodecContext** ptr = &codecContext)
                ffmpeg.avcodec_free_context(ptr);
        }
    }

    public unsafe struct Encoder
    {
        public AVCodec* codec;
        public AVCodecParameters* codecParameters;
        public AVCodecContext* codecContext;
        public AVPacket* packet;
        public AVFrame* hwFrame;

        public Encoder()
        {
            codec = null;
            codecParameters = ffmpeg.avcodec_parameters_alloc();
            codecContext = null;
            packet = ffmpeg.av_packet_alloc();
            hwFrame = ffmpeg.av_frame_alloc();
        }

        /// <summary>
        /// parameters gets form this field
        /// works only with d3d11 and bgra
        /// </summary>
        /// <param name="formatContext"></param>
        public void initHevcNvenc(AVFormatContext* formatContext, AVBufferRef* hwFramesContextNew)
        {
            codec = ffmpeg.avcodec_find_encoder_by_name("hevc_nvenc");
            codecContext = ffmpeg.avcodec_alloc_context3(codec);
            codecContext->time_base = formatContext->streams[0]->time_base;
            codecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_D3D11;
            codecContext->width = formatContext->streams[0]->codecpar->width;
            codecContext->height = formatContext->streams[0]->codecpar->height;
            codecContext->max_b_frames = 0;
            codecContext->framerate = ffmpeg.av_guess_frame_rate(null, formatContext->streams[0], null);

            codecContext->hw_frames_ctx = ffmpeg.av_buffer_ref(hwFramesContextNew);
            ffmpeg.av_hwframe_get_buffer(codecContext->hw_frames_ctx, hwFrame, 0);
            ffmpeg.avcodec_open2(codecContext, codec, null);
            
            ffmpeg.avcodec_parameters_from_context(codecParameters, codecContext);
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
        public void init()
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
        public string userFriendlyName { get; }
        public string name { get; }

        public Codec(string _userFriendlyName, string _name)
        {
            userFriendlyName = _userFriendlyName;
            name = _name;
        }
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