using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;

namespace StreamerWinui.UserControls
{
    public sealed partial class MixerChannel : UserControl
    {
        public const double MinimumVolumeMeterLevelDbfs = -62;

        public MMDevice Device { get; }
        public event EventHandler OnDeleting;
        private WasapiCapture _audioCapture;
        const int _width = 352;
        const int _height = 70;

        public MixerChannel(MMDevice mmDevice)
        {
            if (mmDevice.DataFlow == DataFlow.All)
                throw new Exception("Wrong mmDevice.DataFlow");

            _audioCapture = (mmDevice.DataFlow == DataFlow.Capture)
                ? new WasapiCapture(mmDevice)
                : new WasapiLoopbackCapture(mmDevice);

            Device = mmDevice;
            this.InitializeComponent();
            _audioCapture.StartRecording();
            _audioCapture.DataAvailable += _captureDataRecieved;
            DeviceNameTextBlock.Text = mmDevice.FriendlyName;
        }

        public void SetVolumeMeterLevel(double dbfs)
        {
            this.dbfs = dbfs;
            VolumeCanvas.Invalidate();
        }

        private void _captureDataRecieved(object sender, NAudio.Wave.WaveInEventArgs e)
        {
            //if (_capture.CaptureState == CaptureState.Stopped)
            //    return;

            var buffer = new WaveBuffer(e.Buffer);
            float peak = 0;

            for (int index = 0; index < e.BytesRecorded / 4; index++)
            {
                var sample = buffer.FloatBuffer[index];
                if (sample < 0) sample = -sample;
                if (sample > peak) peak = sample;
            }

            double dbfs = 20 * Math.Log10(peak);
            SetVolumeMeterLevel(dbfs);
        }

        public MixerChannel() => this.InitializeComponent();

        private void DeleteButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            _audioCapture.Dispose();
            OnDeleting.Invoke(this, null);
        }

        Windows.Foundation.Rect rect;
        Windows.UI.Color VolumeMeterColor = Colors.Green;
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
            _audioCapture.Dispose();
        }
    }
}
