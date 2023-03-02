using Microsoft.UI.Xaml.Controls;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StreamerWinui.UserControls
{
    public sealed partial class MixerChannelControl : UserControl
    {
        List<MixerChannel> _devices = new();

        public MixerChannelControl()
        {
            this.InitializeComponent();
        }

        public void AddChannel(MixerChannel mixerChannel)
        {
            StackPanel.Children.Add(mixerChannel);
            _devices.Add(mixerChannel);

            mixerChannel.OnDeleting += (s, e) =>
            {
                StackPanel.Children.Remove(mixerChannel);
                _devices.Remove(mixerChannel);
            };
        }

        public List<MMDevice> GetAvalibleDevices()
        {
            var allDevices = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
            var avalibleDevices = new List<MMDevice>();

            for (int i = 0; i < allDevices.Count; i++)
            {
                bool b = true;
                for (int j = 0; j < _devices.Count; j++)
                {
                    if (allDevices[i].ID == _devices[j].Device.ID)
                    {
                        b = false;
                        break;
                    }
                }
                if (b)
                    avalibleDevices.Add(allDevices[i]);
            }

            return avalibleDevices;
        }

        public void Dispose()
        {
            StackPanel.Children.Clear();
            _devices.ForEach(d=>d.Dispose());
            _devices.Clear();
        }
    }
}
