using Microsoft.UI.Xaml.Controls;
using NAudio.CoreAudioApi;
using System.Collections.Generic;

namespace StreamerWinui.UserControls
{
    public sealed partial class MixerControl : UserControl
    {
        public StreamerLib.StreamController StreamController { get; }

        private List<MixerChannel> _devices = new();
        private StreamerLib.MasterChannel _masterChannel;

        public MixerControl(StreamerLib.StreamController streamController)
        {
            StreamController = streamController;
            _masterChannel = streamController.MasterChannel;
            this.InitializeComponent();
        }

        public void AddChannel(MMDevice device)
        {
            MixerChannel mixerChannel = new(device, _masterChannel.FrameSizeInBytes);
            _masterChannel.AddChannel(mixerChannel.WasapiAudioCapturingChannel);

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
                    availableDevices.Add(allDevices[i]);
            }

            return availableDevices;
        }

        public void Dispose()
        {
            StackPanel.Children.Clear();
            _devices.ForEach(d=>d.Dispose());
            _devices.Clear();
        }

        public void StartStreaming()
        {

        }
    }
}
