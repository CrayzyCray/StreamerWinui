using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using FFmpeg.AutoGen.Abstractions;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;

namespace StreamerLib
{
    public class StreamController
    {
        public const Encoders DefaultVideoEncoder = Encoders.HevcNvenc;
        public const Encoders DefaultAudioEncoder = Encoders.LibOpus;
        
        public bool StreamIsActive => _streamIsActive;
        /// <summary>
        /// if 0 then used defaul value
        /// </summary>
        public int Framerate
        {
            get => _framerate;
            set => _framerate = (value >= 0) ? value : _framerate;
        }
        
        public double ResolutionMultiplyer
        {
            get => _resolutionMultiplyer;
            set => _resolutionMultiplyer = (value is > 0 and <= 1) ? value : _resolutionMultiplyer;
        }

        public Encoders Encoder
        {
            get => _encoder;
            set => _encoder = value;
        }
        
        public bool AudioCapturing
        {
            get => _audioCapturing;
            set => _audioCapturing = value;
        }
        
        public bool VideoCapturing
        {
            get => _videoCapturing;
            set => _videoCapturing = value;
        }

        public Size CropResolution
        {
            get => _cropResolution;
            set => _cropResolution = value;
        }

        public MMDevice MMDevice { get; set; }
        public StreamWriter StreamWriter { get; }
        
        private bool _videoCapturing;
        private bool _audioCapturing;
        private bool _streamIsActive;
        private int _framerate = 0;
        private Size _cropResolution;
        private Encoders _encoder;
        private double _resolutionMultiplyer = 1;
        private Ddagrab _ddagrab;
        private HardwareEncoder _hardwareEncoder;
        private AudioRecorder _audioRecorder;
        private StreamWriter _streamWriter = new();
        private MasterChannel _masterChannel;

        public void StartStream()
        {
            Validate();
            
            _streamIsActive = true;
            
            _masterChannel = new MasterChannel(_streamWriter, Encoders.LibOpus);
            _masterChannel.AddChannel(MMDevice);
            _masterChannel.StartStreaming();
        }
        
        public void StopStream()
        {
            _masterChannel.Dispose();
            _streamWriter.Stop();
        }

        public StreamController()
        {
            FFmpegHelper.InicializeFFmpeg();
        }

        public bool AddClient(IPAddress ipAddress, int port = StreamWriter.DefaultPort) => _streamWriter.AddClient(ipAddress, port);
        public bool AddClientAsFile(string Path) => _streamWriter.AddClientAsFile(Path);

        private string DdagrabParametersToString()
        {
            List<string> parameters = new List<string>();
            
            if (_framerate != 0)
                parameters.Add("framerate=" + _framerate);
            if (_cropResolution != Size.Empty)
                parameters.Add("video_size=" + _cropResolution.Width + "x" + _cropResolution.Height);

            if (parameters.Count == 0)
                return string.Empty;

            string str = parameters.First();

            if (parameters.Count == 1)
                return str;

            str += ":" + parameters[1];

            if (parameters.Count > 2)
                for (int i = 2; i < parameters.Count; i++)
                    str += "," + parameters[i];

            return str;
        }

        private void Validate()
        {
            if (!_audioCapturing) 
                throw new Exception("Nothing set to record");
            if (MMDevice == null)
                throw new Exception("MMDevice not set");
        }

        public static readonly Codec[] SupportedCodecs =
        {
            new Codec("hevc Nvidia", "hevc_nvenc", Encoders.HevcNvenc, MediaTypes.Video),
            new Codec("hevc AMD", "hevc_amf", Encoders.HevcAmf, MediaTypes.Video),
            new Codec("h264 Nvidia", "h264_nvenc", Encoders.H264Nvenc, MediaTypes.Video),
            new Codec("h264 AMD", "h264_amf", Encoders.H264Amf, MediaTypes.Video),
            new Codec("AV1 Nvidia", "av1_nvenc", Encoders.Av1Nvenc, MediaTypes.Video),
            new Codec("Opus", "libopus", Encoders.LibOpus, MediaTypes.Audio)
        };
    }
}
