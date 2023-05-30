using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml.Controls;
using NAudio.CoreAudioApi;
using StreamerLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace StreamerWinui.UserControls;
public sealed partial class MixerChannel : UserControl
{
    public const float MinimumVolumeMeterLevel = -62;

    public WasapiAudioCapturingChannel WasapiAudioCapturingChannel { get; }
    public MMDevice Device => WasapiAudioCapturingChannel.MMDevice;
    public int Channels => WasapiAudioCapturingChannel.Channels;
    public event EventHandler OnDeleting;
    private MixerControl _mixerControl;
    const int _width = 352;
    const int _height = 70;

    public MixerChannel(WasapiAudioCapturingChannel wasapiAudioCapturingChannel, MixerControl mixerControl)
    {
        _mixerControl = mixerControl;

        WasapiAudioCapturingChannel = wasapiAudioCapturingChannel;
        WasapiAudioCapturingChannel.DataAvailable += _captureDataRecieved;
        
        _dbfsToDraw = new float[Channels];
        _dbfsLast = new float[Channels];

        for (int i = 0; i < Channels; i++)
        {
            _dbfsLast[i] = float.NegativeInfinity;
            _dbfsToDraw[i] = float.NegativeInfinity;
        }

        this.InitializeComponent();
        DeviceNameTextBlock.Text = WasapiAudioCapturingChannel.MMDevice.FriendlyName;
    }

    private void _captureDataRecieved(object sender, NAudio.Wave.WaveInEventArgs args)
    {
        float[] dbfs1 = new float[Channels];
        for (int i = 0; i < Channels; i++)
        {
            dbfs1[i] = LibUtil.GetPeakMultichannel(args.Buffer, args.BytesRecorded, Channels, i);
            
        }
        _dbfsToDraw = dbfs1;//bug here 1
        PeakVolumeCanvas.Invalidate();
    }

    private void DeleteButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        WasapiAudioCapturingChannel.StopRecording();
        OnDeleting.Invoke(this, null);
    }

    Windows.UI.Color VolumeMeterColor = Windows.UI.Color.FromArgb(128, 0, 255, 0);
    const int Thickness = 1;

    float[] _dbfsToDraw;
    float[] _dbfsLast = { float.NegativeInfinity };
    float _meterDecay = 24;
    TimeSpan _time = TimeSpan.Zero;
    List<float> list = new(64);
    private void Canvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
    {
        TimeSpan time = _mixerControl.Time;
        float decay = _meterDecay / ((float)(time - _time).TotalSeconds);
        
        _time = time;
        float dbfsPredicted;
        for (int i = 0; i < _dbfsToDraw.Length; i++)
        {
            dbfsPredicted = _dbfsLast[i] - decay;
            //Debug.Assert(list.Count < 16); list.Add(dbfsPredicted);
            if (dbfsPredicted > _dbfsToDraw[i])
                _dbfsToDraw[i] = dbfsPredicted;
        }
        _dbfsLast = _dbfsToDraw;
        DrawPeaksMultichannel(args.DrawingSession, _width, _height, _dbfsToDraw);
        
    }

    void DrawPeaksMultichannel(CanvasDrawingSession drawingSession, float width, float height, float[] dbfs)
    {
        float length = _height / dbfs.Length;
        for (int i = 0; i < dbfs.Length; i++)
        {
            var x = (-MinimumVolumeMeterLevel + dbfs[i]) * _width / -MinimumVolumeMeterLevel;
            if (x < 0)
                x = 0;
            else if (x > _width)
                x = _width;
            x -= Thickness;
            Vector2 p1 = new(x, 0 + length * i);
            Vector2 p2 = new(x, p1.Y + length);
            drawingSession.DrawLine(p1, p2, VolumeMeterColor, 1);
        }
    }

    public void Dispose()
    {
        WasapiAudioCapturingChannel.StopRecording();
    }

    TimeSpan prevUpdateTime = TimeSpan.Zero;

    private void VolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        float v = (float)(e.NewValue / (sender as Slider).Maximum);
        WasapiAudioCapturingChannel.Volume = v * v;
    }
}