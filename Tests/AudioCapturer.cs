using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFmpeg.AutoGen.Abstractions;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using System.Runtime.InteropServices;

namespace Tests
{
    public unsafe class AudioCapturer
    {
        public void start()
        {
            var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            Directory.CreateDirectory(outputFolder);
            var outputFilePath = Path.Combine(outputFolder, "output.wav");
            var capture = new WasapiLoopbackCapture();
            var writer = new WaveFileWriter(outputFilePath, capture.WaveFormat);
            int i = 0;

            //var outFormat = new WaveFormat()


            capture.DataAvailable += (s, a) =>
            {
                writer.Write(a.Buffer, 0, a.BytesRecorded);
                if (writer.Position > capture.WaveFormat.AverageBytesPerSecond * 5)
                {
                    capture.StopRecording();
                }
                Console.WriteLine(i++);
                Console.WriteLine(a.BytesRecorded);
            };

            capture.RecordingStopped += (s, a) =>
            {
                writer.Dispose();
                writer = null;
                capture.Dispose();
            };

            capture.StartRecording();
            while(capture.CaptureState != CaptureState.Stopped)
                Thread.Sleep(500);
        }

        public unsafe void start1()
        {
            if (DynamicallyLoadedBindings.LibrariesPath == String.Empty)
            {
                DynamicallyLoadedBindings.LibrariesPath = @"C:\Users\Cray\Desktop\Programs\ffmpeg";
                DynamicallyLoadedBindings.Initialize();
            }

            var capture = new WasapiLoopbackCapture();



            AVCodec* codec = ffmpeg.avcodec_find_encoder_by_name("pcm_f32le");
            AVCodecContext* c = ffmpeg.avcodec_alloc_context3(codec);
            c->sample_rate = capture.WaveFormat.SampleRate;
            c->sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_FLT;
            ffmpeg.av_channel_layout_default(&c->ch_layout, 2);
            //c->channel_layout = AV_CHANNEL_LAYOUT_STEREO
            int ret;
            ret = ffmpeg.avcodec_open2(c, codec, null);

            AVFrame* frame = ffmpeg.av_frame_alloc();
            frame->format = (int)c->sample_fmt;

            
            

            capture.DataAvailable += (s, a) =>
            {
                frame->nb_samples = a.BytesRecorded / 4;
                ffmpeg.av_frame_get_buffer(frame, 0);
                //hwFrame->data
                Console.WriteLine(a.BytesRecorded);
                Console.WriteLine(a.Buffer);
            };
        }

        public void start2()
        {
            if (DynamicallyLoadedBindings.LibrariesPath == String.Empty)
            {
                DynamicallyLoadedBindings.LibrariesPath = @"C:\Users\Cray\Desktop\Programs\ffmpeg";
                DynamicallyLoadedBindings.Initialize();
            }

            AVFormatContext* inputFormatContext = ffmpeg.avformat_alloc_context();
            ffmpeg.avformat_open_input(&inputFormatContext, @"C:\Users\Cray\Desktop\output.wav", null, null);
            ffmpeg.avformat_find_stream_info(inputFormatContext, null);
            AVCodec* codec = ffmpeg.avcodec_find_decoder(inputFormatContext->streams[0]->codecpar->codec_id);
            AVCodecContext* codecContext = ffmpeg.avcodec_alloc_context3(codec);
            ffmpeg.avcodec_parameters_to_context(codecContext, inputFormatContext->streams[0]->codecpar);
            ffmpeg.avcodec_open2(codecContext, codec, null);
            AVPacket* packet = ffmpeg.av_packet_alloc();
            ffmpeg.av_read_frame(inputFormatContext, packet);
            ffmpeg.avcodec_send_packet(codecContext, packet);
            AVFrame* frame = ffmpeg.av_frame_alloc();
            ffmpeg.avcodec_receive_frame(codecContext, frame);
            for (int i = 0; i < frame->nb_samples * 4 * 2; i++)
            {
                byte* buf = frame->data[0];
                Console.WriteLine(*(buf + i));
                if (i == 500)
                {
                    int h = 7;
                }
            }
            //ffmpeg.avcodec_fill_audio_frame();
        }
    }
}
