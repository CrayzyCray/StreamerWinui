using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Linq;

namespace StreamerLib
{

    public class MasterChannel : IDisposable
    {
        public const int SampleSizeInBytes = 4;

        public List<WasapiAudioCapturingChannel> WasapiAudioChannels => _wasapiAudioChannels;
        public int FrameSizeInBytes => _audioEncoder.FrameSizeInBytes;
        public StreamWriter StreamWriter { get; }
        public Encoders Encoder { get; }
        public MasterChannelState State { get; private set; } = MasterChannelState.Monitoring;

        private AudioEncoder _audioEncoder;
        private List<WasapiAudioCapturingChannel> _wasapiAudioChannels = new(2);

        public MasterChannel(StreamWriter streamWriter, Encoders encoder)
        {
            StreamWriter = streamWriter;
            Encoder = encoder;
            _audioEncoder = new(streamWriter, encoder);
        }

        //public void AddChannel(MMDevice mmDevice)
        //{
        //    var channel = new WasapiAudioCapturingChannel(mmDevice, _audioEncoder.FrameSizeInBytes);
        //    channel.DataAvailable += RecieveBuffer;
        //    _wasapiAudioChannels.Add(channel);
        //}

        public void AddChannel(WasapiAudioCapturingChannel capturingChannel)
        {
            //capturingChannel.DataAvailable += RecieveBuffer;
            _wasapiAudioChannels.Add(capturingChannel);
        }

        public void RemoveChannel(WasapiAudioCapturingChannel capturingChannel)
        {
            if (!_wasapiAudioChannels.Contains(capturingChannel))
                throw new ArgumentException("MasterChannel not contains this WasapiAudioCapturingChannel");

            capturingChannel.DataAvailable -= RecieveBuffer;
            _wasapiAudioChannels.Remove(capturingChannel);
        }

        public void DeleteAllChannels()
        {
            Dispose();
            _wasapiAudioChannels.Clear();
        }

        public void StartStreaming()
        {
            foreach (var channel in _wasapiAudioChannels)
                channel.DataAvailable += RecieveBuffer;

            foreach (var channel in _wasapiAudioChannels)
                if (channel.CaptureState == CaptureState.Stopped)
                    channel.StartRecording();
        }

        private void RecieveBuffer(object? sender, EventArgs e)
        {
            if (true)
            {

            }
            // foreach (var item in _wasapiAudioChannels)
            //     if (item.BufferIsAvailable == false)
            //         return;

            //mixing and encoding
            var test = _wasapiAudioChannels[0];
            while (test.BufferIsAvailable)
                _audioEncoder.EncodeAndWriteFrame(test.ReadNextBuffer());
        }

        public void Dispose()
        {
            _wasapiAudioChannels.ForEach(c => c.StopRecording());
        }
    }

    public enum MasterChannelState
    {
        Monitoring,
        Streaming
    }

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