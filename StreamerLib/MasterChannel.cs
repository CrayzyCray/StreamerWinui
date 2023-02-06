using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Linq;

namespace StreamerLib
{

    public class MasterChannel : IDisposable
    {
        public const int SampleSizeInBytes = 4;

        private AudioEncoder _audioEncoder;
        private int _channelIdPointer = 0;
        private List<WasapiAudioCapturingChannel> _wasapiAudioChannels;

        public MasterChannel(StreamWriter streamWriter, Encoders encoder)
        {
            _audioEncoder = new(streamWriter, encoder);
            _wasapiAudioChannels = new List<WasapiAudioCapturingChannel>(2);
        }

        public void AddChannel(MMDevice mmDevice, float volume = 1f)
        {
            var channel = new WasapiAudioCapturingChannel(mmDevice, GetNewRegisteredId(), _audioEncoder.FrameSizeInBytes)
            {
                Volume = volume
            };
            channel.DataAvailable += RecieveBuffer;
            _wasapiAudioChannels.Add(channel);
        }

        public void DeleteAllChannels()
        {
            _wasapiAudioChannels.ForEach(c => c.StopRecording());
            _wasapiAudioChannels.Clear();
        }

        public void StartStreaming()
        {
            _wasapiAudioChannels.ForEach(c => c.StartRecording());
        }

        private void RecieveBuffer(object? sender, EventArgs e)
        {
            // foreach (var item in _wasapiAudioChannels)
            //     if (item.BufferIsAvailable == false)
            //         return;

            //mixing and encoding
            var test = _wasapiAudioChannels[0];
            while (test.BufferIsAvailable)
                _audioEncoder.EncodeAndWriteFrame(test.ReadNextBuffer());
        }

        private int GetNewRegisteredId() => ++_channelIdPointer;

        public void Dispose()
        {
            _wasapiAudioChannels.ForEach(c => c.StopRecording());
        }
    }

    class WasapiAudioCapturingChannel
    {
        public const float DefaultVolume = 1f;
        public const int QueueCapacity = 8;

        public int Channels => _wasapiCapture.WaveFormat.Channels;
        public WaveFormat WaveFormat => _wasapiCapture.WaveFormat;
        public MMDevice MMDevice { get; }
        public int ChannelID { get; }
        public float Volume { get; set; } = DefaultVolume;
        public Queue<ArraySegment<byte>> Buffers => _buffersQueue;
        public event EventHandler DataAvailable;

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

        public WasapiAudioCapturingChannel(MMDevice mmDevice, int channelID, int frameSizeInBytes)
        {
            if (mmDevice.DataFlow == DataFlow.All)
                throw new Exception("Wrong mmDevice.DataFlow");
            MMDevice = mmDevice;
            ChannelID = channelID;
            _wasapiCapture = (mmDevice.DataFlow == DataFlow.Capture)
                ? new WasapiCapture(mmDevice)
                : new WasapiLoopbackCapture(mmDevice);
            _audioBufferSlicer = new AudioBufferSlicer(frameSizeInBytes);

            _wasapiCapture.DataAvailable += (s, e) =>
            {
                if (Volume != 1f)
                    ApplyVolume(e.Buffer, e.BytesRecorded, Volume);

                var buffersList = _audioBufferSlicer.SliceBufferToArraySegments(e.Buffer, e.BytesRecorded);
                if (_buffersQueue.Count + buffersList.Count > QueueCapacity)
                    _buffersQueue.Clear();
                foreach (var item in buffersList)
                    _buffersQueue.Enqueue(item);
                DataAvailable.Invoke(this, null);
            };
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