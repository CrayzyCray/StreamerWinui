using Microsoft.UI.Xaml.Controls;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using StreamerLib;
using System;
using System.Collections.Generic;
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

    private Queue<double> queueOfDbfs = new(64);
    private int _updateRate;

    //public MixerChannel(MMDevice mmDevice, int frameSizeInBytes)
    //{
    //    if (mmDevice.DataFlow == DataFlow.All)
    //        throw new Exception("Wrong mmDevice.DataFlow");

    //    WasapiAudioCapturingChannel = new(mmDevice, frameSizeInBytes);
    //    WasapiAudioCapturingChannel.StartRecording();
    //    WasapiAudioCapturingChannel.DataAvailable += _captureDataRecieved;

    //    this.InitializeComponent();

    //    DeviceNameTextBlock.Text = WasapiAudioCapturingChannel.MMDevice.FriendlyName;
    //}

    public MixerChannel(WasapiAudioCapturingChannel wasapiAudioCapturingChannel, int updateRate = 30)
    {
        _updateRate = updateRate;

        this.InitializeComponent();
        
        WasapiAudioCapturingChannel = wasapiAudioCapturingChannel;
        WasapiAudioCapturingChannel.DataAvailable += _captureDataRecieved;
        
        DeviceNameTextBlock.Text = WasapiAudioCapturingChannel.MMDevice.FriendlyName;
    }

    public void SetVolumeMeterLevel(double dbfs)
    {
        this.dbfs = dbfs;
        PeakVolumeCanvas.Invalidate();
    }

    private void _captureDataRecieved(object sender, NAudio.Wave.WaveInEventArgs args)
    {
        float dbfs = LibUtil.get_peak_safe(args.Buffer, args.BytesRecorded);
        SetVolumeMeterLevel(dbfs);
    }

    private void DeleteButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        WasapiAudioCapturingChannel.StopRecording();
        OnDeleting.Invoke(this, null);
    }

    Windows.Foundation.Rect rect;
    Windows.UI.Color VolumeMeterColor = Windows.UI.Color.FromArgb(128, 0, 255, 0);
    double _dbfsToDraw = 0;
    private double _meterDecay = 24;

    private void Canvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
    {
        double dbfsPredicted = _dbfsToDrawOld - _meterDecay / _updateRate;
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

    double dbfs = double.NegativeInfinity;
    private double _dbfsToDrawOld = double.NegativeInfinity;
    TimeSpan prevUpdateTime = TimeSpan.Zero;

    internal void UpdatePeak(object sender, TimeSpan elapsed)
    {
        if (queueOfDbfs.Count == 0)
            return;

        dbfs = queueOfDbfs.Dequeue();
        if (elapsed - prevUpdateTime > TimeSpan.FromSeconds(1))
        {
            prevUpdateTime = elapsed;
        }
        PeakVolumeCanvas.Invalidate();
    }
}