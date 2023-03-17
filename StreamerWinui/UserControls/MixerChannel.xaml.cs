using Microsoft.UI.Xaml.Controls;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using StreamerLib;
using System;
using System.Threading.Tasks;

namespace StreamerWinui.UserControls;
public sealed partial class MixerChannel : UserControl
{
    public const double MinimumVolumeMeterLevelDbfs = -62;

    public WasapiAudioCapturingChannel WasapiAudioCapturingChannel { get; }
    public MMDevice Device => WasapiAudioCapturingChannel.MMDevice;
    public event EventHandler OnDeleting;
    const int _width = 352;
    const int _height = 70;

    public MixerChannel(MMDevice mmDevice, int frameSizeInBytes)
    {
        if (mmDevice.DataFlow == DataFlow.All)
            throw new Exception("Wrong mmDevice.DataFlow");

        WasapiAudioCapturingChannel = new(mmDevice, frameSizeInBytes);
        WasapiAudioCapturingChannel.StartRecording();
        WasapiAudioCapturingChannel.DataAvailable += _captureDataRecieved;

        this.InitializeComponent();

        DeviceNameTextBlock.Text = WasapiAudioCapturingChannel.MMDevice.FriendlyName;
    }

    public MixerChannel(WasapiAudioCapturingChannel wasapiAudioCapturingChannel)
    {
        this.InitializeComponent();

        wasapiAudioCapturingChannel.DataAvailable += _captureDataRecieved;
        if (wasapiAudioCapturingChannel.CaptureState == CaptureState.Stopped)
            wasapiAudioCapturingChannel.StartRecording();

        DeviceNameTextBlock.Text = wasapiAudioCapturingChannel.MMDevice.FriendlyName;
        WasapiAudioCapturingChannel = wasapiAudioCapturingChannel;
    }

    public void SetVolumeMeterLevel(double dbfs)
    {
        this.dbfs = dbfs;
        VolumeCanvas.Invalidate();
    }

    private void _captureDataRecieved(object sender, NAudio.Wave.WaveInEventArgs args)
    {
        //if (_capture.CaptureState == CaptureState.Stopped)
        //    return;
            var buffer = new WaveBuffer(args.Buffer);
            float peak = 0;

            for (int index = 0; index < args.BytesRecorded / 4; index++)
            {
                var sample = buffer.FloatBuffer[index];
                if (sample < 0) sample = -sample;
                if (sample > peak) peak = sample;
            }

            double dbfs = 20 * Math.Log10(peak);
            SetVolumeMeterLevel(dbfs);
    }

    private void DeleteButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        WasapiAudioCapturingChannel.StopRecording();
        OnDeleting.Invoke(this, null);
    }

    Windows.Foundation.Rect rect;
    Windows.UI.Color VolumeMeterColor = Windows.UI.Color.FromArgb(128, 0, 255, 0);
    double dbfs = 0;

    private void Canvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
    {
        var width = (-MinimumVolumeMeterLevelDbfs + dbfs) * _width / -MinimumVolumeMeterLevelDbfs;
        if (width < 0)
            width = 0;
        else if (width > _width)
            width = _width;
        rect.Width = width;
        args.DrawingSession.DrawLine((float)(width - 1), 0, (float)(width - 1), _height, VolumeMeterColor, 1);
    }

    public void Dispose()
    {
        WasapiAudioCapturingChannel.StopRecording();
    }
}