using NAudio.CoreAudioApi;

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

        public WasapiAudioCapturingChannel AddChannel(MMDevice device)
        {
            var channel = new WasapiAudioCapturingChannel(device, _audioEncoder.FrameSizeInBytes);
            _wasapiAudioChannels.Add(channel);
            return channel;
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
            State = MasterChannelState.Streaming;
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
}