using System.Net;
using FFmpeg.AutoGen.Abstractions;

namespace StreamerWinui;

public unsafe class Streamer : IDisposable
{
    public const int DefaultPort = 10000;

    /// List[StreamIndex]
    public List<IntPtr> FormatContexts => _formatContexts;
    
    /// List[StreamIndex]
    public List<string> IpList => _ipList;
    
    /// List[StreamIndex]
    public List<AVRational> Timebases => _timebases;

    private List<IntPtr> _formatContexts = new();
    private List<string> _ipList = new();
    private List<IntPtr> _codecParametersArray = new();
    private List<AVRational> _timebases = new();
    
    /// <returns>stream index</returns>
    public int AddAvStream(AVCodecParameters* codecParameters, AVRational timebase = new AVRational())
    {
        _codecParametersArray.Add((IntPtr)codecParameters);
        _timebases.Add(timebase);
        return _codecParametersArray.Count - 1;
    }
    
    /// <returns>to do, always true</returns>
    private bool AddAvClient(string ipWithPort)
    {
        string outputUrl = $"rist://{ipWithPort}";
        AVFormatContext* formatContext = null;
        ffmpeg.avformat_alloc_output_context2(&formatContext, null, "mpegts", null);

        for (int i = 0; i < _timebases.Count; i++)
        {
            ffmpeg.avformat_new_stream(formatContext,
                ffmpeg.avcodec_find_encoder( ( (AVCodecParameters*)(_codecParametersArray[i]) )->codec_id));
            
            ffmpeg.avcodec_parameters_copy(formatContext->streams[i]->codecpar, (AVCodecParameters*)(_codecParametersArray[i]));
            formatContext->streams[i]->time_base = _timebases[i];
        }
        ffmpeg.avio_open(&formatContext->pb, outputUrl, 2);
        ffmpeg.avformat_write_header(formatContext, null);
        
        _formatContexts.Add((IntPtr)formatContext);
        _ipList.Add(ipWithPort);
        
        for (int i = 0; i < _timebases.Count; i++)
            _timebases[i] = formatContext->streams[i]->time_base;

        return true;
    }

    public bool AddClient(IPAddress ipAddress, int port = DefaultPort)
    {
        if (port is < 0 or > 65535)
            return false;
        
        if (_formatContexts.Count == 0)
            return false;
        
        return AddAvClient(ipAddress.ToString() + ":" + port);
    }

    private AVFormatContext* AddClientAsFile(string Path)
    {
        AVFormatContext* formatContext = null;
        ffmpeg.avformat_alloc_output_context2(&formatContext, null, null, Path);

        for (int i = 0; i < _timebases.Count; i++)
        {
            AVStream* stream = ffmpeg.avformat_new_stream(formatContext,
                ffmpeg.avcodec_find_encoder( ( (AVCodecParameters*)(_codecParametersArray[i]) )->codec_id));
            
            ffmpeg.avcodec_parameters_copy(formatContext->streams[i]->codecpar, (AVCodecParameters*)(_codecParametersArray[i]));
            formatContext->streams[i]->time_base = _timebases[i];
            //stream->time_base = _timebases[i];
        }
        ffmpeg.avio_open(&formatContext->pb, Path, 2);
        ffmpeg.avformat_write_header(formatContext, null);
        _formatContexts.Add((IntPtr)formatContext);
        
        for (int i = 0; i < _timebases.Count; i++)
            _timebases[i] = formatContext->streams[i]->time_base;
        
        return formatContext;
    }
    
    public void DeleteAllClients()
    {
        foreach (var fc in _formatContexts)
        {
            ffmpeg.av_write_trailer((AVFormatContext*)fc);
            ffmpeg.avio_closep(&(((AVFormatContext*)fc)->pb));
            ffmpeg.avformat_free_context((AVFormatContext*)fc);
        }
        
        _ipList.Clear();
        _formatContexts.Clear();
    }

    public int WriteFrame(AVPacket* packet)
    {
        foreach (var formatContext in _formatContexts)
        {
            int ret = ffmpeg.av_write_frame((AVFormatContext*)formatContext, packet);
            if (ret < 0)
                return ret;
        }

        return 0;
    }
    
    public void Dispose()
    {
        DeleteAllClients();
    }
}