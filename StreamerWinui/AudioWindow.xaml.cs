using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NAudio.CoreAudioApi;
using StreamerWinui.UserControls;
using System;
using System.Diagnostics;
using Windows.Foundation;
using WinRT;
using WinRT.Interop;

namespace StreamerWinui
{
    public sealed partial class AudioWindow : Window
    {
        private MMDevice mMDevice;
        private MMDeviceCollection mMDevices;
        private MMDeviceEnumerator mDeviceEnumerator = new();
        //private StreamSession _streamSession;
        private AppWindow m_AppWindow;

        const string DefaultPath = @"D:\\video\\img\\2.opus";

        public AudioWindow()
        {
            this.InitializeComponent();
            TrySetAcrylicBackdrop();
            m_AppWindow = GetAppWindowForCurrentWindow();

            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);

            m_AppWindow.Resize(new Windows.Graphics.SizeInt32 { 
                Width = 380, 
                Height = 500 });
            AddMixerChannel(new MixerChannel());
            FillComboBox();
        }

        public void FillComboBox()
        {
            mMDevices = mDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
            devicesComboBox.Items.Clear();
            foreach (var item in mMDevices)
                devicesComboBox.Items.Add(item.FriendlyName);
            devicesComboBox.SelectedIndex = 0;
        }

        public void AddMixerChannel(MixerChannel mixerChannel) => StackPanel.Children.Add(mixerChannel);

        private void PrintDebugInfo()
        {
            Debug.WriteLine(".NET " + Environment.Version.ToString());

            if (AppWindowTitleBar.IsCustomizationSupported())
                Debug.WriteLine("AppTitleBar customization is supported");
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

        private void devicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => mMDevice = mMDevices[devicesComboBox.SelectedIndex];

        private Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController m_acrylicController;
        private Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration m_configurationSource;

        bool TrySetAcrylicBackdrop()
        {
            if (!Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
                return false; // Acrylic is not supported on this system

            new WindowsSystemDispatcherQueueHelper().EnsureWindowsSystemDispatcherQueueController();

            // Hooking up the policy object
            m_configurationSource = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration();
            this.Activated += WindowActivated;
            this.Closed += WindowClosed;
            ((FrameworkElement)this.Content).ActualThemeChanged += WindowThemeChanged;

            // Initial configuration state.
            m_configurationSource.IsInputActive = true;
            SetConfigurationSourceTheme();

            m_acrylicController = new Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController();

            // Enable the system backdrop.
            // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
            m_acrylicController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
            m_acrylicController.SetSystemBackdropConfiguration(m_configurationSource);

            return true; // succeeded
        }

        private void WindowActivated(object sender, WindowActivatedEventArgs args) =>
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;

        private void WindowClosed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
            // use this closed window.
            if (m_acrylicController != null)
            {
                m_acrylicController.Dispose();
                m_acrylicController = null;
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
                case ElementTheme.Dark: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Default; break;
            }
        }

        private bool SetTitleBarColors()
        {
            // Check to see if customization is supported.
            // Currently only supported on Windows 11.
            if (!AppWindowTitleBar.IsCustomizationSupported())
                return false;

            if (m_AppWindow is null)
                m_AppWindow = GetAppWindowForCurrentWindow();

            var titleBar = m_AppWindow.TitleBar;

            // Set active window colors
            titleBar.ForegroundColor = Colors.White;
            titleBar.BackgroundColor = Colors.Green;
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonBackgroundColor = Colors.SeaGreen;
            titleBar.ButtonHoverForegroundColor = Colors.Gainsboro;
            titleBar.ButtonHoverBackgroundColor = Colors.DarkSeaGreen;
            titleBar.ButtonPressedForegroundColor = Colors.Gray;
            titleBar.ButtonPressedBackgroundColor = Colors.LightGreen;

            // Set inactive window colors
            titleBar.InactiveForegroundColor = Colors.Gainsboro;
            titleBar.InactiveBackgroundColor = Colors.SeaGreen;
            titleBar.ButtonInactiveForegroundColor = Colors.Gainsboro;
            titleBar.ButtonInactiveBackgroundColor = Colors.SeaGreen;
            return true;
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        private void UpdateDevicesListButton_Click(object sender, RoutedEventArgs e) => FillComboBox();

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            var m = new MixerChannel(mMDevices[devicesComboBox.SelectedIndex]);
            StackPanel.Children.Add(m);
        }

        private void CanvasControl_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            var rect = new Rect(0, 0, 10, 10);
            args.DrawingSession.DrawRectangle(rect, Colors.Black);
            Debug.WriteLine("Draw");
        }
    }
}
