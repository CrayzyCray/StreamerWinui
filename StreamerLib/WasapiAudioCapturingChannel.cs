using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;

namespace StreamerLib;

public class WasapiAudioCapturingChannel
{
    public const float DefaultVolume = 1f;
    public const int QueueMaximumCapacity = 16;

    public int Channels => _wasapiCapture.WaveFormat.Channels;
    public WaveFormat WaveFormat => _wasapiCapture.WaveFormat;
    public MMDevice MMDevice { get; }
    public float Volume { get; set; } = DefaultVolume;
    public event EventHandler<WaveInEventArgs> DataAvailable;
    public CaptureState CaptureState => _wasapiCapture.CaptureState;
    public string DeviceFriendlyName { get; }
    public bool BufferIsAvailable
    {
        get
        {
            lock(queueLock)
            {
                return _bufferIsAvailable;
            }
        }
    }

    private AudioBufferSlicer _audioBufferSlicer;
    private WasapiCapture _wasapiCapture;
    private Queue<byte[]> _buffersQueue = new(QueueMaximumCapacity);
    private object queueLock = new();
    private bool _bufferIsAvailable;

    public byte[] ReadNextBuffer()
    {
        if (_wasapiCapture.CaptureState == CaptureState.Stopped)
            throw new Exception("CaptureState.Stopped");

        return Dequeue();
    }

    private byte[] Dequeue()
    {
        lock (queueLock)
        {
            if (_buffersQueue.Count == 0)
                throw new Exception("buffers queue is empty");

            var buffer = _buffersQueue.Dequeue();

            if (_buffersQueue.Count == 0)
                _bufferIsAvailable = false;

            return buffer;
        }
    }

    private void Enqueue(List<byte[]> buffers)
    {
        lock (queueLock)
        {
            if (_buffersQueue.Count + buffers.Count > QueueMaximumCapacity)
            {
                _buffersQueue.Clear();
                Debug.WriteLine("queue cleared");
            }

            foreach (var buffer in buffers)
            {
                _buffersQueue.Enqueue(buffer);
            }

            if (_buffersQueue.Count > 0)
                _bufferIsAvailable = true;
        }
    }

    public WasapiAudioCapturingChannel(MMDevice mmDevice, int frameSizeInBytes)
    {
        if (mmDevice.DataFlow == DataFlow.Capture)
            _wasapiCapture = new WasapiCapture(mmDevice);
        else if (mmDevice.DataFlow == DataFlow.Render)
            _wasapiCapture = new WasapiLoopbackCapture(mmDevice);
        else throw new Exception("Unsupported MMDevice.DataFlow");

        MMDevice = mmDevice;
        DeviceFriendlyName = mmDevice.FriendlyName;
        _audioBufferSlicer = new(frameSizeInBytes);
        _wasapiCapture.DataAvailable += _wasapiCapture_DataAvailable;
    }

    private void _wasapiCapture_DataAvailable(object? s, WaveInEventArgs args)
    {
        if (args.BytesRecorded == 0)
            return;

        if (Volume < 1f)
            ApplyVolume(args.Buffer, args.BytesRecorded, Volume);

        var buffersList = _audioBufferSlicer.SliceBufferToArraySegments(args.Buffer, args.BytesRecorded);
        
        Enqueue(buffersList);
        DataAvailable.Invoke(this, args);
    }

    private unsafe void ApplyVolume(byte[] buffer, int bufferLength, float volume)
    {
        fixed (byte* ptr = buffer)
        {
            float* bufferFloat = (float*)ptr;
            for (int i = 0; i < bufferLength / 4; i++)
                bufferFloat[i] *= volume;
        }
    }

    public void StartRecording() => _wasapiCapture.StartRecording();

    public void StopRecording()
    {
        _wasapiCapture.StopRecording();
        _audioBufferSlicer.Clear();
    }
}