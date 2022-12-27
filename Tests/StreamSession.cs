using System.Diagnostics;
using System.Drawing;
using System.Net;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using FFmpeg.AutoGen.Abstractions;
using System.Runtime.InteropServices;

namespace StreamerWinui
{
    public class StreamSession
    {
        public const Encoders DefaultVideoEncoder = Encoders.HevcNvenc;
        
        public bool StreamIsActive => _streamIsActive;
        
        public int Framerate
        {
            get => _framerate;
            set
            {
                if (value >= 0)
                    _framerate = value;
            }
        }
        
        public double ResolutionMultiplyer
        {
            get => _resolutionMultiplyer;
            set
            {
                if (value is > 0 and <= 1)
                    _resolutionMultiplyer = value;
            }
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
        
        private bool _videoRecording;
        private bool _audioRecording;
        private int _framerate = 0;
        private Size _cropResolution;
        private Encoders _encoder;
        private double _resolutionMultiplyer = 1;
        private bool _streamIsActive;
        
        
        private Task _task;

        private Ddagrab _ddagrab;
        private HardwareEncoder _hardwareEncoder;
        private AudioRecorder _audioRecorder;
        private Streamer _streamer;
        
        
        private bool _stopStreamFlag;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="codecName"></param>
        /// <param name="framerate">if 0, the default value will be used</param>
        /// <param name="ipToStream"></param>
        /// <param name="cropResolution"></param>
        /// <param name="recordVideo"></param>
        /// <param name="recordAudio"></param>
        public void StartStream()
        {
            if (!_streamIsActive)
                _task.Start();
        }

        unsafe void StartProcess()
        {
            if (!_audioRecording && !_videoRecording) 
                return;

            _streamIsActive = true;
            
            _streamer = new();
            
            if (_videoRecording)
            {
                _ddagrab = new Ddagrab(DdagrabParametersToString());
                _hardwareEncoder = new HardwareEncoder(_ddagrab.formatContext, _ddagrab.hwFrame->hw_frames_ctx, "hevc_nvenc", _streamer);
                _hardwareEncoder.StreamIndex = _streamer.AddAvStream(_hardwareEncoder.codecParameters, _ddagrab.timebaseMin);
            }

            if (_audioRecording)
            {
                _audioRecorder = new AudioRecorder(_streamer);
                _audioRecorder.StreamIndex = _streamer.AddAvStream(_audioRecorder.codecParameters, _audioRecorder.timebase);
            }

            if (_videoRecording)
                Task.Run(() =>
                {
                    while (true)
                    {
                        if (_stopStreamFlag)
                            break;
                    
                        _ddagrab.ReadAvFrame();
                        _hardwareEncoder.EncodeAndWriteFrame(_ddagrab.hwFrame);
                    }
                });
            
            if (_audioRecording)
                _audioRecorder.StartEncoding();
        }
        
        /// <summary>
        /// send signal to stop stream
        /// </summary>
        public void StopStream()
        {
            _stopStreamFlag = true;
            _task.Wait();
            _audioRecorder.StopEncoding();
            _ddagrab.Dispose();
            _audioRecorder.Dispose();
            _streamer.Dispose();
        }

        public bool AddClient(IPAddress ipAddress, int port = Streamer.DefaultPort) => _streamer.AddClient(ipAddress, port);

        private string DdagrabParametersToString()
        {
            string str;
            List<string> parameters = new List<string>();
            
            if (_framerate != 0)
                parameters.Add("framerate=" + _framerate);
            if (_cropResolution != Size.Empty)
                parameters.Add("video_size=" + _cropResolution.Width + "x" + _cropResolution.Height);
            
            str = parameters.First();
            
            if (parameters.Count >= 2)
            {
                str += ":" + parameters[1];
                if (parameters.Count > 2)
                    for (int i = 2; i < parameters.Count; i++)
                        str += "," + parameters[i];
            }

            return str;
        }

        public StreamSession()
        {
            if (DynamicallyLoadedBindings.LibrariesPath == String.Empty)
                Inicialize();
            
            _task = new(StartProcess);
        }

        public static readonly Codec[] SupportedCodecs =
        {
            new Codec("hevc Nvidia", "hevc_nvenc"),
            new Codec("hevc AMD", "hevc_amf"),
            new Codec("h264 Nvidia", "h264_nvenc"),
            new Codec("h264 AMD", "h264_amf"),
            new Codec("AV1 Nvidia", "av1_nvenc"),
        };

        public static unsafe void ErrStrPrint(int errNum)
        {
            byte[] str1 = new byte[64];
            fixed (byte* str2 = str1)
            {
                ffmpeg.av_make_error_string(str2, 64, errNum);
                DebugLogUnmanagedPtr(str2);
            }
        }

        public static unsafe void DebugLogUnmanagedPtr(byte* ptr)
        {
            Debug.WriteLine(Marshal.PtrToStringAnsi((IntPtr)ptr));
        }

        public static unsafe string UnmanagedPtrToString(byte* ptr)
        {
            return Marshal.PtrToStringAnsi((IntPtr)ptr) ?? "";
        }

        public static void Inicialize()
        {
            SetFfmpegBinaresPath(@"C:\Users\Cray\Desktop\Programs\ffmpeg");
            DynamicallyLoadedBindings.Initialize();
        }

        static void SetFfmpegBinaresPath()
        {
            string ffmpegBinaresPath = Path.Combine(Environment.CurrentDirectory, "ffmpeg", "bin");
            DynamicallyLoadedBindings.LibrariesPath = ffmpegBinaresPath;
        }

        static void SetFfmpegBinaresPath(string path) 
        { 
            DynamicallyLoadedBindings.LibrariesPath = path; 
        }
    }
}
