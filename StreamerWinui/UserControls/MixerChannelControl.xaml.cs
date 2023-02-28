using Microsoft.UI.Xaml.Controls;

namespace StreamerWinui.UserControls
{
    public sealed partial class MixerChannelControl : UserControl
    {
        public MixerChannelControl()
        {
            this.InitializeComponent();
        }

        public void AddChannel(MixerChannel mixerChannel)
        {
            mixerChannel.OnDeleting += (s, e) =>
            {
                StackPanel.Children.Remove(mixerChannel);
            };
            StackPanel.Children.Add(mixerChannel);
        }
    }
}
