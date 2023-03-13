using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace StreamerLib
{
    public class WasapiAudioCapturingChannel
    {
        public const float DefaultVolume = 1f;
        public const int QueueCapacity = 8;

        public int Channels => _wasapiCapture.WaveFormat.Channels;
        public WaveFormat WaveFormat => _wasapiCapture.WaveFormat;
        public MMDevice MMDevice { get; }
        public float Volume { get; set; } = DefaultVolume;
        public Queue<ArraySegment<byte>> Buffers => _buffersQueue;
        public event EventHandler<WaveInEventArgs> DataAvailable;
        public CaptureState CaptureState => _wasapiCapture.CaptureState;

        private AudioBufferSlicer _audioBufferSlicer;
        private WasapiCapture _wasapiCapture;
        private Queue<ArraySegment<byte>> _buffersQueue = new Queue<ArraySegment<byte>>(QueueCapacity);

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
                throw new Exception("Wrong mmDevice.DataFlow");
            MMDevice = mmDevice;
            _wasapiCapture = (mmDevice.DataFlow == DataFlow.Capture)
                ? new WasapiCapture(mmDevice)
                : new WasapiLoopbackCapture(mmDevice);
            _audioBufferSlicer = new AudioBufferSlicer(frameSizeInBytes);

            _wasapiCapture.DataAvailable += _wasapiCapture_DataAvailable;
        }

        private void _wasapiCapture_DataAvailable(object? s, WaveInEventArgs e)
        {
            if (Volume != 1f)
                ApplyVolume(e.Buffer, e.BytesRecorded, Volume);

            var buffersList = _audioBufferSlicer.SliceBufferToArraySegments(e.Buffer, e.BytesRecorded);
            if (_buffersQueue.Count + buffersList.Count > QueueCapacity)
                _buffersQueue.Clear();
            foreach (var item in buffersList)
                _buffersQueue.Enqueue(item);
            DataAvailable.Invoke(this, e);
        }

        public bool BufferIsAvailable
        {
            get
            {
                if (_buffersQueue.Count > 0 || _wasapiCapture.CaptureState == CaptureState.Stopped)
                    return true;
                return false;
            }
        }

        private unsafe void ApplyVolume(byte[] buffer, int bufferSize, float volume)
        {
            fixed (byte* p = buffer)
            {
                float* pf = (float*)p;
                for (int i = 0; i < bufferSize / 4; i++)
                    pf[i] *= volume;
            }
        }

        public void StartRecording() => _wasapiCapture.StartRecording();
        public void StopRecording() => _wasapiCapture.StopRecording();
    }
}