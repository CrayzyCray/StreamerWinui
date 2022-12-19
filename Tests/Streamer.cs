using FFmpeg.AutoGen.Abstractions;

namespace StreamerWinui;

public unsafe class Streamer
{
    public static List<nint> _formatContexts = new List<nint>();
    public static AVFormatContext* AddStream(string ipWithPort, AVCodecParameters*[] codecParametersArray, AVRational[] timebases)
    {
        string outputUrl = $"rist://{ipWithPort}";
        AVFormatContext* formatContext = null;
        ffmpeg.avformat_alloc_output_context2(&formatContext, null, "mpegts", null);

        for (int i = 0; i < timebases.Length; i++)
        {
            ffmpeg.avformat_new_stream(formatContext, ffmpeg.avcodec_find_encoder(codecParametersArray[i]->codec_id));
            ffmpeg.avcodec_parameters_copy(formatContext->streams[i]->codecpar, codecParametersArray[i]);
            formatContext->streams[i]->time_base = timebases[i];
        }
        
        ffmpeg.avio_open(&formatContext->pb, outputUrl, 2);
        ffmpeg.avformat_write_header(formatContext, null);
        _formatContexts.Add((nint)formatContext);
        return formatContext;
    }

    public static int WriteFrame(AVPacket* packet)
    {
        foreach (var formatContext in _formatContexts)
        {
            int ret = ffmpeg.av_write_frame((AVFormatContext*)formatContext, packet);
            if (ret < 0)
                return ret;
        }

        return 0;
    }
}