using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace StreamerLib;

public class WasapiAudioCapturingChannel
{
    public const float DefaultVolume = 1f;
    public const int QueueMaximumCapacity = 32;

    public int Channels => _wasapiCapture.WaveFormat.Channels;
    public WaveFormat WaveFormat => _wasapiCapture.WaveFormat;
    public MMDevice MMDevice { get; }
    public float Volume { get; set; } = DefaultVolume;
    public Queue<ArraySegment<byte>> Buffers => _buffersQueue;
    public event EventHandler<WaveInEventArgs> DataAvailable;
    public CaptureState CaptureState => _wasapiCapture.CaptureState;
    public string DeviceFriendlyName { get; }

    private AudioBufferSlicer _audioBufferSlicer;
    private WasapiCapture _wasapiCapture;
    private Queue<ArraySegment<byte>> _buffersQueue = new();

    public ArraySegment<byte> ReadNextBuffer()
    {
        if (!BufferIsAvailable)
            throw new Exception("Buffer is not available");
        if (_wasapiCapture.CaptureState == CaptureState.Stopped)
            return ArraySegment<byte>.Empty;
        return _buffersQueue.Dequeue();
    }

    public WasapiAudioCapturingChannel(MMDevice mmDevice, int frameSizeInBytes)
    {
        if (mmDevice.DataFlow == DataFlow.All)
            throw new Exception("Unsupported MMDevice.DataFlow");

        _wasapiCapture = (mmDevice.DataFlow == DataFlow.Capture)
            ? new WasapiCapture(mmDevice)
            : new WasapiLoopbackCapture(mmDevice);

        MMDevice = mmDevice;
        DeviceFriendlyName = mmDevice.FriendlyName;
        _audioBufferSlicer = new AudioBufferSlicer(frameSizeInBytes);
        _wasapiCapture.DataAvailable += _wasapiCapture_DataAvailable;
    }

    private void _wasapiCapture_DataAvailable(object? s, WaveInEventArgs args)
    {
        if (args.BytesRecorded == 0)
            return;
        if (Volume < 1f)
            ApplyVolume(args.Buffer, args.BytesRecorded, Volume);

        var buffersList = _audioBufferSlicer.SliceBufferToArraySegments(args.Buffer, args.BytesRecorded);
        if (_buffersQueue.Count + buffersList.Count > QueueMaximumCapacity)
        {
            _buffersQueue.Clear();
            LoggingHelper.LogToCon("buffer queue cleared");
        }
        foreach (var item in buffersList)
            _buffersQueue.Enqueue(item);
        DataAvailable.Invoke(this, args);
    }

    public bool BufferIsAvailable => (_buffersQueue.Count > 0) ? true : false;

    private unsafe void ApplyVolume(byte[] buffer, int bufferSize, float volume)
    {
        fixed (byte* ptr = buffer)
        {
            float* ptrFloat = (float*)ptr;
            for (int i = 0; i < bufferSize / 4; i++)
                ptrFloat[i] *= volume;
        }
    }

    public void StartRecording() => _wasapiCapture.StartRecording();

    public void StopRecording()
    {
        _wasapiCapture.StopRecording();
        _audioBufferSlicer.Clear();
    }
}