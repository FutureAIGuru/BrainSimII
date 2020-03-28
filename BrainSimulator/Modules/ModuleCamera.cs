//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Touchless.Vision.Camera;
using System.Windows.Threading;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;


namespace BrainSimulator.Modules
{
    public class ModuleCamera : ModuleBase
    {
        Camera theCamera;
        bool running = false; //this is needed to force a camera initialize even if the module as a whole has been initialized
        DispatcherTimer dt;
        private CameraFrameSource _frameSource;
        private static System.Drawing.Bitmap _latestFrame;


        [XmlIgnore]
         System.Drawing.Bitmap theBitMap1 = null;
        [XmlIgnore]
         System.Drawing.Bitmap theBitMap2 = null;
        [XmlIgnore]
        public BitmapImage theDlgBitMap = null;

     
        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            if (!running) Initialize();
            if (theBitMap1 == null) return;
            na.GetBounds(out int X1, out int Y1, out int X2, out int Y2);
            float ratio = theBitMap1.Width / (X2 - X1);
            float ratio2 = theBitMap1.Height / (Y2 - Y1);
            if (ratio2 < ratio) ratio = ratio2;

            for (int i = 0; i < na.Width; i++)
                for (int j = 0; j < na.Height; j++)
                {
                    Neuron n = na.GetNeuronAt(i,j);
                    int x = (int)(i* ratio);
                    int y = (int)(j* ratio);
                    if (x >= theBitMap1.Width) break;
                    if (y >= theBitMap1.Height) break;
                    System.Drawing.Color c = theBitMap1.GetPixel(x, y);
                    System.Windows.Media.Color c1 = new System.Windows.Media.Color
                    { A = c.A, R = c.R, G = c.G, B = c.B };
                    int theColor = Utils.ColorToInt(c1);

                    if (theColor != 0 && theColor != 303)
                        n.SetValueInt(theColor);
                    else
                        n.SetValueInt(0);
                }
            theBitMap1 = null;
            UpdateDialog();
        }

        public override void Initialize()
        {
            if (CameraService.AvailableCameras.Count > 0)
                theCamera = CameraService.AvailableCameras[0];
            else
            {
                MessageBox.Show("No camera found");
                return;
            }
            startCapturing();
            for (int i = 0; i < na.NeuronCount; i++)
            {
                na.GetNeuronAt(i).Model = Neuron.modelType.Color;
                na.GetNeuronAt(i).SetValue(0);
            }
            Application.Current.Dispatcher.Invoke((Action)delegate { StartTimer(); });
            running = true;
        }

        private void StartTimer()
        {
            dt = new DispatcherTimer();
            dt.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dt.Tick += Dt_Tick;
            dt.Start();

        }
        private void Dt_Tick(object sender, EventArgs e)
        {
            if (_latestFrame == null) return;
            theDlgBitMap = Convert(_latestFrame);
            if (theBitMap1 == null)
                theBitMap1 = (System.Drawing.Bitmap)_latestFrame.Clone();
            else if (theBitMap2 == null)
                theBitMap2 = (System.Drawing.Bitmap)_latestFrame.Clone();
        }

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
                if (_frameSource == null)
                {
                    setFrameSource(new CameraFrameSource(c));
                    _frameSource.Camera.CaptureWidth = 640;//640;
                    _frameSource.Camera.CaptureHeight = 360;// 480;
                    _frameSource.Camera.Fps = 50;
                    _frameSource.NewFrame += OnImageCaptured;
                    _frameSource.StartFrameCapture();
                }
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
    }
}
