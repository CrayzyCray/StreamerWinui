using NAudio.CoreAudioApi;
using NAudio.Wave;
using FFmpeg.AutoGen.Abstractions;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NAudio.Wave.SampleProviders;

namespace StreamerWinui
{
    public unsafe class AudioRecorder : IDisposable
    {
        public const int SampleSizeInBytes = 4;
        public bool PrintDebugInfo;
        public int Channels => _wasapiLoopbackCapture.WaveFormat.Channels;
        
        private WasapiLoopbackCapture _wasapiLoopbackCapture;
        private int _ret;
        private Streamer _streamer;
        private AudioEncoder _audioEncoder;
        private AudioBufferSlicer _audioBufferSlicer;
        private AVFrame* _avFrame;


        public void Dispose() => StopEncoding();

        public AudioRecorder(Streamer Streamer)
        {
            _wasapiLoopbackCapture = new WasapiLoopbackCapture();
            _audioEncoder = new(Streamer);
            _audioBufferSlicer = new(_audioEncoder.FrameSizeInSamples, 4, _audioEncoder.Channels);
            _avFrame = ffmpeg.av_frame_alloc();
            _avFrame->nb_samples = _audioEncoder.FrameSizeInSamples;
            ffmpeg.av_channel_layout_default(&_avFrame->ch_layout, _audioEncoder.Channels);
            _avFrame->format = (int)_audioEncoder.SampleFormat;
            
            int _frameSizeInBytes = _audioEncoder.FrameSizeInSamples * _wasapiLoopbackCapture.WaveFormat.Channels * SampleSizeInBytes;
            List<int> listOfIndexes;
            int framesNumber = 0;
            
            _wasapiLoopbackCapture.DataAvailable += (s, a) =>
            {
                Debug.WriteLine("\nWasapi Buffer " + a.BytesRecorded);
                listOfIndexes = _audioBufferSlicer.SendBuffer(a.Buffer, a.BytesRecorded);
                
                if (_audioBufferSlicer.Buffer.IsFull)
                {
                    fixed (byte* buf = _audioBufferSlicer.Buffer.Buffer)
                        _ret = ffmpeg.avcodec_fill_audio_frame(_avFrame, _audioEncoder.Channels, _audioEncoder.SampleFormat, buf, _frameSizeInBytes, 1);
                    _audioEncoder.EncodeAndWriteFrame(_avFrame);
                    Debug.WriteLine("frame " + framesNumber++ + " writed (buffer)");
                }
                foreach (var i in listOfIndexes)
                {
                    fixed (byte* buf = &a.Buffer[i])
                        _ret = ffmpeg.avcodec_fill_audio_frame(_avFrame, _wasapiLoopbackCapture.WaveFormat.Channels, _audioEncoder.SampleFormat, buf, _frameSizeInBytes, 1);
                    _audioEncoder.EncodeAndWriteFrame(_avFrame);
                    Debug.WriteLine("frame " + framesNumber++ + " writed (normal)");
                }
                Debug.WriteLine("Buffered " + _audioBufferSlicer.Buffer.Count);
            };
            _wasapiLoopbackCapture.RecordingStopped += (s, a) => _wasapiLoopbackCapture.Dispose();
        }
        
        public void StartEncoding() => _wasapiLoopbackCapture.StartRecording();
        public void StopEncoding() => _wasapiLoopbackCapture.StopRecording();
    }

    public class AudioBufferSlicer
    {
        public BufferByte Buffer => _bufferByte;
        
        private BufferByte _bufferByte;
        private BufferByte _bufferByteSecond;
        private BufferByte _bufferByteSwap;
        private int _bytesWritedInBufferMode = 0;
        private int _sliceSizeInBytes;
        private List<int> returnArray = new();
        
        public AudioBufferSlicer(int SliceSizeInSamples, int SampleSizeInBytes, int Channels)
        {
            _bufferByte = new(SliceSizeInSamples * SampleSizeInBytes * Channels);
            _bufferByteSecond = new(SliceSizeInSamples * SampleSizeInBytes * Channels);
            _sliceSizeInBytes = SliceSizeInSamples * SampleSizeInBytes * Channels;
        }
        
        /// <returns>List of indexes in Buffer</returns>
        public List<int> SendBuffer(in byte[] Buffer, int BufferLength)
        {
            returnArray.Clear();
            if (_bufferByte.IsFull)
                _bufferByte.clear();
            
            //swap buffers
            (_bufferByte, _bufferByteSecond) = (_bufferByteSecond, _bufferByte);
            
            if (_bufferByte.NotEmpty)
            {
                _bytesWritedInBufferMode = _bufferByte.SizeRemain;
                for (int i = 0; i < _bytesWritedInBufferMode; i++)
                    _bufferByte.Append(Buffer[i]);
            }
            
            for (int i = _bytesWritedInBufferMode; i + _sliceSizeInBytes <= BufferLength; i += _sliceSizeInBytes)
                returnArray.Add(i);
            
            for (int i = BufferLength - (BufferLength - _bytesWritedInBufferMode) % _sliceSizeInBytes; i < BufferLength; i++)
                _bufferByteSecond.Append(Buffer[i]);
            
            _bytesWritedInBufferMode = 0;
            
            return returnArray;
        }
    }
}
