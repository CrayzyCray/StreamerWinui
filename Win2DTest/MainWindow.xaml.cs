// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Threading;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Win2DTest
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        public void Draw()
        {
            myCanvas.Invalidate();
        }

        float radius = 60;
        float various = 40;
        int _framesCount = 0;
        Windows.UI.Color PrimaryTextColor = Microsoft.UI.Colors.White;

        private void myCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            _framesCount++;
            var mul = Math.Sin(_framesCount * 0.05);
            var ds = args.DrawingSession;
            ds.DrawCircle(System.Numerics.Vector2.Zero, radius - (float)( various * mul), Colors.Red, 5f);
            ds.DrawText(radius.ToString(), 100, 100, PrimaryTextColor);
            ds.DrawText("Frames: "+_framesCount, 0, 0, PrimaryTextColor);
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var v = (float)e.NewValue * 10;
            if (v >= various)
                radius = v;
        }

        private void myCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            TimerCallback timerCallback = new TimerCallback((obj) => Draw());
            Timer timer = new Timer(timerCallback, null, 0, 1000/144);
        }
    }
}
