using NAudio.CoreAudioApi;

namespace StreamerLib;

public class MasterChannel : IDisposable
{
    public const int SampleSizeInBytes = 4;

    //public List<WasapiAudioCapturingChannel> AudioChannels => _audioChannels;
    public int FrameSizeInBytes => _audioEncoder.FrameSizeInBytes;
    public StreamWriter StreamWriter { get; }
    public Encoders Encoder { get; }
    public MasterChannelStates State { get; private set; } = MasterChannelStates.Monitoring;

    private AudioEncoder _audioEncoder;
    private List<WasapiAudioCapturingChannel> _audioChannels = new(2);
    private byte[] _masterBuffer;
    private Task _mixerThread;
    private ManualResetEvent _manualResetEvent = new(false);

    public MasterChannel(StreamWriter streamWriter, Encoders encoder)
    {
        _mixerThread = new Task(MixingMethodTest);
        _mixerThread.Start();
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
        channel.DataAvailable += ReceiveBuffer;
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
        foreach (var channel in _audioChannels)
            if (channel.CaptureState == CaptureState.Stopped)
                channel.StartRecording();
        State = MasterChannelStates.Streaming;
    }
    
    internal void StopStreaming()
    {
        State = MasterChannelStates.Monitoring;
    }

    private void ReceiveBuffer(object? sender, EventArgs e)
    {
        if (State != MasterChannelStates.Streaming)
        {
            return;
        }
        
        Console.WriteLine($"Set. Queue elements count:{_audioChannels[0].Queue.Count}");
        _manualResetEvent.Set();
    }

    int c = 0;

    private unsafe void MixingMethodTest()
    {
        while (true)
        {
            _manualResetEvent.WaitOne();
            Console.Write($"WaitOne ");

            while (AllBuffersIsAvailable())
                Mix();
            _manualResetEvent.Reset();
            Console.WriteLine("Reset");
        }

        void Mix()
        {
            c++;
            var readBuffer = _audioChannels[0].ReadNextBuffer();
            if (readBuffer.Count != _masterBuffer.Length)
                throw new Exception();
            Array.Copy(readBuffer.Array, readBuffer.Offset, _masterBuffer, 0, _masterBuffer.Length);

            fixed (byte* ptr1 = _masterBuffer)
            {
                float* masterBufferFloat = (float*)ptr1;

                for (int i = 1; i < _audioChannels.Count; i++)
                {
                    var buffer = _audioChannels[i].ReadNextBuffer();

                    fixed (byte* ptr2 = &buffer.Array[buffer.Offset])
                    {
                        float* bufferFloat = (float*)ptr2;
                        AddBuffer(masterBufferFloat, bufferFloat, _masterBuffer.Length);
                    }
                }

                ClipBuffer(masterBufferFloat, _masterBuffer.Length / 4);

                _audioEncoder.EncodeAndWriteFrame(_masterBuffer);
            }

            void AddBuffer(float* destonation, float* source, int length)
            {
                for (int i = 0; i < length; i++)
                {
                    destonation[i] += source[i];
                }
            }

            void ClipBuffer(float* buffer, int length)
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

    private unsafe void MixingMethod()
    {
        while (true)
        {
            _manualResetEvent.WaitOne();
            Console.WriteLine("WaitOne");

            while (AllBuffersIsAvailable())
                Mix();

            _manualResetEvent.Reset();
            Console.WriteLine("Reset");
        }

        void Mix()
        {
            c++;
            var readBuffer = _audioChannels[0].ReadNextBuffer();
            if (readBuffer.Count != _masterBuffer.Length)
                throw new Exception();
            Array.Copy(readBuffer.Array, readBuffer.Offset, _masterBuffer, 0, _masterBuffer.Length);

            fixed (byte* ptr1 = _masterBuffer)
            {
                float* masterBufferFloat = (float*)ptr1;

                for (int i = 1; i < _audioChannels.Count; i++)
                {
                    var buffer = _audioChannels[i].ReadNextBuffer();

                    fixed (byte* ptr2 = &buffer.Array[buffer.Offset])
                    {
                        float* bufferFloat = (float*)ptr2;
                        AddBuffer(masterBufferFloat, bufferFloat, _masterBuffer.Length);
                    }
                }

                ClipBuffer(masterBufferFloat, _masterBuffer.Length);
                
                _audioEncoder.EncodeAndWriteFrame(_masterBuffer);
            }

            void AddBuffer(float* destonation, float* source, int length)
            {
                for (int i = 0; i < length; i++)
                {
                    destonation[i] += source[i];
                }
            }

            void ClipBuffer(float* buffer, int length)
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
        for (int i = 0; i < _audioChannels.Count; i++)
        {
            if (!_audioChannels[i].BufferIsAvailable)
            {
                return false;
            }
        }
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