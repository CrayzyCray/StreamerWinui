#include <Windows.h>
#include <string.h>
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

DllExport int AudioEncoder_Constructor(
    const char* encoderName,
    int _sampleRate, 
    int channels,
    int* FrameSizeInSamples,
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

    AVFrame* avFrame = av_frame_alloc();
    avFrame->nb_samples = FrameSizeInSamples;
    av_channel_layout_default(&avFrame->ch_layout, channels);
    avFrame->format = AV_SAMPLE_FMT_FLT;

    AVRational timebase = (AVRational){ 1, _sampleRate };
    
    *FrameSizeInSamples = codecContext->frame_size;
    *codecContextOut = codecContext;
    *packetOut = av_packet_alloc();
    *timebaseOut = &timebase;
    *codecParametersOut = codecParameters;
    *avFrameOut = avFrame;
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

DllExport bool AudioEncoder_EncodeAndWriteFrame(
    byte* buffer, 
    int frameSizeInBytes,
    int channels,
    int streamIndex,
    long pts,
    AVCodecContext* codecContext,
    AVPacket* packet,
    AVFrame* frame)
{
    av_packet_unref(packet);
    av_frame_unref(frame);
    avcodec_fill_audio_frame(frame, channels, AV_SAMPLE_FMT_FLT, buffer, frameSizeInBytes, 1);
    frame->pts = pts;
    avcodec_send_frame(codecContext, frame);
    packet->stream_index = streamIndex;

    if (avcodec_receive_packet(codecContext, packet) == 0)
        return true;

    return false;
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
        streamParameters[i].Timebase = &(formatContext->streams[i]->time_base);
}

DllExport int StreamWriter_DeleteAllClients(AVFormatContext* formatContext)
{
    av_write_trailer(formatContext);
    avio_closep(&formatContext);
    avformat_free_context(formatContext);
}

DllExport int StreamWriter_WriteFrame(
    AVPacket* packet, 
    AVRational* packetTimebase, 
    AVRational* streamTimebase, 
    AVFormatContext* formatContexts[])
{
    av_packet_rescale_ts(packet, *packetTimebase, *streamTimebase);
    int length = sizeof(formatContexts) / sizeof(formatContexts[0]);
    for (int i = 0; i < length; i++)
    {
        int ret = av_write_frame(formatContexts[i], packet);
        if (ret < 0)
            return ret;
    }

    return 0;
}

#pragma region testsRegion

struct TestStruct
{
    int A;
    int B;
};

DllExport int TestStructs(struct TestStruct s)
{
    printf("%d %d\n", s.A, s.B);
}

DllExport int Test(AVCodec** codec)
{
    AVCodec* c = avcodec_find_encoder_by_name("libopus");
    *codec = c;
}

DllExport int PrintCodecLongName(AVCodec* codec)
{
    printf(codec->long_name);
}

int Here() { printf("\n\nhere\n\n"); }

DllExport char* GetNameOfEncoder(const char* encoderName)
{
    av_frame_alloc();
    char str[] = "libopus";
    AVCodec* avcodec = avcodec_find_encoder_by_name(encoderName);

    char* ret = avcodec->long_name;
    printf(ret);
    return ret;
}

DllExport AVCodec* AVCodecFindEncoderByName(const char* encoderName)
{
    return avcodec_find_encoder_by_name(encoderName);
}

DllExport AVCodecContext* AvCodecAllocContext3(AVCodec* avCodec)
{
    return avcodec_alloc_context3(avCodec);
}

#pragma endregion testsRegion