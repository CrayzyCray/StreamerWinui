using System;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using FFmpeg.AutoGen.Abstractions;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NAudio.Wave.SampleProviders;

namespace StreamerLib
{
    public unsafe class AudioRecorder : IDisposable
    {
        public const int SampleSizeInBytes = 4;
        public bool PrintDebugInfo;
        public int Channels => _wasapiCapture.WaveFormat.Channels;
        
        private WasapiCapture _wasapiCapture;
        private int _ret;
        private Streamer _streamer;
        private AudioEncoder _audioEncoder;
        private AudioBufferSlicer _audioBufferSlicer;
        private AVFrame* _avFrame;

        public void Dispose() => StopEncoding();

        public AudioRecorder(Streamer Streamer, MMDevice MMDevice)
        {
            if (MMDevice.DataFlow == DataFlow.Render)
                _wasapiCapture = new WasapiLoopbackCapture(MMDevice);
            else if (MMDevice.DataFlow == DataFlow.Capture)
                _wasapiCapture = new WasapiCapture(MMDevice);
            else
                throw new Exception("Wrong DataFlow");

            _audioEncoder = new(Streamer);
            _audioBufferSlicer = new(_audioEncoder.FrameSizeInSamples, 4, _audioEncoder.Channels);
            _avFrame = ffmpeg.av_frame_alloc();
            _avFrame->nb_samples = _audioEncoder.FrameSizeInSamples;
            ffmpeg.av_channel_layout_default(&_avFrame->ch_layout, _audioEncoder.Channels);
            _avFrame->format = (int)_audioEncoder.SampleFormat;
            
            int _frameSizeInBytes = _audioEncoder.FrameSizeInSamples * _wasapiCapture.WaveFormat.Channels * SampleSizeInBytes;
            
            int framesNumber = 0;
            
            _wasapiCapture.DataAvailable += (s, a) =>
            {
                Debug.WriteLine("\nWasapi Buffer " + a.BytesRecorded);
                
                _audioBufferSlicer.SendBuffer(a.Buffer, a.BytesRecorded);
                
                if (_audioBufferSlicer.BufferIsFull)
                {
                    fixed (byte* buf = _audioBufferSlicer.Buffer)
                        _ret = ffmpeg.avcodec_fill_audio_frame(_avFrame, _audioEncoder.Channels, _audioEncoder.SampleFormat, buf, _frameSizeInBytes, 1);
                    _audioEncoder.EncodeAndWriteFrame(_avFrame);
                    Debug.WriteLine("frame " + framesNumber++ + " writed (buffer)");
                }

                foreach (var i in _audioBufferSlicer.SliceIndexes) 
                {
                    fixed (byte* buf = &a.Buffer[i])
                        ffmpeg.avcodec_fill_audio_frame(_avFrame,
                            _wasapiCapture.WaveFormat.Channels,
                            _audioEncoder.SampleFormat,
                            buf,
                            _frameSizeInBytes,
                            1);
                    _audioEncoder.EncodeAndWriteFrame(_avFrame);
                    Debug.WriteLine("frame " + framesNumber++ + " writed (normal)");
                }
                Debug.WriteLine("Buffered " + _audioBufferSlicer.BufferedCount);
            };
            _wasapiCapture.RecordingStopped += (s, a) => _wasapiCapture.Dispose();
        }
        
        public void StartEncoding() => _wasapiCapture.StartRecording();
        public void StopEncoding() => _wasapiCapture.StopRecording();
    }
}
