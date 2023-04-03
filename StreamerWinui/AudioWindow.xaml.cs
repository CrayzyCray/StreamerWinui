using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StreamerLib;
using StreamerWinui.UserControls;
using System;
using Windows.Graphics;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT;
using System.IO;

namespace StreamerWinui
{
    public sealed partial class AudioWindow : Window
    {
        private AppWindow m_AppWindow;
        private StreamerLib.StreamController _streamController = new();
        private MixerControl _mixerChannelControl;

        public static readonly SizeInt32 DefaultWindowSize = new(380, 500);

        private string _folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private string _fileName = "Recording1";
        const string _defaultFileExtension = ".opus";

        public AudioWindow()
        {
            InitializeComponent();
            CreateMixerChannelControl();
            TrySetMicaBackdrop();

            m_AppWindow = GetAppWindowForCurrentWindow();
            m_AppWindow.Resize(DefaultWindowSize);

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            DotNetTextBlock.Text = ".NET " + Environment.Version;

            FillDevicesComboBox();
            Closed += WindowClosed;
        }

        public void FillDevicesComboBox() => devicesComboBox.ItemsSource = _mixerChannelControl.GetAvalibleDevices();

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_streamController.StreamIsActive)
            {
                var path = Path.Combine(_folderPath, _fileName + _defaultFileExtension);

                if (!Directory.Exists(_folderPath))
                    throw new Exception("Folder path not exist");

                _streamController.StartStream();

                if (_streamController.StreamIsActive == false)
                    return;

                _streamController.AddClientAsFile(path);
            }
            else
            {
                _streamController.StopStream();
            }

            StartButtonText.Text = _streamController.StreamIsActive ? "Stop" : "Start";
        }

        private void CreateMixerChannelControl()
        {
            _mixerChannelControl = new(_streamController.MasterChannel);
            MixerChannelControlContainer.Children.Add(_mixerChannelControl);
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
            ((FrameworkElement)this.Content).ActualThemeChanged += WindowThemeChanged;

            // Initial configuration state.
            m_configurationSource.IsInputActive = true;
            SetConfigurationSourceTheme();

            m_micaController = new Microsoft.UI.Composition.SystemBackdrops.MicaController();
            m_micaController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
            m_micaController.SetSystemBackdropConfiguration(m_configurationSource);

            return true;
        }

        private async void PickFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a folder picker
            FolderPicker openPicker = new Windows.Storage.Pickers.FolderPicker();

            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            nint hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            // Initialize the folder picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            // Set options for your folder picker
            openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            openPicker.FileTypeFilter.Add("*");

            // Open the picker for the user to pick a folder
            StorageFolder folder = await openPicker.PickSingleFolderAsync();

            if (folder != null)
                _folderPath = folder.Path;
        }

        private void WindowActivated(object sender, WindowActivatedEventArgs args) =>
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;

        private void WindowClosed(object sender, WindowEventArgs args)
        {
            if (_mixerChannelControl != null)
                _mixerChannelControl.Dispose();
            if (m_micaController != null)
                m_micaController.Dispose();
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
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        private void devicesComboBox_DropDownOpened(object sender, object e)
        {
            FillDevicesComboBox();
        }

        private void devicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(devicesComboBox.SelectedItem is NAudio.CoreAudioApi.MMDevice device)
            {
                _mixerChannelControl.AddChannel(device);
                devicesComboBox.SelectedIndex = -1;
            }
        }
    }
}
