// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StreamerWinui.UserControls
{
    public sealed partial class MixerChannel : UserControl
    {
        public MMDevice Device { get; }
        public double MinimumVolumeMeterLevel { get; set; } = -62;

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

            float max = 0;
            var buffer = new WaveBuffer(e.Buffer);

            for (int index = 0; index < e.BytesRecorded / 4; index++)
            {
                var sample = buffer.FloatBuffer[index];
                if (sample < 0) sample = -sample;
                if (sample > max) max = sample;
            }

            double dbfs = 20 * Math.Log10(max);
            SetVolumeMeterLevel(dbfs);
        }

        public MixerChannel() => this.InitializeComponent();
    }
}
