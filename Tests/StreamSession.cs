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

namespace StreamerWinui
{
    public class StreamSession
    {
        public const Encoders DefaultVideoEncoder = Encoders.HevcNvenc;
        
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
        
        public bool AudioRecording
        {
            get => _audioRecording;
            set => _audioRecording = value;
        }
        
        public bool VideoRecording
        {
            get => _videoRecording;
            set => _videoRecording = value;
        }

        public Size CropResolution
        {
            get => _cropResolution;
            set => _cropResolution = value;
        }

        public MMDevice MMDevice { get; set; }
        
        private bool _videoRecording;
        private bool _audioRecording;
        private bool _streamIsActive;
        private int _framerate = 0;
        private Size _cropResolution;
        private Encoders _encoder;
        private double _resolutionMultiplyer = 1;
        private Task _task;
        private Ddagrab _ddagrab;
        private HardwareEncoder _hardwareEncoder;
        private AudioRecorder _audioRecorder;
        private Streamer _streamer;

        public void StartStream()
        {
            if (!_audioRecording) 
                return;
            _streamIsActive = true;

            _audioRecorder = new AudioRecorder(_streamer, Encoders.LibOpus);
            _audioRecorder.MMDevice = MMDevice;
            _audioRecorder.StartEncoding();
        }
        
        /// <summary>
        /// send signal to stop stream
        /// </summary>
        public void StopStream()
        {
            _audioRecorder.Dispose();
            _streamer.Dispose();
        }

        public bool AddClient(IPAddress ipAddress, int port = Streamer.DefaultPort) => _streamer.AddClient(ipAddress, port);
        public bool AddClientAsFile(string Path) => _streamer.AddClientAsFile(Path);
        public void WaitTask() => _task.Wait();

        private string DdagrabParametersToString()
        {
            List<string> parameters = new List<string>();
            
            if (_framerate != 0)
                parameters.Add("framerate=" + _framerate);
            if (_cropResolution != Size.Empty)
                parameters.Add("video_size=" + _cropResolution.Width + "x" + _cropResolution.Height);

            if (parameters.Count > 0)
            {
                string str = parameters.First();

                if (parameters.Count >= 2)
                {
                    str += ":" + parameters[1];
                    if (parameters.Count > 2)
                        for (int i = 2; i < parameters.Count; i++)
                            str += "," + parameters[i];
                }
                return str;
            }
            return string.Empty;
        }

        public StreamSession()
        {
            if (DynamicallyLoadedBindings.LibrariesPath == String.Empty)
                Inicialize();
            
            _streamer = new Streamer();
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

        public static unsafe void ErrStrPrint(int errNum)
        {
            int maxErrorStringSize = 64;
            byte[] str1 = new byte[maxErrorStringSize];
            fixed (byte* str2 = str1)
            {
                ffmpeg.av_make_error_string(str2, 64, errNum);
                DebugLogUnmanagedPtr(str2);
            }
        }
        
        public static void Inicialize()
        {
            SetFfmpegBinaresPath(@"C:\Users\Cray\Desktop\Programs\ffmpeg");
            DynamicallyLoadedBindings.Initialize();
        }

        public static unsafe void DebugLogUnmanagedPtr(byte* ptr) =>
            Debug.WriteLine(Marshal.PtrToStringAnsi((IntPtr)ptr));

        public static unsafe string UnmanagedPtrToString(byte* ptr) =>
            Marshal.PtrToStringAnsi((IntPtr)ptr) ?? String.Empty;

        static void SetFfmpegBinaresPath()=>
            DynamicallyLoadedBindings.LibrariesPath = Path.Combine(Environment.CurrentDirectory, "ffmpeg", "bin");

        static void SetFfmpegBinaresPath(string path) =>
            DynamicallyLoadedBindings.LibrariesPath = path; 
    }
}
