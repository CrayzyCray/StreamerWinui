using NAudio.CoreAudioApi;

namespace StreamerLib;

public sealed class MasterChannel : IDisposable
{
    public const int SampleSizeInBytes = 4;
    public StreamWriter StreamWriter { get; }
    public Codecs Codec { get; }
    public MasterChannelStates State { get; private set; } = MasterChannelStates.Stopped;
    public int DevicesCount => _audioChannels.Count;

    private AudioEncoder _audioEncoder;
    private List<WasapiAudioCapturingChannel> _audioChannels = new(2);
    private byte[] _masterBuffer;
    private Thread? _mixerThread;
    private ManualResetEvent _manualResetEvent = new(false);
    private CancellationTokenSource _cancellationTokenSource = new();

    public MasterChannel(StreamWriter streamWriter, Codecs codec)
    {
        this.StreamWriter = streamWriter;
        this.Codec = codec;
        _audioEncoder = new(StreamWriter, codec);
        _masterBuffer = new byte[_audioEncoder.FrameSizeInBytes];
    }


    public WasapiAudioCapturingChannel? AddChannel(MMDevice device)
    {
        if (State == MasterChannelStates.Streaming)
            return null;
        
        var channel = new WasapiAudioCapturingChannel(device, _audioEncoder.FrameSizeInBytes);

        if (channel.WaveFormat.SampleRate != _audioEncoder.SampleRate)
            throw new Exception("sample rate must be 48000");

        channel.DataAvailable += ReceiveBuffer;
        
        if (State == MasterChannelStates.Monitoring)
            channel.StartRecording();
        
        _audioChannels.Add(channel);
        
        return channel;
    }

    public void RemoveChannel(WasapiAudioCapturingChannel capturingChannel)
    {
        if (!_audioChannels.Contains(capturingChannel))
            throw new ArgumentException("MasterChannel not contains this WASAPIAudioCapturingChannel");

        capturingChannel.DataAvailable -= ReceiveBuffer;
        _audioChannels.Remove(capturingChannel);
    }

    internal void StartStreaming()
    {
        if (State == MasterChannelStates.Streaming)
            return;
        if (_audioChannels.Count == 0)
            throw new Exception("no audio devices");
        
        if (State == MasterChannelStates.Stopped)
        {
            foreach (var channel in _audioChannels)
                if (channel.CaptureState == CaptureState.Stopped)
                    channel.StartRecording();
        }

        _audioEncoder.RegisterAVStream(StreamWriter);

        _cancellationTokenSource = new();
        _mixerThread = new Thread(MixingMethod);
        _mixerThread.Start();
        
        State = MasterChannelStates.Streaming;
    }

    public void StartMonitoring()
    {
        if (State != MasterChannelStates.Stopped)
            return;
        
        foreach (var channel in _audioChannels)
            if (channel.CaptureState == CaptureState.Stopped)
                channel.StartRecording();
        State = MasterChannelStates.Monitoring;
    }
    
    internal void StopStreaming()
    {
        State = MasterChannelStates.Monitoring;
        _cancellationTokenSource.Cancel();
        _manualResetEvent.Set();
        _mixerThread?.Join();
        _audioEncoder.ResetPacketTimeStamp();
    }

    public void Stop()
    {
        if (State == MasterChannelStates.Streaming)
            StopStreaming();
        
        foreach (var channel in _audioChannels)
            if (channel.CaptureState != CaptureState.Stopped)
                channel.StopRecording();
        
    }

    private void ReceiveBuffer(object? sender, EventArgs e)
    {
        if (State != MasterChannelStates.Streaming)
            return;

        _manualResetEvent.Set();
    }

    private void MixingMethod()
    {
        var cancellationToken = _cancellationTokenSource.Token;

        while (true)
        {
            _manualResetEvent.WaitOne();

            while (AllBuffersIsAvailable())
                Mix();
            
            _manualResetEvent.Reset();
            if (cancellationToken.IsCancellationRequested)
                return;
        }

        unsafe void Mix()
        {
            _masterBuffer = _audioChannels[0].ReadNextBuffer();

            fixed (byte* ptr1 = _masterBuffer)
            {
                float* masterBufferFloat = (float*)ptr1;

                for (int i = 1; i < _audioChannels.Count; i++)
                {
                    var buffer = _audioChannels[i].ReadNextBuffer();

                    fixed (byte* ptr2 = buffer)
                    {
                        float* bufferFloat = (float*)ptr2;
                        SumBuffers(masterBufferFloat, bufferFloat, _masterBuffer.Length / 4);
                    }
                }

                ApplyClipping(masterBufferFloat, _masterBuffer.Length / 4);

                _audioEncoder.EncodeAndWriteFrame(_masterBuffer);
            }

            void SumBuffers(float* destonation, float* source, int length)
            {
                for (int i = 0; i < length; i++)
                    destonation[i] += source[i];
            }

            void ApplyClipping(float* buffer, int length)
            {
                for (int i = 0; i < length; i++)
                {
                    if (buffer[i] > 1f)
                        buffer[i] = 1f;
                    else if (buffer[i] < -1f)
                        buffer[i] = -1f;
                }
            }
        }
    }

    private bool AllBuffersIsAvailable()
    {
        foreach (var channel in _audioChannels)
            if (channel.BufferIsAvailable == false) 
                return false;
        return true;
    }

    public void Dispose()
    {
        Stop();
    }
}

public enum MasterChannelStates
{
    Stopped,
    Monitoring,
    Streaming
}