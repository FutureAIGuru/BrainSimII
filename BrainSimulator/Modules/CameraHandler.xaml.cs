//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

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
using System.Windows.Shapes;

using Touchless.Vision.Camera;
using System.Windows.Threading;
using System.IO;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class CameraHandler : Window
    {
        Camera theCamera;
        DispatcherTimer dt;
        public CameraHandler()
        {
            InitializeComponent();
            if (CameraService.AvailableCameras.Count > 0)
                theCamera = CameraService.AvailableCameras[0];
            else
                this.Close();
            startCapturing();
        }
        public System.Drawing.Bitmap theBitMap1 = null;
        public System.Drawing.Bitmap theBitMap2 = null;

        private void Dt_Tick(object sender, EventArgs e)
        {
            var tmp = Convert(_latestFrame);
            if (tmp != null)
                image.Source = tmp;
            if (_latestFrame == null) return;
            if (theBitMap1 == null)
                theBitMap1 = (System.Drawing.Bitmap) _latestFrame.Clone();
            else if (theBitMap2 == null)
                theBitMap2 = (System.Drawing.Bitmap)_latestFrame.Clone();

        }

        private CameraFrameSource _frameSource;
        private static System.Drawing.Bitmap _latestFrame;
        private Camera CurrentCamera
        {
            get
            {
                return theCamera;// comboBoxCameras.SelectedItem as Camera;
            }
        }
        private void startCapturing()
        {
            try
            {
                Camera c = theCamera;
                setFrameSource(new CameraFrameSource(c));
                _frameSource.Camera.CaptureWidth = 640;//640;
                _frameSource.Camera.CaptureHeight = 360;// 480;
                _frameSource.Camera.Fps = 50;
                _frameSource.NewFrame += OnImageCaptured;
                _frameSource.StartFrameCapture();

                dt = new DispatcherTimer();
                dt.Interval = new TimeSpan(0, 0, 0, 0, 100);
                dt.Tick += Dt_Tick;
                dt.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void OnImageCaptured(Touchless.Vision.Contracts.IFrameSource frameSource, Touchless.Vision.Contracts.Frame frame, double fps)
        {
            _latestFrame = frame.Image;
        }

        public BitmapImage Convert(System.Drawing.Bitmap src)
        {
            if (src == null) return null;
            MemoryStream ms = new MemoryStream();
            ((System.Drawing.Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        private void setFrameSource(CameraFrameSource cameraFrameSource)
        {
            if (_frameSource == cameraFrameSource)
                return;

            _frameSource = cameraFrameSource;
        }

        bool moving = false;
        System.Windows.Point prevPosition = new System.Windows.Point(-1, -1);
        private void Window_MouseMove_1(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!moving)
                {
                    prevPosition = PointToScreen(e.GetPosition(this));
                    Mouse.Capture(this);
                    moving = true;
                }
                else
                {
                    System.Windows.Point currentPosition = PointToScreen(e.GetPosition(this));
                    Vector v = currentPosition - prevPosition;
                    this.Left += v.X;
                    this.Top += v.Y;
                    prevPosition = currentPosition;
                }
            }
            else
            {
                moving = false;
                Mouse.Capture(null);
            }

        }

    }
}
