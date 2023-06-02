namespace StreamerLib;

/*
/// Not implemented
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
*/

public readonly struct Codec
{
    public string UserFriendlyName { get; }
    public string Name { get; }
    public Codecs Encoder { get; }
    public MediaTypes MediaType { get; }

    public Codec(string UserFriendlyName,
        string Name,
        Codecs Encoder,
        MediaTypes MediaType)
    {
        this.UserFriendlyName = UserFriendlyName;
        this.Name = Name;
        this.Encoder = Encoder;
        this.MediaType = MediaType;
    }
}

public enum Codecs
{
    HevcNvenc,
    HevcAmf,
    H264Nvenc,
    H264Amf,
    Av1Nvenc,
    LibOpus
}

public enum MediaTypes
{
    Audio,
    Video
}