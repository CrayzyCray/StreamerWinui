using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using System.Diagnostics;

namespace StreamerLib;

public class MasterChannel : IDisposable
{
    public const int SampleSizeInBytes = 4;

    public List<WasapiAudioCapturingChannel> AudioChannels => _audioChannels;
    public int FrameSizeInBytes => _audioEncoder.FrameSizeInBytes;
    public StreamWriter StreamWriter { get; }
    public Encoders Encoder { get; }
    public MasterChannelStates State { get; private set; } = MasterChannelStates.Monitoring;

    private AudioEncoder _audioEncoder;
    private List<WasapiAudioCapturingChannel> _audioChannels = new(2);
    private byte[] _masterBuffer;
    private Thread _mixerThread;
    private object _lockObj = new();

    public MasterChannel(StreamWriter streamWriter, Encoders encoder)
    {
        _mixerThread = new Thread(MixingMethod);
        StreamWriter = streamWriter;
        Encoder = encoder;
        _audioEncoder = new(streamWriter, encoder);
        _masterBuffer = new byte[_audioEncoder.FrameSizeInBytes];
    }

    public WasapiAudioCapturingChannel? AddChannel(MMDevice device)
    {
        if (State == MasterChannelStates.Streaming)
            return null;
        var channel = new WasapiAudioCapturingChannel(device, _audioEncoder.FrameSizeInBytes);
        _audioChannels.Add(channel);
        channel.DataAvailable += RecieveBuffer;
        return channel;
    }

    public void RemoveChannel(WasapiAudioCapturingChannel capturingChannel)
    {
        if (!_audioChannels.Contains(capturingChannel))
            throw new ArgumentException("MasterChannel not contains this WasapiAudioCapturingChannel");

        capturingChannel.DataAvailable -= RecieveBuffer;
        _audioChannels.Remove(capturingChannel);
    }

    public void DeleteAllChannels()
    {
        Dispose();
        _audioChannels.Clear();
    }

    internal void StartStreaming()
    {
        foreach (var channel in _audioChannels)
            if (channel.CaptureState == CaptureState.Stopped)
                channel.StartRecording();
        State = MasterChannelStates.Streaming;
    }

    private void RecieveBuffer(object? sender, EventArgs e)
    {
        LoggingHelper.LogToCon($"ReceveBuffer event {(sender as WasapiAudioCapturingChannel).DeviceFriendlyName}");

        if (State != MasterChannelStates.Streaming)
        {
            LoggingHelper.LogToCon("state is not streaming");
            return;
        }

        if (Monitor.IsEntered(_lockObj))
        {
            LoggingHelper.LogToCon("tryEnter is false, MixingMethod is skipped");
        }
        else
        {
            LoggingHelper.LogToCon("tryEnter is true, starting MixingMethod");
            MixingMethod();
        }
        //if (_mixerThread.IsAlive)
        //    return;

        //_mixerThread = new Thread(MixingMethod);
        //_mixerThread.Start();
    }

    

    private unsafe void MixingMethod()
    {
        Monitor.Enter(_lockObj);

        //LoggingHelper.LogToCon("Devices list:");
        //foreach (var channel in _audioChannels)
        //    LoggingHelper.LogToCon($"    name: {channel.MMDevice.FriendlyName} buffers: {channel.Buffers.Count}");

        while (AllBuffersIsAvalibel())
        {
            LoggingHelper.LogToCon("All devices have avalible buffer");
            LoggingHelper.LogToCon($"Devices count: {_audioChannels.Count} list:");
            foreach (var channel in _audioChannels)
                LoggingHelper.LogToCon($"    name: {channel.MMDevice.FriendlyName} buffers: {channel.Buffers.Count}");
            _audioChannels[0].ReadNextBuffer().CopyTo(_masterBuffer, 0);
            fixed (byte* ptr1 = _masterBuffer)
            {
                float* masterBufferFloat = (float*)ptr1;

                for (int i = 1; i < _audioChannels.Count; i++)
                {
                    var buffer = _audioChannels[i].ReadNextBuffer();

                    fixed (byte* ptr2 = &buffer.Array[buffer.Offset])
                    {
                        float* bufferFloat = (float*)ptr2;
                        for (int j = 0; j < _masterBuffer.Length; j++)
                        {
                            masterBufferFloat[j] += bufferFloat[j];
                        }
                    }
                }

                //clipping
                for (int j = 0; j < _masterBuffer.Length; j++)
                {
                    if (masterBufferFloat[j] > 1f)
                        masterBufferFloat[j] = 1f;
                    else if (masterBufferFloat[j] < -1f)
                        masterBufferFloat[j] = -1f;
                }
                _audioEncoder.EncodeAndWriteFrame(_masterBuffer);
            }
        }
        Monitor.Exit(_lockObj);
        LoggingHelper.LogToCon("Mixer method finished");
    }

    private bool AllBuffersIsAvalibel()
    {
        for (int i = 0; i < _audioChannels.Count; i++)
            if (!_audioChannels[i].BufferIsAvailable)
                return false;
        return true;
    }

    public void Dispose()
    {
        _audioChannels.ForEach(c => c.StopRecording());
    }
}

public enum MasterChannelStates
{
    Monitoring,
    Streaming
}