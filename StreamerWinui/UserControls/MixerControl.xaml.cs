using Microsoft.UI.Xaml.Controls;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using StreamerLib;

namespace StreamerWinui.UserControls
{
    public sealed partial class MixerControl : UserControl
    {

        private List<MixerChannel> _devices = new();
        private StreamerLib.MasterChannel _masterChannel;

        public MixerControl(MasterChannel masterChannel)
        {
            this.InitializeComponent();
            _masterChannel = masterChannel;
            _masterChannel.StartMonitoring();
        }

        public void AddChannel(MMDevice device)
        {
            var channel = _masterChannel.AddChannel(device);
            var mixerChannel = new MixerChannel(channel);
            _devices.Add(mixerChannel);
            StackPanel.Children.Add(mixerChannel);
            mixerChannel.OnDeleting += MixerChannel_OnDeleting;
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
}
