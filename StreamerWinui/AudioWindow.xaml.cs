using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using NAudio.Wasapi;
using NAudio.CoreAudioApi;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StreamerWinui
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AudioWindow : Window
    {
        private MMDevice mMDevice;
        private MMDeviceCollection mMDevices;
        private MMDeviceEnumerator mDeviceEnumerator;
        private StreamSession _streamSession;

        const string DefaultPath = @"D:\\video\\img\\2.opus";

        public AudioWindow()
        {
            this.InitializeComponent();
            mDeviceEnumerator = new();
            _streamSession = new();
        }

        private void devicesComboBox_DropDownOpened(object sender, object e)
        {
            mMDevices = mDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
            devicesComboBox.Items.Clear();
            foreach (var item in mMDevices)
                devicesComboBox.Items.Add(item.FriendlyName);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (_streamSession.StreamIsActive)
            {
                _streamSession.StopStream();
                StartButton.Content = "Start";
                return;
            }
            
            string Path;
            if (pathTextBox.Text.Length == 0)
                Path = DefaultPath;
            else
                Path = pathTextBox.Text;
            _streamSession.AudioRecording = true;
            _streamSession.MMDevice = mMDevice;
            _streamSession.StartStream();
            _streamSession.AddClientAsFile(Path);
            StartButton.Content = "Stop";
        }

        private void devicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            mMDevice = mMDevices[devicesComboBox.SelectedIndex];
    }
}
