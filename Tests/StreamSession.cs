using System.Diagnostics;
using System.Drawing;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using FFmpeg.AutoGen.Abstractions;
using System.Runtime.InteropServices;

namespace StreamerWinui
{
    public class StreamSession
    {
        public bool StreamIsActive => _streamIsActive;
        private Thread _thread;

        private bool _streamIsActive;
        private Ddagrab _ddagrab;
        private Encoder _encoder;
        private AudioRecorder _audioRecorder;
        private bool _stopStreamFlag;
        private string _codecName = "";
        private string _ipToStream = "";
        private float _resolutionMultiplyer = 1;
        private string _ddagrabParameters = "";
        private bool _recordVideo;
        private bool _recordAudio;
        private Streamer _streamer;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="codecName"></param>
        /// <param name="framerate">if 0, the default value will be used</param>
        /// <param name="ipToStream"></param>
        /// <param name="cropResolution"></param>
        /// <param name="recordVideo"></param>
        /// <param name="recordAudio"></param>
        public void StartStream(string codecName = "hevc_nvenc",
            int framerate = 0,
            string ipToStream = "localhost",
            Size cropResolution = new Size(),
            bool recordVideo = true,
            bool recordAudio = true)
        {
            List<string> parameters = new List<string>();
            
            if (framerate != 0)
                parameters.Add("framerate=" + framerate);
            if (cropResolution != Size.Empty)
                parameters.Add("video_size=" + cropResolution.Width + "x" + cropResolution.Height);
            
            _ddagrabParameters = parameters.First();
            
            if (parameters.Count >= 2)
            {
                _ddagrabParameters += ":" + parameters[1];
                if (parameters.Count > 2)
                    for (int i = 2; i < parameters.Count; i++)
                        _ddagrabParameters += "," + parameters[i];
            }
            
            _codecName = codecName;
            _ipToStream = ipToStream;
            _recordVideo = recordVideo;
            _recordAudio = recordAudio;
            _thread.Start();
            _streamIsActive = true;
        }

        unsafe void StartProcess()
        {
            _streamer = new();
            
            if (_recordVideo)
            {
                _ddagrab = new Ddagrab(_ddagrabParameters);
                _encoder = new Encoder(_ddagrab.formatContext, _ddagrab.hwFrame->hw_frames_ctx, _codecName, _streamer);
                _encoder.StreamIndex = _streamer.AddAvStream(_encoder.codecParameters, _ddagrab.timebaseMin);
            }

            if (_recordAudio)
            {
                _audioRecorder = new AudioRecorder(_streamer);
                _audioRecorder.StreamIndex = _streamer.AddAvStream(_audioRecorder.codecParameters, _audioRecorder.timebase);
                _audioRecorder.RecordingStopped += () => _streamer.CloseAllClients();
            }
            
            _streamer.AddClient(_ipToStream + ":10000");
            //_streamer.AddClientAsFile(@"D:\video\img\2.opus");
            
            if (_recordAudio)
                _audioRecorder.StartEncoding();
            
            //main loop
            if (_recordVideo)
                while (true)
                {
                    if (_stopStreamFlag)
                        break;
                    
                    _ddagrab.ReadAvFrame();
                    _encoder.EncodeAndWriteFrame(_ddagrab.hwFrame);
                }
            Thread.Sleep(10000 * 1000);
            if (_recordAudio)
                _audioRecorder.StopEncoding();

            _stopStreamFlag = false;
            _streamIsActive = false;
        }
        
        /// <summary>
        /// send signal to stop stream
        /// </summary>
        public void StopStream()
        {
            _stopStreamFlag = true;
        }

        public StreamSession()
        {
            if (DynamicallyLoadedBindings.LibrariesPath == String.Empty)
                Inicialize();
            _thread = new Thread(StartProcess);
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
