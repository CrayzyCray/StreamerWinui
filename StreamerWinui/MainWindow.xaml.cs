﻿using Microsoft.UI.Xaml;
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
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace StreamerLib
{
    public sealed partial class MainWindow : Window
    {
        public const string defaultFramerate = "64";
        public const string defaultIpToStream = "localhost";

        StreamSession streamSession;

        public MainWindow()
        {
            this.InitializeComponent();

            //this.Closed += (s, e) => { if (streamSession.StreamIsActive) { streamSession.stopStream(); } };//выключение корировщика при закрытии

            streamSession = new StreamSession();

            //заполнение codecComboBox
            List<string> userFriendlyCodecNames = new List<string>();
            foreach (var item in StreamSession.SupportedCodecs)
            {
                userFriendlyCodecNames.Add(item.UserFriendlyName);
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
            Debug.WriteLine(Environment.Version.ToString());
        }

        private void startStreamButton_Click(object sender, RoutedEventArgs e)
        {
            showConsoleCheckBox.IsEnabled = streamSession.StreamIsActive;
            framerateTextBlock.IsEnabled = streamSession.StreamIsActive;
            ipTextBlock.IsEnabled = streamSession.StreamIsActive;

            if (!streamSession.StreamIsActive)
            {
                string ipToStream = "localhost";

                int.TryParse(framerateTextBlock.Text, out int framerate);
                
                if (IPAddress.TryParse(ipTextBlock.Text, out IPAddress? ip))
                    ipToStream = ip.ToString();

                string codecName = StreamSession.SupportedCodecs[codecComboBox.SelectedIndex].Name;
                
                //streamSession.StartStream(IpToStream:ipToStream, framerate:framerate, codecName:codecName);
                startStreamButton.Content = "Stop";
            }
            else
            {
                streamSession.StopStream();
                startStreamButton.Content = "Start";
            }
                
        }
    }
}
