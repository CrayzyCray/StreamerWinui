﻿using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NAudio.CoreAudioApi;
using StreamerWinui.UserControls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;
using WinRT;
using WinRT.Interop;

namespace StreamerWinui
{
    public sealed partial class AudioWindow : Window
    {
        //private StreamSession _streamSession;
        private AppWindow m_AppWindow;

        public readonly SizeInt32 DefaultWindowSize = new SizeInt32(380, 500);

        const string DefaultPath = @"D:\\video\\img\\2.opus";

        public AudioWindow()
        {
            InitializeComponent();
            TrySetMicaBackdrop();
            m_AppWindow = GetAppWindowForCurrentWindow();
            m_AppWindow.Resize(DefaultWindowSize);

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            FillDevicesComboBox();
        }
        MMDeviceCollection mMDevices;

        public async void FillDevicesComboBox()
        {
            devicesComboBox.ItemsSource = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
        }

        public void AddMixerChannel(MixerChannel mixerChannel) => MixerChannelContainer.AddChannel(mixerChannel);

        private void UpdateDevicesListButton_Click(object sender, RoutedEventArgs e) => FillDevicesComboBox();

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            var m = new MixerChannel(devicesComboBox.SelectedItem as MMDevice);
            MixerChannelContainer.AddChannel(m);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            //if (_streamSession.StreamIsActive)
            //{
            //    _streamSession.StopStream();
            //    StartButton.Content = "Start";
            //    return;
            //}

            //string Path;
            //if (pathTextBox.Text.Length == 0)
            //    Path = DefaultPath;
            //else
            //    Path = pathTextBox.Text;
            //_streamSession.AudioRecording = true;
            //_streamSession.MMDevice = mMDevice;
            //_streamSession.StartStream();
            //_streamSession.AddClientAsFile(Path);
            //StartButton.Content = "Stop";
        }

        private void PrintDebugInfo()
        {
            Debug.WriteLine(".NET " + Environment.Version.ToString());

            if (AppWindowTitleBar.IsCustomizationSupported())
                Debug.WriteLine("AppTitleBar customization is supported");
        }

        private Microsoft.UI.Composition.SystemBackdrops.MicaController m_micaController;
        private Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration m_configurationSource;

        bool TrySetMicaBackdrop()
        {
            if (!Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
                return false;

            new WindowsSystemDispatcherQueueHelper().EnsureWindowsSystemDispatcherQueueController();

            // Hooking up the policy object
            m_configurationSource = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration();
            Activated += WindowActivated;
            Closed += WindowClosed;
            ((FrameworkElement)this.Content).ActualThemeChanged += WindowThemeChanged;

            // Initial configuration state.
            m_configurationSource.IsInputActive = true;
            SetConfigurationSourceTheme();

            m_micaController = new Microsoft.UI.Composition.SystemBackdrops.MicaController();
            m_micaController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
            m_micaController.SetSystemBackdropConfiguration(m_configurationSource);

            return true;
        }

        private void WindowActivated(object sender, WindowActivatedEventArgs args) =>
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;

        private void WindowClosed(object sender, WindowEventArgs args)
        {
            if (m_micaController != null)
            {
                m_micaController.Dispose();
                m_micaController = null;
            }
            this.Activated -= WindowActivated;
            m_configurationSource = null;
        }

        private void WindowThemeChanged(FrameworkElement sender, object args)
        {
            if (m_configurationSource != null)
                SetConfigurationSourceTheme();
        }

        private void SetConfigurationSourceTheme()
        {
            switch (((FrameworkElement)this.Content).ActualTheme)
            {
                case ElementTheme.Dark: 
                    m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Dark;
                    break;
                case ElementTheme.Light: 
                    m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Light;
                    break;
                case ElementTheme.Default: 
                    m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Default;
                    break;
            }
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        private void devicesComboBox_DropDownOpened(object sender, object e)
        {
            FillDevicesComboBox();
        }
    }
}
