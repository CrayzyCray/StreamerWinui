using Microsoft.UI.Xaml.Controls;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;

namespace StreamerWinui.UserControls
{
    public sealed partial class MixerChannel : UserControl
    {
        public const double MinimumVolumeMeterLevel = -62;

        public MMDevice Device { get; }
        public event EventHandler OnDeleting;
        private WasapiCapture _audioCapture;

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

        public bool SetVolumeMeterLevel(double dbfs)
        {
            if (DispatcherQueue == null)
                return false;

            return DispatcherQueue.TryEnqueue(() =>
            {
                var width = (-MinimumVolumeMeterLevel + dbfs) * Canvas.ActualWidth / -MinimumVolumeMeterLevel;
                if (width < 0)
                    width = 0;
                else if (width > Canvas.ActualWidth)
                    width = Canvas.ActualWidth;

                Rect.Width = width;
            });
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
    }
}
