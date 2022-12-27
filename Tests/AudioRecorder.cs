﻿using NAudio.CoreAudioApi;
using NAudio.Wave;
using FFmpeg.AutoGen.Abstractions;
using System.Diagnostics;
using NAudio.Wave.SampleProviders;

namespace StreamerWinui
{
    public unsafe class AudioRecorder : IDisposable
    {
        public AVCodecParameters* codecParameters;
        public AVRational timebase;
        public WasapiLoopbackCapture _wasapiLoopbackCapture;
        public int StreamIndex;
        
        private int _ret;
        private AVCodecContext* _codecContext;
        private AVFrame* _frame;
        private AVPacket* _packet;
        private AVFormatContext* _outputFormatContext;
        private AVCodec* _codec;
        private BufferByte _buffer;
        private Streamer _streamer;
        //private SwrContext* swrContext;

        public void Dispose()
        {
            StopEncoding();
            
            if (_outputFormatContext != null)
                ffmpeg.avformat_free_context(_outputFormatContext);
            
            if (codecParameters != null)
                fixed(AVCodecParameters** p = &codecParameters)
                    ffmpeg.avcodec_parameters_free(p);
            
            if (_packet != null || _codecContext != null)
            {
                ffmpeg.av_packet_unref(_packet);
                ffmpeg.avcodec_send_packet(_codecContext, _packet);//flush codecContext
            }
            
            if (_codecContext != null)
                fixed (AVCodecContext** p = &_codecContext)
                    ffmpeg.avcodec_free_context(p);
            
            if (_packet != null)
                fixed(AVPacket** p = &_packet)
                    ffmpeg.av_packet_free(p);
            
            if (_frame != null)
                fixed(AVFrame** p = &_frame)
                    ffmpeg.av_frame_free(p);
        }

