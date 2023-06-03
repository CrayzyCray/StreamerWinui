#include <Windows.h>
#include <stdbool.h>
#include <libavutil/frame.h>
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>

#pragma comment(lib, "avutil.lib")
#pragma comment(lib, "avcodec.lib")
#pragma comment(lib, "avformat.lib")
#pragma comment(lib, "avfilter.lib")

#define DllExport __declspec(dllexport)

struct StreamParameters
{
	AVCodecParameters* CodecParameters;
	AVRational* Timebase;
};

DllExport int AudioEncoder_Dispose(
	AVPacket* packet,
	AVFrame* frame,
	AVCodecContext* codecContext,
	AVCodecParameters* codecParameters,
	AVRational* timebase)
{
	av_packet_unref(packet);
	avcodec_send_packet(codecContext, packet);//flush codecContext
	avcodec_free_context(&codecContext);
	av_packet_free(&packet);
	av_frame_free(&frame);
	avcodec_parameters_free(&codecParameters);
}

DllExport int StreamWriter_AddClient(
	const char* outputUrl,
	AVFormatContext** formatContextOut,
	struct StreamParameters* streamParameters,
	int streamParametersLength)
{
	AVFormatContext* formatContext;
	avformat_alloc_output_context2(&formatContext, NULL, "mpegts", NULL);
	for (int i = 0; i < streamParametersLength; i++)
	{
		avformat_new_stream(
			formatContext, 
			avcodec_find_encoder(streamParameters[i].CodecParameters->codec_id));
		avcodec_parameters_copy(
			formatContext->streams[i]->codecpar, 
			streamParameters[i].CodecParameters);
		formatContext->streams[i]->time_base = *(streamParameters[i].Timebase);
	}

	avio_open(&formatContext->pb, outputUrl, AVIO_FLAG_WRITE);
	avformat_write_header(formatContext, NULL);
	*formatContextOut = formatContext;

	for (int i = 0; i < streamParametersLength; i++)
		streamParameters[i].Timebase = &(formatContext->streams[i]->time_base);
}

DllExport int StreamWriter_AddClientAsFile(
	const char* path,
	AVFormatContext** formatContextOut,
	struct StreamParameters* streamParameters,
	int streamParametersLength)
{
	AVFormatContext* formatContext;
	avformat_alloc_output_context2(&formatContext, NULL, NULL, path);
	for (int i = 0; i < streamParametersLength; i++)
	{
		avformat_new_stream(
			formatContext,
			avcodec_find_encoder(streamParameters[i].CodecParameters->codec_id));
		avcodec_parameters_copy(
			formatContext->streams[i]->codecpar,
			streamParameters[i].CodecParameters);
		formatContext->streams[i]->time_base = *(streamParameters[i].Timebase);
	}

	avio_open(&formatContext->pb, path, AVIO_FLAG_WRITE);
	avformat_write_header(formatContext, NULL);
	*formatContextOut = formatContext;

	for (int i = 0; i < streamParametersLength; i++)
	{
		streamParameters[i].Timebase = &(formatContext->streams[i]->time_base);
	}
}

DllExport int StreamWriter_CloseFormatContext(AVFormatContext* formatContext)
{
	av_write_trailer(formatContext);
	avio_closep(&formatContext->pb);
	avformat_free_context(formatContext);
}

DllExport int StreamWriter_WriteFrame(
	AVPacket* packet, 
	AVRational* packetTimebase,
	AVRational* streamTimebase,
	AVFormatContext *formatContexts[],
	int formatContextsCount)
{
	av_packet_rescale_ts(packet, *packetTimebase, *streamTimebase);
	for (int i = 0; i < formatContextsCount; i++)
	{
		int ret = av_write_frame(formatContexts[i], packet);
		if (ret < 0)
		{
			return ret;
		}
	}
	return 0;
}