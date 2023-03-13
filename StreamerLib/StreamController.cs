using NAudio.CoreAudioApi;
using System.Drawing;
using System.Net;

namespace StreamerLib;
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

    public MasterChannel MasterChannel
    {
        get
        {
            if (_masterChannel == null)
                _masterChannel = new MasterChannel(_streamWriter, DefaultAudioEncoder);

            return _masterChannel;
        }
    }

    public StreamWriter StreamWriter => _streamWriter;
    
    private bool _videoCapturing;
    private bool _audioCapturing;
    private bool _streamIsActive;
    private int _framerate = 0;
    private Size _cropResolution;
    private double _resolutionMultiplyer = 1;
    //private Ddagrab _ddagrab;
    //private HardwareEncoder _hardwareEncoder;
    private StreamWriter _streamWriter = new();
    private MasterChannel _masterChannel;

    public void StartStream()
    {
        ValidateParameters();
        
        _streamIsActive = true;
        
        //if _masterChannel not set, then setup defoult audio output device
        if (_masterChannel == null)
        {
            _masterChannel = new MasterChannel(_streamWriter, DefaultAudioEncoder);
            var device = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            _masterChannel.AddChannel(device);
        }
        
        _masterChannel.StartStreaming();
    }
    
    public void StopStream()
    {
        _masterChannel.Dispose();
        _streamWriter.Stop();
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

    private void ValidateParameters()
    {
        if (!_audioCapturing) 
            throw new Exception("Nothing set to record");
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