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
#define TRACE_ST_LogToFile 1
#define TRACE_ST_LogToConsole 2
#define TRACE_ST false

#if TRACE_ST == 1
	#include <stdio.h>
	#include <time.h>
	FILE* _logFile;
	const char* path = "C:\\Users\\Cray\\Desktop\\dbg.log";
	bool isFirstLogToFile = true;

	void inline LogToFile(const char* format, ...)
	{
		if (isFirstLogToFile)
		{
			isFirstLogToFile = false;
			if (_logFile == NULL)
			{
				fopen_s(&_logFile, path, "w");
				if (_logFile == NULL)
					return;
				time_t current_time = time(NULL);
				struct tm local_time;
				localtime_s(&local_time, &current_time);
				char str[64];
				asctime_s(&str, sizeof str, &local_time);
				fprintf(_logFile, "File opened\nDate:%s\n\n", str);
			}
		}
	
		va_list ap;
		va_start(ap, format);
		vfprintf(_logFile, format, ap);
	}
#elif TRACE_ST == 2
	#include <stdio.h>
	#include <time.h>
	bool isFirstLogToFile = true;

	void inline LogToFile(const char* format, ...)
	{

		va_list ap;
		va_start(ap, format);
		vprintf(format, ap);
	}
#else
	#define LogToFile(...)
#endif

struct StreamParameters
{
	AVCodecParameters* CodecParameters;
	AVRational* Timebase;
};

DllExport void Here()
{
	LogToFile("\n\nhere%d\n\n", -1);
}

inline void PrintAVError(int errnum)
{
	char str[64];
	av_strerror(errnum, &str, 64);
	LogToFile(str);
	LogToFile("\n");
}

DllExport int __stdcall AudioEncoder_Constructor(
	const char* encoderName,
	int _sampleRate, 
	int channels,
	int* frameSizeInSamples,
	AVCodecContext** codecContextOut,
	AVPacket** packetOut, 
	AVRational** timebaseOut,
	AVCodecParameters** codecParametersOut,
	AVFrame** avFrameOut) 
{
	AVCodec* codec = avcodec_find_encoder_by_name(encoderName);
	AVCodecContext* codecContext = avcodec_alloc_context3(codec);
	codecContext->sample_rate = _sampleRate;
	codecContext->sample_fmt = AV_SAMPLE_FMT_FLT;
	av_channel_layout_default(&codecContext->ch_layout, channels);
	avcodec_open2(codecContext, codec, NULL);

	AVCodecParameters* codecParameters = avcodec_parameters_alloc();
	avcodec_parameters_from_context(codecParameters, codecContext);
	*codecParametersOut = codecParameters;

	*frameSizeInSamples = codecContext->frame_size;
	AVFrame* avFrame = av_frame_alloc();
	avFrame->nb_samples = *frameSizeInSamples;
	av_channel_layout_default(&avFrame->ch_layout, channels);
	avFrame->format = AV_SAMPLE_FMT_FLT;
	
	*codecContextOut = codecContext;
	*packetOut = av_packet_alloc();
	*timebaseOut = &(codecContext->time_base);
	*avFrameOut = avFrame;
	LogToFile("AudioEncoder_Constructor\ntimebase = %d/%d\n", (**timebaseOut).num, (**timebaseOut).den);
}

DllExport bool __stdcall AudioEncoder_EncodeAndWriteFrame(
	const uint8_t* buffer, 
	int frameSizeInBytes,
	int channels,
	int streamIndex,
	__int64 pts,
	AVCodecContext* codecContext,
	AVPacket* packet,
	AVFrame* frame)
{
	LogToFile("\nAudioEncoder_EncodeAndWriteFrame\n");
	int ret;
	av_packet_unref(packet);
	//av_frame_unref(frame);
	//frame->nb_samples = codecContext->frame_size;
	frame->pts = pts;
	//av_channel_layout_default(&frame->ch_layout, channels);
	//LogToFileMy("%d\n", frame->nb_samples);
	
	LogToFile("fill parameters: frame=%p channels=%d buffer=%p frameSize=%d\n", frame, channels, buffer, frameSizeInBytes);
	ret = 0;
	/*AVFrame* testFrame = av_frame_alloc();
	testFrame->nb_samples = 960;
	testFrame->format = codecContext->sample_fmt;
	testFrame->sample_rate = codecContext->sample_rate;
	av_channel_layout_default(&(testFrame->ch_layout), 2);*/
 	ret = avcodec_fill_audio_frame(frame, channels, AV_SAMPLE_FMT_FLT, buffer, frameSizeInBytes, 4 * channels);
	if (ret < 0)
	{
		LogToFile("fill error: ");
		PrintAVError(ret);
		return false;
	}

	ret = avcodec_send_frame(codecContext, frame);
	if (ret < 0)
	{
		LogToFile("send error: ");
		PrintAVError(ret);
		return false;
	}

	LogToFile("frame pts = %d\n", pts);

	ret = avcodec_receive_packet(codecContext, packet);
	if (ret < 0)
	{
		LogToFile("recieve error: ");
		PrintAVError(ret);
		return false;
	}

	if (ret == 0)
	{

		packet->stream_index = streamIndex;
		LogToFile("packet pts = %d\n", packet->pts);
		return true;
	}

	return false;
}

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
	LogToFile("\nStreamWriter_AddClientAsFile\n");
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
		LogToFile("stream%d tb: %d/%d\n", i, formatContext->streams[i]->time_base.num, formatContext->streams[i]->time_base.den);
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
	LogToFile("\nStreamWriter_WriteFrame\nClients: %d\n", formatContextsCount);
	LogToFile("packet pts rescaled from %d/%d to %d/%d\n", packetTimebase->num, packetTimebase->den, streamTimebase->num, streamTimebase->den);
	for (int i = 0; i < formatContextsCount; i++)
	{
		int ret = av_write_frame(formatContexts[i], packet);
		LogToFile("Here2\n");
		if (ret < 0)
		{
			LogToFile("Here3\n");
			return ret;
		}
	}
	LogToFile("Here4\n");
	return 0;
}