﻿StreamerWinui.StreamSession streamSession = new StreamerWinui.StreamSession();
StreamerWinui.StreamSession.errStrPrint(-1313558101);
streamSession.startStream("mpegts");

//-11 Resource temporarily unavailable
//-22 Invalid argument
//-40 Function not implemented
//-1313558101 Unknown error occurred

















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
    //pixFmtConv->inputs->filter_ctx= pixFmtConv->bufferSinkContext;
    //pixFmtConv->inputs->pad_idx = 0;
    //pixFmtConv->inputs->next = null;
    //response = ffmpeg.av_opt_set_bin(pixFmtConv->bufferSinkContext, "pix_fmts", (byte*)&encoder->codecContext->pix_fmt, sizeof(AVPixelFormat), 1<<0);
    //response = ffmpeg.avfilter_graph_parse_ptr(pixFmtConv->filterGraph, "format=rgba", &pixFmtConv->inputs, &pixFmtConv->outputs, null);
    //response = ffmpeg.avfilter_graph_config(pixFmtConv->filterGraph, null);
}