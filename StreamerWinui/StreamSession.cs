using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using FFmpeg.AutoGen.Abstractions;
using System.Runtime.InteropServices;
using System.Threading;

namespace StreamerWinui
{
    public class StreamSession
    {
        public bool StreamIsActive { get { return _streamIsActive; } }
        private Thread _thread;

        private bool _streamIsActive = false;
        private Ddagrab _ddagrab;
        private Encoder _encoder;
        private bool _stopStreamFlag = false;
        private int _response;
        private string _codecName = "";
        private string _ipToStream = "";
        private string _outputUrl = "";
        private float _resolutionMultiplyer = 1;
        private string _ddagrabParameters = "";

        /// <summary>
        /// if framerate is 0 then default value will be used
        /// </summary>
        /// <param name="codecName"></param>
        /// <param name="framerate"></param>
        /// <param name="ipToStream"></param>
        /// <param name="cropResolution"></param>
        /// <param name="showConsoleStream"></param>
        public void startStream(string codecName = "hevc_nvenc",
            int framerate = 0,
            string ipToStream = "localhost",
            Size cropResolution = new Size())
        {
            List<string> parameters = new List<string>();
            
            if (framerate != 0)
                parameters.Add("framerate=" + framerate);
            if (cropResolution != Size.Empty)
                parameters.Add("video_size=" + cropResolution.Width + "x" + cropResolution.Height);
            
            _ddagrabParameters = parameters.FirstOrDefault();
            
            if (parameters.Count >= 2)
            {
                _ddagrabParameters += ":" + parameters[1];
                if (parameters.Count > 2)
                    for (int i = 2; i < parameters.Count; i++)
                        _ddagrabParameters += "," + parameters[i];
            }
            
            _codecName = codecName;
            _ipToStream = ipToStream;
            _thread.Start();
            _streamIsActive = true;
        }

        unsafe void startProcess()
        {
            _ddagrab = new Ddagrab(_ddagrabParameters);
            _encoder = new Encoder(_ddagrab.formatContext, _ddagrab.hwFrame->hw_frames_ctx, _codecName);
            
            AVRational[] timebases = new[] { _ddagrab.timebaseMin };
            Streamer.AddStream(_ipToStream + ":10000", new []{_encoder.codecParameters}, ref timebases);

            //main loop
            while (true)
            {
                if (_stopStreamFlag)
                    break;
                _response = ffmpeg.av_read_frame(_ddagrab.formatContext, _ddagrab.packet);
                _response = ffmpeg.avcodec_send_packet(_ddagrab.codecContext, _ddagrab.packet);
                _response = ffmpeg.avcodec_receive_frame(_ddagrab.codecContext, _ddagrab.hwFrame);
                _response = ffmpeg.avcodec_send_frame(_encoder.codecContext, _ddagrab.hwFrame);
                _response = ffmpeg.avcodec_receive_packet(_encoder.codecContext, _encoder.packet);

                ffmpeg.av_packet_rescale_ts(_encoder.packet,
                    _encoder.codecContext->time_base,
                    timebases[_encoder.packet->stream_index]);
                if (_response == 0)
                {
                    _response = Streamer.WriteFrame(_encoder.packet);
                    //response = ffmpeg.av_write_frame(outputFormatContext, encoder.packet);
                    ffmpeg.av_packet_unref(_ddagrab.packet);
                    Console.WriteLine($"frame {_encoder.codecContext->frame_number} writed. pts {_encoder.packet->dts}. dts {_encoder.packet->dts}");
                }
            }

            //response = ffmpeg.av_write_trailer(outputFormatContext);
            //ffmpeg.avio_close(outputFormatContext->pb);
            //ffmpeg.avformat_free_context(outputFormatContext);

            _stopStreamFlag = false;
            _streamIsActive = false;
        }

        public void stopStream()
        {
            _stopStreamFlag = true;
        }

        public StreamSession()
        {
            if (DynamicallyLoadedBindings.LibrariesPath == String.Empty)
                inicialize();
            _thread = new Thread(startProcess);
        }

        public static Codec[] supportedCodecs =
        {
            new Codec("hevc Nvidia", "hevc_nvenc"),
            new Codec("hevc AMD", "hevc_amf"),
            new Codec("h264 Nvidia", "h264_nvenc"),
            new Codec("h264 AMD", "h264_amf"),
        };

        public static unsafe void errStrPrint(int errNum)
        {
            byte[] str1 = new byte[64];
            fixed (byte* str2 = str1)
            {
                ffmpeg.av_make_error_string(str2, 64, errNum);
                debugLogUnmanagedPtr(str2);
            }
        }

        public static unsafe void debugLogUnmanagedPtr(byte* ptr)
        {
            Debug.WriteLine(Marshal.PtrToStringAnsi((IntPtr)ptr));
        }

        public static unsafe string unmanagedPtrToString(byte* ptr)
        {
            return Marshal.PtrToStringAnsi((IntPtr)ptr) ?? "";
        }

        public static void inicialize()
        {
            setFFMpegBinaresPath(@"C:\Users\Cray\Desktop\Programs\ffmpeg");
            DynamicallyLoadedBindings.Initialize();
        }

        static void setFFMpegBinaresPath()
        {
            string ffmpegBinaresPath = Path.Combine(Environment.CurrentDirectory, "ffmpeg", "bin");
            DynamicallyLoadedBindings.LibrariesPath = ffmpegBinaresPath;
        }

        static void setFFMpegBinaresPath(string path) 
        { 
            DynamicallyLoadedBindings.LibrariesPath = path; 
        }
    }
}
