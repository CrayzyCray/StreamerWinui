#include <libavutil/frame.h>
#include <libavcodec/avcodec.h>
#include <Windows.h>
#include <string.h>
#define DllExport __declspec(dllexport)
#pragma comment(lib, "avutil.lib")
#pragma comment(lib, "avcodec.lib")
#pragma comment(lib, "avformat.lib")
#pragma comment(lib, "avfilter.lib")

DllExport char* GetNameOfEncoder() {
	av_frame_alloc();
	char str[] = "libopus";
	AVCodec* avcodec = avcodec_find_encoder_by_name(str);
	
	printf(avcodec->name);
	return avcodec->name;
}

DllExport int get_int_val() {
	return 4712;
}

DllExport AVCodec* AVCodecFindEncoderByName(const char* encoderName )
{
	return avcodec_find_encoder_by_name(encoderName);
}