using System.Diagnostics;
using System.Runtime.CompilerServices;
using FFmpeg.AutoGen.Abstractions;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace StreamerWinui;

public class MasterChannel
{
    public const int SampleSizeInBytes = 4;
    public bool PrintDebugInfo;
    
    private Streamer _streamer;
    private AudioEncoder _audioEncoder;
    private AudioBufferSlicer _audioBufferSlicer;
    private unsafe AVFrame* _avFrame;
    private List<WaveInEventArgs> _buffers;
    private List<int> _registeredChannelIds;
    private int _channelIdPointer = 0;
    private List<WasapiAudioChannel> _wasapiAudioChannel;
    private int _frameSizeInBytes;

    public unsafe MasterChannel(Streamer Streamer, Encoders encoder)
    {
        _buffers = new List<WaveInEventArgs>();
        _audioEncoder = new(Streamer, encoder);
        _avFrame = ffmpeg.av_frame_alloc();
        _avFrame->nb_samples = _audioEncoder.FrameSizeInSamples;
        ffmpeg.av_channel_layout_default(&_avFrame->ch_layout, _audioEncoder.Channels);
        _avFrame->format = (int)_audioEncoder.SampleFormat;
        _frameSizeInBytes = _audioEncoder.FrameSizeInSamples * _audioEncoder.Channels * SampleSizeInBytes;
    }

    private void RecieveBuffer(object? sender, AudioBufferSliced audioBufferSliced)
    {
        
    }

    public void AddChannel(MMDevice mmDevice)
    {
        var a = new WasapiAudioChannel(mmDevice, GetNewRegisteredId(), _frameSizeInBytes);
        a.DataAvailable += RecieveBuffer;
        _wasapiAudioChannel.Add(a);
    }

    private int GetNewRegisteredId() => ++_channelIdPointer;
}

public class WasapiAudioChannel
{
    public const float DefaultVolume = 1f;
    
    public int Channels => _wasapiCapture.WaveFormat.Channels;
    public WaveFormat WaveFormat => _wasapiCapture.WaveFormat;
    public MMDevice MMDevice { get; }
    public int ChannelID { get; }
    public float Volume { get; set; } = DefaultVolume;
    public event EventHandler<AudioBufferSliced> DataAvailable;
    private AudioBufferSlicer _audioBufferSlicer;

    private WasapiCapture _wasapiCapture;

    public WasapiAudioChannel(MMDevice mmDevice, int channelID, int frameSizeInBytes)
    {
        if (mmDevice.DataFlow == DataFlow.All)
            throw new Exception("Wrong mmDevice.DataFlow");
        MMDevice = mmDevice;
        ChannelID = channelID;
        _wasapiCapture = (mmDevice.DataFlow == DataFlow.Capture)
            ? new WasapiCapture(mmDevice)
            : new WasapiLoopbackCapture(mmDevice);
        _audioBufferSlicer = new AudioBufferSlicer(frameSizeInBytes, _wasapiCapture.WaveFormat.Channels);
        
        _wasapiCapture.DataAvailable += (s, e) =>
        {
            if (Volume != 1f)
                ApplyVolume(e.Buffer, e.BytesRecorded, Volume);
            DataAvailable.Invoke(this, _audioBufferSlicer.SliceBuffer(e.Buffer, e.BytesRecorded));
        };
    }
    
    private unsafe void ApplyVolume(byte[] buffer, int bufferSize, float volume)
    {
        fixed (byte* p = buffer)
        {
            float* pf = (float*)p;
            for (int i = 0; i < bufferSize/4; i++)
                pf[i] *= volume;
        }
    }

    public void StartRecording() => _wasapiCapture.StartRecording();
    public void StopRecording() => _wasapiCapture.StopRecording();
}