using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio.CoreAudioApi;
using StreamerLib;

namespace WpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string DefaultPath = @"D:\video\StreamerLibTests\2.opus";
        
        private MMDevice mMDevice;
        private MMDeviceCollection mMDevices;
        private MMDeviceEnumerator mDeviceEnumerator;
        private StreamSession _streamSession;
        
        public MainWindow()
        {
            InitializeComponent();
            mDeviceEnumerator = new();
            _streamSession = new();
            FillComboBox();
            devicesComboBox.SelectedIndex = 1;
            pathTextBox.Text = DefaultPath;
        }
        
        private void devicesComboBox_DropDownOpened(object sender, object e)
        {
            
        }

        void FillComboBox()
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
            
            string path;
            if (pathTextBox.Text.Length == 0)
                path = DefaultPath;
            else
                path = pathTextBox.Text;
            _streamSession.AudioRecording = true;
            _streamSession.MMDevice = mMDevice;
            _streamSession.StartStream();
            _streamSession.AddClientAsFile(path);
            StartButton.Content = "Stop";
        }

        private void devicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            mMDevice = mMDevices[devicesComboBox.SelectedIndex];
    }
}