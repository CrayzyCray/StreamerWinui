using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace StreamerWinui
{
    public sealed partial class MainWindow : Window
    {
        const double defaultFramerate = 30;
        const string defaultIpToStream = "192.168.0.115";

        StreamSession streamSession;

        public MainWindow()
        {
            this.InitializeComponent();

            //выключение корировщика при закрытии
            this.Closed += (s, e) => { if (streamSession.StreamIsActive) { streamSession.stopStream(); } };

            streamSession = new StreamSession();

            //заполнение codecComboBox
            List<string> userFriendlyCodecNames = new List<string>();
            foreach (var item in streamSession.supportedCodecs)
            {
                userFriendlyCodecNames.Add(item.userFriendlyName);
            }
            codecComboBox.ItemsSource = userFriendlyCodecNames;
            codecComboBox.SelectedIndex = 0;

            //кастомный тайтлбар
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(appTitleBar);

            //установка размера окна
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 240, Height = 300 });

            StreamSession.inicialize();
            //startStreamButton_Click(null, null);
        }

        private void startStreamButton_Click(object sender, RoutedEventArgs e)
        {
            showConsoleCheckBox.IsEnabled = streamSession.StreamIsActive;
            framerateTextBlock.IsEnabled = streamSession.StreamIsActive;
            ipTextBlock.IsEnabled = streamSession.StreamIsActive;

            if (!streamSession.StreamIsActive)
            {
                double framerate = defaultFramerate;
                string ipToStream = defaultIpToStream;

                if (framerateTextBlock.Text != "")
                    try
                    {
                        framerate = Convert.ToDouble(framerateTextBlock.Text);
                    }
                    catch { }

                if (ipTextBlock.Text != "")
                    ipToStream = ipTextBlock.Text;

                string codec = streamSession.supportedCodecs[codecComboBox.SelectedIndex].name;
                streamSession.startStream(codec, framerate, ipToStream, showConsoleCheckBox.IsChecked.GetValueOrDefault());
                
                startStreamButton.Content = "Stop";
            }
            else
            {
                streamSession.stopStream();
                startStreamButton.Content = "Start";
            }
                
        }
    }
}
