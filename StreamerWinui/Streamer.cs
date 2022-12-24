using System.Collections.Generic;
using FFmpeg.AutoGen.Abstractions;

namespace StreamerWinui;

public unsafe class Streamer
{
    public static List<nint> formatContexts = new List<nint>();
    public static AVFormatContext* AddStream(string ipWithPort, AVCodecParameters*[] codecParametersArray, ref AVRational[] timebases)
    {
        string outputUrl = $"rist://{ipWithPort}";
        AVFormatContext* formatContext = null;
        ffmpeg.avformat_alloc_output_context2(&formatContext, null, "mpegts", null);

        for (int i = 0; i < timebases.Length; i++)
        {
            AVStream* stream = ffmpeg.avformat_new_stream(formatContext,
                ffmpeg.avcodec_find_encoder(codecParametersArray[i]->codec_id));
            ffmpeg.avcodec_parameters_copy(stream->codecpar, codecParametersArray[i]);
            stream->time_base = timebases[i];
        }
        
        ffmpeg.avio_open(&formatContext->pb, outputUrl, 2);
        ffmpeg.avformat_write_header(formatContext, null);
        for (int i = 0; i < timebases.Length; i++)
        {
            timebases[i] = formatContext->streams[i]->time_base;
        }
        formatContexts.Add((nint)formatContext);
        
        return formatContext;
    }

    public static int WriteFrame(AVPacket* packet)
    {
        foreach (var formatContext in formatContexts)
        {
            int ret = ffmpeg.av_write_frame((AVFormatContext*)formatContext, packet);
            if (ret < 0)
                return ret;
        }

        return 0;
    }
}