        public AudioRecorder(Streamer streamer)
        {
            _wasapiLoopbackCapture = new WasapiLoopbackCapture();
            _streamer = streamer;

            _codec = ffmpeg.avcodec_find_encoder_by_name("libopus");
            _codecContext = ffmpeg.avcodec_alloc_context3(_codec);
            _codecContext->sample_rate = 48000;
            _codecContext->sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_FLT;
            ffmpeg.av_channel_layout_default(&_codecContext->ch_layout, 2);
            _ret = ffmpeg.avcodec_open2(_codecContext, _codec, null);

            _frame = ffmpeg.av_frame_alloc();
            _frame->nb_samples = _codecContext->frame_size;
            ffmpeg.av_channel_layout_default(&_frame->ch_layout, 2);
            _frame->format = (int)_codecContext->sample_fmt;
            _packet = ffmpeg.av_packet_alloc();

            codecParameters = ffmpeg.avcodec_parameters_alloc();
            ffmpeg.avcodec_parameters_from_context(codecParameters, _codecContext);
            timebase.num = 1;
            timebase.den = _codecContext->sample_rate;

            long pts = 0;

            // swrContext = ffmpeg.swr_alloc();
            // AVChannelLayout channelLayoutIn = new AVChannelLayout();
            // AVChannelLayout channelLayoutStereo = new AVChannelLayout();
            // ffmpeg.av_channel_layout_default(&channelLayoutStereo, 2);
            // ffmpeg.av_channel_layout_default(&channelLayoutIn, wasapiLoopbackCapture.WaveFormat.Channels);
            //
            // ffmpeg.av_opt_set_chlayout(swrContext, "in_chlayout", &channelLayoutIn, 0);
            // ffmpeg.av_opt_set_int(swrContext, "in_sample_rate", wasapiLoopbackCapture.WaveFormat.SampleRate, 0);
            // ffmpeg.av_opt_set_sample_fmt(swrContext, "in_sample_fmt", AVSampleFormat.AV_SAMPLE_FMT_FLT, 0);
            //
            // ffmpeg.av_opt_set_chlayout(swrContext, "out_chlayout", &channelLayoutStereo, 0);
            // ffmpeg.av_opt_set_int(swrContext, "out_sample_rate", 48000, 0);
            // ffmpeg.av_opt_set_sample_fmt(swrContext, "out_sample_fmt", AVSampleFormat.AV_SAMPLE_FMT_FLT, 0);
            // ffmpeg.swr_init(swrContext);

            //byte[] bufferOut = new byte[_wasapiLoopbackCapture.WaveFormat.AverageBytesPerSecond];

            //int outCount = _wasapiLoopbackCapture.WaveFormat.AverageBytesPerSecond /
            //                _wasapiLoopbackCapture.WaveFormat.Channels / 4; //4 is size of sample in bytes
            //int inCount;
            
            int _frameSizeInBytes = _codecContext->frame_size * _wasapiLoopbackCapture.WaveFormat.Channels * 4;
            int bytesWrited = 0; //указывает на сэмпл, который не записан
            _buffer = new BufferByte(_frameSizeInBytes);
            _wasapiLoopbackCapture.DataAvailable += (s, a) =>
            {
                int number = 0;
                Debug.WriteLine("\nrecord buffer size " + a.BytesRecorded/8);
                if (_wasapiLoopbackCapture.CaptureState == CaptureState.Stopped)
                    return;
                
                if (_buffer.NotEmpty)
                {
                    bytesWrited = _buffer.SizeRemain;
                    for (int i = 0; i < bytesWrited; i++)
                        _buffer.Append(a.Buffer[i]);
                    fixed (byte* buf = _buffer.Buffer)
                        _ret = ffmpeg.avcodec_fill_audio_frame(_frame, _wasapiLoopbackCapture.WaveFormat.Channels, _codecContext->sample_fmt, buf, _frameSizeInBytes, 1);
                    _frame->pts = pts;
                    _ret = ffmpeg.avcodec_send_frame(_codecContext, _frame);
                    _ret = ffmpeg.avcodec_receive_packet(_codecContext, _packet);
                    //_packet->pts = pts;
                    //_packet->dts = pts;
                    _packet->stream_index = StreamIndex;
                    //ffmpeg.av_packet_rescale_ts(_packet, );
                    Debug.WriteLine("w: " + _codecContext->frame_number + " pts: " + _packet->pts);
                    _ret = _streamer.WriteFrame(_packet);
                    //Debug.WriteLine("av_write_frame " + _ret);
                    pts += _codecContext->frame_size;
                    Debug.WriteLine("writed in buffer mode " + _frameSizeInBytes/8);
                    _buffer.clear();
                }
                
                
                for (int i = bytesWrited; i + _frameSizeInBytes <= a.BytesRecorded; i += _frameSizeInBytes)
                {
                    fixed (byte* buf = &a.Buffer[i])
                        _ret = ffmpeg.avcodec_fill_audio_frame(_frame, _wasapiLoopbackCapture.WaveFormat.Channels, _codecContext->sample_fmt, buf, _frameSizeInBytes, 1);
                    //Debug.WriteLine("avcodec_fill_audio_frame " + _ret);
                    _frame->pts = pts;
                    _ret = ffmpeg.avcodec_send_frame(_codecContext, _frame);
                    //Debug.WriteLine("avcodec_send_frame " + _ret);
                    _ret = ffmpeg.avcodec_receive_packet(_codecContext, _packet);
                    //Debug.WriteLine("avcodec_receive_packet " + _ret);
                    //_packet->pts = pts;
                    //_packet->dts = pts;
                    _packet->stream_index = StreamIndex;
                    Debug.WriteLine("w: " + _codecContext->frame_number + " pts: " + _packet->pts);
                    _ret = _streamer.WriteFrame(_packet);
                    //Debug.WriteLine("av_write_frame " + _ret);
                    pts += _codecContext->frame_size;

                    number += _frameSizeInBytes/8;
                }
                Debug.WriteLine("writed in normal mode " + number);

                
                
                
                // for (int i = a.BytesRecorded - a.BytesRecorded % _frameSizeInBytes + bytesWrited; i < a.BytesRecorded; i++)
                //     _buffer.Append(a.Buffer[i]);
                for (int i = a.BytesRecorded - (a.BytesRecorded-bytesWrited) % _frameSizeInBytes; i < a.BytesRecorded; i++)
                    _buffer.Append(a.Buffer[i]);
                
                bytesWrited = 0;
                Debug.WriteLine("buffered " + _buffer.Count/8);
                //inCount = a.BytesRecorded / _wasapiLoopbackCapture.WaveFormat.Channels / 4; //4 is size of sample in bytes
                //fixed(byte* buf = bufferOut)
                //    fixed(byte* inBuf = a.Buffer)
                //ffmpeg.swr_convert(swrContext, &buf, outCount, &inBuf, inCount);
                //_frame->nb_samples = a.BytesRecorded / (2 * 4);
                //int audioBufferSize = ffmpeg.av_samples_get_buffer_size(null, wasapiLoopbackCapture.WaveFormat.Channels, _frame->nb_samples, _codecContext->sample_fmt, 1);
            };

            _wasapiLoopbackCapture.RecordingStopped += (s, a) =>
            {
                _wasapiLoopbackCapture.Dispose();
                RecordingStopped();
            };
        }
        
        public void StartEncoding()
        {
            _wasapiLoopbackCapture.StartRecording();
        }

        public void StopEncoding()
        {
            _wasapiLoopbackCapture.StopRecording();
        }

        public delegate void Evnt();

        public event Evnt RecordingStopped;

        /// <summary>
        /// record with standart writer
        /// </summary>
        public static void StartWithNaudioWaveWriter()
        {
            var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            Directory.CreateDirectory(outputFolder);
            var outputFilePath = Path.Combine(outputFolder, "output1.wav");
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
