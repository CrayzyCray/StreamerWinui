using FFmpeg.AutoGen.Abstractions;

namespace StreamerWinui;

public unsafe class Streamer
{
    public List<nint> formatContexts = new();
    /// <summary>
    /// List[StreamIndex]
    /// </summary>
    public List<AVRational> Timebases => _timebases;
    
    private List<nint> _codecParametersArray = new();
    private List<AVRational> _timebases = new();
    
    /// <returns>stream index</returns>
    public int AddAvStream(AVCodecParameters* codecParameters, AVRational timebase = new AVRational())
    {
        _codecParametersArray.Add((nint)codecParameters);
        _timebases.Add(timebase);
        return _codecParametersArray.Count - 1;
    }
    
    public AVFormatContext* AddClient(string ipWithPort)
    {
        string outputUrl = $"rist://{ipWithPort}";
        AVFormatContext* formatContext = null;
        ffmpeg.avformat_alloc_output_context2(&formatContext, null, "mpegts", null);

        for (int i = 0; i < _timebases.Count; i++)
        {
            AVStream* stream = ffmpeg.avformat_new_stream(formatContext,
                ffmpeg.avcodec_find_encoder( ( (AVCodecParameters*)(_codecParametersArray[i]) )->codec_id));
            
            ffmpeg.avcodec_parameters_copy(formatContext->streams[i]->codecpar, (AVCodecParameters*)(_codecParametersArray[i]));
            formatContext->streams[i]->time_base = _timebases[i];
            //stream->time_base = _timebases[i];
        }
        ffmpeg.avio_open(&formatContext->pb, outputUrl, 2);
        ffmpeg.avformat_write_header(formatContext, null);
        formatContexts.Add((nint)formatContext);
        
        for (int i = 0; i < _timebases.Count; i++)
            _timebases[i] = formatContext->streams[i]->time_base;
        
        return formatContext;
    }

    public int WriteFrame(AVPacket* packet)
    {
        foreach (var formatContext in formatContexts)
        {
            int ret = ffmpeg.av_interleaved_write_frame((AVFormatContext*)formatContext, packet);
            if (ret < 0)
                return ret;
        }

        return 0;
    }
}