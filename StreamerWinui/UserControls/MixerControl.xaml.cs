using Microsoft.UI.Xaml.Controls;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using StreamerLib;
using System.Diagnostics;

namespace StreamerWinui.UserControls
{
    public sealed partial class MixerControl : UserControl
    {

        private List<MixerChannel> _devices = new();
        private StreamerLib.MasterChannel _masterChannel;

        private event PeakUpdateEvent _peakUpdateTick;
        private System.Threading.Timer _timer;
        private const int _peakUpdateRate = 30;
        private Stopwatch _stopwatch = new();

        public MixerControl(MasterChannel masterChannel)
        {
            this.InitializeComponent();
            _masterChannel = masterChannel;
            _masterChannel.StartMonitoring();
            _timer = new(TimerTick, null, TimeSpan.Zero, new TimeSpan(1000 * 10000 / _peakUpdateRate));
            _stopwatch.Start();
        }

        private void TimerTick(object s)
        {
            _peakUpdateTick?.Invoke(this, _stopwatch.Elapsed);
        }

        public void AddChannel(MMDevice device)
        {
            var channel = _masterChannel.AddChannel(device);
            var mixerChannel = new MixerChannel(channel);
            _devices.Add(mixerChannel);
            StackPanel.Children.Add(mixerChannel);
            mixerChannel.OnDeleting += MixerChannel_OnDeleting;
            _peakUpdateTick += mixerChannel.UpdatePeak;
        }

        private void MixerChannel_OnDeleting(object sender, System.EventArgs e)
        {
            StackPanel.Children.Remove(sender as MixerChannel);
            _devices.Remove(sender as MixerChannel);
            _masterChannel.RemoveChannel((sender as MixerChannel).WasapiAudioCapturingChannel);
        }

        public List<MMDevice> GetAvalibleDevices()
        {
            var allDevices = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
            var availableDevices = new List<MMDevice>();

            for (int i = 0; i < allDevices.Count; i++)
            {
                bool isAvailable = true;
                for (int j = 0; j < _devices.Count; j++)
                {
                    if (allDevices[i].ID == _devices[j].Device.ID)
                    {
                        isAvailable = false;
                        break;
                    }
                }
                if (isAvailable)
                    availableDevices.Add(allDevices[i]);
            }

            return availableDevices;
        }

        public void Dispose()
        {
            _devices.Clear();
            _masterChannel.Dispose();
        }
    }

    public delegate void PeakUpdateEvent(object sender, TimeSpan elapsed);
}
