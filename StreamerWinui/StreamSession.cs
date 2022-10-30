using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace StreamerWinui
{
    public class StreamSession
    {
        public bool StreamIsActive { get { return streamIsActive; } }
        public Process FfmpegProcess { get { return ffmpegProcess; } }

        bool streamIsActive = false;
        Process ffmpegProcess = new Process();

        public struct Codec
        {
            public Codec(string _userFriendlyName, string _name)
            {
                userFriendlyName = _userFriendlyName;
                name = _name;
            }
            public string userFriendlyName { get; }
            public string name { get; }
        }
        public Codec[] supportedCodecs =
        {
            new Codec("hevc Nvidia", "hevc_nvenc"),
            new Codec("hevc AMD", "hevc_amf"),
            new Codec("hevc Intel", "hevc_qsv"),
            new Codec("h264 Nvidia", "h264_nvenc"),
            new Codec("h264 AMD", "h264_amf"),
            new Codec("h264 Intel", "h264_qsv")
        };


        public void startStream(string codec, double framerate, string ipToStream, bool showConsole)
        {
            //путь к ffmpeg
            string pathToFfmpegFile = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            pathToFfmpegFile = Path.Combine(pathToFfmpegFile, "ffmpeg.exe");

            string arguments = $"ffmpeg.exe -init_hw_device d3d11va -filter_complex ddagrab=0:framerate={framerate} -c:v {codec} -cq:v 20 -bf 0 -f mpegts rist://{ipToStream}:10000";

            ffmpegProcess.StartInfo.FileName = "cmd.exe";
            ffmpegProcess.StartInfo.Arguments = $"/c {pathToFfmpegFile} & {arguments}" +" & pause";
            ffmpegProcess.StartInfo.CreateNoWindow = !showConsole;

            ffmpegProcess.Start();
            streamIsActive = true;
            
        }

        public void stopStream()
        {
            ffmpegProcess.Kill(true);
            streamIsActive = false;
        }
    }
}
