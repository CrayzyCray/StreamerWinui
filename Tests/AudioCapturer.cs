using NAudio.CoreAudioApi;
using NAudio.Wave;
using FFmpeg.AutoGen.Abstractions;
using System.Diagnostics;

namespace StreamerWinui
{
    public unsafe class AudioRecorder
    {
        private int _ret;
        private AVCodecContext* _codecContext;
        private AVFrame* _frame;
        private AVPacket* _packet;
        private AVFormatContext* _outputFormatContext;
        private AVCodec* _codec;

        public void Start1(int timeInSeconds)
        {
            var wasapiLoopbackCapture = new WasapiLoopbackCapture();

            _codec = ffmpeg.avcodec_find_encoder_by_name("pcm_f32le");
            _codecContext = ffmpeg.avcodec_alloc_context3(_codec);
            _codecContext->sample_rate = wasapiLoopbackCapture.WaveFormat.SampleRate;
            _codecContext->sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_FLT;
            ffmpeg.av_channel_layout_default(&_codecContext->ch_layout, 2);
            _ret = ffmpeg.avcodec_open2(_codecContext, _codec, null);

            _frame = ffmpeg.av_frame_alloc();
            _frame->nb_samples = 512;
            ffmpeg.av_channel_layout_default(&_frame->ch_layout, 2);
            _frame->format = (int)_codecContext->sample_fmt;
            _packet = ffmpeg.av_packet_alloc();

            _outputFormatContext = ffmpeg.avformat_alloc_context();
            fixed(AVFormatContext** ptr = &_outputFormatContext)
                ffmpeg.avformat_alloc_output_context2(ptr, null, null, @"C:\\Users\\Cray\\Desktop\\output.wav");
            ffmpeg.avformat_new_stream(_outputFormatContext, _codec);
            ffmpeg.avcodec_parameters_from_context(_outputFormatContext->streams[0]->codecpar, _codecContext);
            _outputFormatContext->streams[0]->time_base.num = 1;
            _outputFormatContext->streams[0]->time_base.den = wasapiLoopbackCapture.WaveFormat.SampleRate;
            ffmpeg.avio_open(&_outputFormatContext->pb, @"C:\\Users\\Cray\\Desktop\\output.wav", 2);
            ffmpeg.avformat_write_header(_outputFormatContext, null);

            Stopwatch sw = new Stopwatch();
            long pts = 0;
            
            wasapiLoopbackCapture.DataAvailable += (s, a) =>
            {
                _frame->nb_samples = a.BytesRecorded / (2 * 4);
                //int audioBufferSize = ffmpeg.av_samples_get_buffer_size(null, wasapiLoopbackCapture.WaveFormat.Channels, _frame->nb_samples, _codecContext->sample_fmt, 1);
                fixed (byte* buf = a.Buffer)
                    _ret = ffmpeg.avcodec_fill_audio_frame(_frame, 2, _codecContext->sample_fmt, buf, a.BytesRecorded, 1);
                Debug.WriteLine("avcodec_fill_audio_frame " + _ret);
                _ret = ffmpeg.avcodec_send_frame(_codecContext, _frame);
                Debug.WriteLine("avcodec_send_frame " + _ret);
                _ret = ffmpeg.avcodec_receive_packet(_codecContext, _packet);
                Debug.WriteLine("avcodec_receive_packet " + _ret);
                _packet->pts = pts;
                _packet->dts = pts;
                _ret = ffmpeg.av_write_frame(_outputFormatContext, _packet);
                Debug.WriteLine("av_write_frame " + _ret);
                pts += a.BytesRecorded;
            };

            wasapiLoopbackCapture.RecordingStopped += (s, a) =>
            {
                wasapiLoopbackCapture.Dispose();
            };

            wasapiLoopbackCapture.StartRecording();
            sw.Start();
            Thread.Sleep(timeInSeconds * 1000);
            Debug.WriteLine(pts + " samples");
            Debug.WriteLine(sw.ElapsedMilliseconds + " ms");
            wasapiLoopbackCapture.StopRecording();
            ffmpeg.av_write_trailer(_outputFormatContext);
            ffmpeg.avio_close(_outputFormatContext->pb);
        }

        /// <summary>
        /// record with standart writer
        /// </summary>
        public void Start3()
        {
            var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            Directory.CreateDirectory(outputFolder);
            var outputFilePath = Path.Combine(outputFolder, "output.wav");
            var capture = new WasapiLoopbackCapture();
            var writer = new WaveFileWriter(outputFilePath, capture.WaveFormat);
            int i = 0;

            capture.DataAvailable += (s, a) =>
            {
                writer.Write(a.Buffer, 0, a.BytesRecorded);
                if (writer.Position > capture.WaveFormat.AverageBytesPerSecond * 5)
                    capture.StopRecording();
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
            while (capture.CaptureState != CaptureState.Stopped)
                Thread.Sleep(500);
        }
    }
}
