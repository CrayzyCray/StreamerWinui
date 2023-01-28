using System.Net;
using FFmpeg.AutoGen.Abstractions;

namespace StreamerLib;

public unsafe class Streamer : IDisposable
{
    public const int DefaultPort = 10000;

    /// List[StreamIndex]
    public List<StreamParameters> StreamParametersList => _streamParametersList;
    /// List[StreamIndex]
    public List<StreamClient> StreamClientsList => _streamClientsList;
    
    private List<StreamParameters> _streamParametersList = new();
    private List<StreamClient> _streamClientsList = new();
    
    /// <returns>stream index</returns>
    public int AddAvStream(AVCodecParameters* codecParameters, AVRational timebase)
    {
        _streamParametersList.Add(new StreamParameters()
            { CodecParameters = codecParameters, Timebase = timebase });
        
        return _streamParametersList.Count - 1; //this is a StreamIndex
    }

    public bool AddClient(IPAddress ipAddress, int port = DefaultPort)
    {
        if (port is < 0 or > 65535)
            return false;
        
        if (_streamParametersList.Count == 0)
            return false;
        
        string outputUrl = $"rist://{ipAddress.ToString()}:{port}";
        
        AVFormatContext* formatContext = null;
        ffmpeg.avformat_alloc_output_context2(&formatContext, null, "mpegts", null);

        for (int i = 0; i < _streamParametersList.Count; i++)
        {
            ffmpeg.avformat_new_stream(formatContext,ffmpeg.avcodec_find_encoder(_streamParametersList[i].CodecParameters->codec_id));
            
            ffmpeg.avcodec_parameters_copy(formatContext->streams[i]->codecpar, _streamParametersList[i].CodecParameters);
            formatContext->streams[i]->time_base = _streamParametersList[i].Timebase;
        }
        ffmpeg.avio_open(&formatContext->pb, outputUrl, 2);
        ffmpeg.avformat_write_header(formatContext, null);
        
        _streamClientsList.Add(new StreamClient(){FormatContext = formatContext, IP = ipAddress, Port = port});
        
        for (int i = 0; i < _streamParametersList.Count; i++)
            _streamParametersList[i].Timebase = formatContext->streams[i]->time_base;

        return true;
    }

    public bool AddClientAsFile(string Path)
    {
        if (_streamParametersList.Count == 0)
            return false;
        
        string outputUrl = Path;
        
        AVFormatContext* formatContext = null;
        ffmpeg.avformat_alloc_output_context2(&formatContext, null, null, Path);

        for (int i = 0; i < _streamParametersList.Count; i++)
        {
            ffmpeg.avformat_new_stream(formatContext,ffmpeg.avcodec_find_encoder(_streamParametersList[i].CodecParameters->codec_id));
            
            ffmpeg.avcodec_parameters_copy(formatContext->streams[i]->codecpar, _streamParametersList[i].CodecParameters);
            formatContext->streams[i]->time_base = _streamParametersList[i].Timebase;
        }
        ffmpeg.avio_open(&formatContext->pb, outputUrl, 2);
        ffmpeg.avformat_write_header(formatContext, null);
        
        _streamClientsList.Add(new StreamClient(){FormatContext = formatContext, IsFile = true});
        
        for (int i = 0; i < _streamParametersList.Count; i++)
            _streamParametersList[i].Timebase = formatContext->streams[i]->time_base;

        return true;
    }
    
    public void DeleteAllClients()
    {
        foreach (var fc in _streamClientsList)
        {
            ffmpeg.av_write_trailer(fc.FormatContext);
            ffmpeg.avio_closep(&fc.FormatContext->pb);
            ffmpeg.avformat_free_context(fc.FormatContext);
        }
        
        _streamClientsList.Clear();
    }

    public int WriteFrame(AVPacket* Packet, AVRational PacketTimebase)
    {
        ffmpeg.av_packet_rescale_ts(Packet, PacketTimebase, _streamParametersList[Packet->stream_index].Timebase);
        foreach (var streamClient in _streamClientsList)
        {
            int ret = ffmpeg.av_write_frame(streamClient.FormatContext, Packet);
            
            if (ret < 0)
                return ret;
        }

        return 0;
    }
    
    public void Dispose()
    {
        DeleteAllClients();
    }
    
    public class StreamClient
    {
        public IPAddress IP;
        public int Port;
        public AVFormatContext* FormatContext;
        public bool IsFile;
    }
    
    public class StreamParameters
    {
        public AVCodecParameters* CodecParameters;
        public AVRational Timebase;
    }

    private AVRational GetTimebase(AVCodecParameters* codecParameters, AVRational timebase, string format = "mpegts")
    {
        string outputUrl = $"rist://127.0.0.1:10000";
        
        AVFormatContext* formatContext = null;
        ffmpeg.avformat_alloc_output_context2(&formatContext, null, format, null);
        
        ffmpeg.avformat_new_stream(formatContext,ffmpeg.avcodec_find_encoder(codecParameters->codec_id));
        
        ffmpeg.avcodec_parameters_copy(formatContext->streams[0]->codecpar, codecParameters);
        //formatContext->streams[0]->time_base = timebase;
            
        ffmpeg.avio_open(&formatContext->pb, outputUrl, 2);
        ffmpeg.avformat_write_header(formatContext, null);

        AVRational tb = formatContext->streams[0]->time_base;
        
        ffmpeg.avio_closep(&formatContext->pb);
        ffmpeg.avformat_free_context(formatContext);

        return tb;
    }
}