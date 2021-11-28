//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleRobotCamera : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleRobotCamera()
        {
            minHeight = 2;
            maxHeight = 500;
            minWidth = 2;
            maxWidth = 500;
        }

        //TODO gte IP from UDP broadcast

        Bitmap bitmap1 = null;
        Object bitmapLock = new Object();
        WebClient wc = new WebClient();
        DispatcherTimer dt = new DispatcherTimer();
        IPAddress theIP = null;
        public string theIPString = "";

        [XmlIgnore]
        public BitmapImage theBitmap = null;

        public override void Fire()
        {
            try
            {
                Init();  //be sure to leave this here
                if (wc.IsBusy)
                {
                    //wc.CancelAsync();
                    return;
                }
                if (theIP.Equals(new IPAddress(new byte[] { 0, 0, 0, 0 }))) return;

                lock (bitmapLock)
                {
                    //the intent here is that the bitmap is either null or contains a complete image
                    //if the test is performed while the callback is in process, the lock should cause this to wait for the write to bitmap to complete
                    if (bitmap1 != null)
                    {
                        LoadImage(bitmap1);
                        na.GetNeuronAt(0, 0).SetValueInt(0x00ff00);
                        bitmap1 = null;
                    }
                }

                if (bitmap1 == null)
                {
                    downloadProgress = 0;
                    wc.DownloadDataAsync(new Uri("http://" + theIP.ToString()));
                    na.GetNeuronAt(0, 0).SetValueInt(0xffff00);
                    dt.Start();
                }

                //if you want the dlg to update, use the following code whenever any parameter changes
                UpdateDialog();

            }
            catch (Exception e)
            {
                throw;
            }
        }


        private void Wc_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            try
            {
                dt.Stop();
                lock (bitmapLock)
                {
                    if (e.Error == null && !e.Cancelled)
                    {
                        na.GetNeuronAt(0, 0).SetValueInt(0x00ff00);
                        using (MemoryStream mem = new MemoryStream(e.Result))
                        {
                            try
                            {//you may have gotten to a website which isn't an image...just ignore it
                                bitmap1 = (Bitmap)System.Drawing.Image.FromStream(mem);
                                bitmap1.RotateFlip(RotateFlipType.Rotate270FlipNone);
                                mem.Position = 0;
                                theBitmap = new BitmapImage();
                                theBitmap.BeginInit();
                                theBitmap.Rotation = Rotation.Rotate270;
                                theBitmap.StreamSource = mem;
                                theBitmap.CacheOption = BitmapCacheOption.OnLoad;
                                theBitmap.EndInit();
                                theBitmap.Freeze();

                            }
                            catch
                            {
                            }
                        }
                    }
                    else
                    { }
                }

            }
            catch (Exception e1)
            {

                throw;
            }
        }
        private void webRequest_Timeout(object sender, EventArgs e)
        {
            dt.Stop();
            try
            {
                na.GetNeuronAt(0, 0).SetValueInt(0xff0000);
                //System.Diagnostics.Debug.WriteLine("ModuleRobotCamera:WebClient Request timed out.");
                if (wc.IsBusy && downloadProgress == 0)
                {
                    lock (bitmapLock)
                    {
                        wc.CancelAsync();
                    }
                }
                bitmap1 = null;
            }
            catch (Exception e1)
            {

                throw;
            }
        }

        private void LoadImage(Bitmap bitmap1)
        {

            try
            {
                float vRatio = bitmap1.Height / (float)na.Height;
                float hRatio = bitmap1.Width / (float)na.Width;
                for (int i = 0; i < na.Height; i++)
                    for (int j = 0; j < na.Width; j++)
                    {
                        Neuron n = na.GetNeuronAt(j, i);
                        int x = (int)(j * (bitmap1.Width - 1) / (float)(na.Width - 1));
                        int y = (int)(i * (bitmap1.Height - 1) / (float)(na.Height - 1));

                        System.Drawing.Color c = bitmap1.GetPixel(x, y);
                        int val = Utils.ColorToInt(c);
                        n.LastChargeInt = val;
                    }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            if (na == null) return; //this is called the first time before the module actually exists
            foreach (Neuron n in na.Neurons1)
                n.Model = Neuron.modelType.Color;

            dt = new DispatcherTimer();
            dt.Interval = new TimeSpan(0, 0, 1);// TimeSpan.FromSeconds(1);
            dt.Tick += webRequest_Timeout;

            if (wc.IsBusy) wc.CancelAsync();
            wc.DownloadDataCompleted += Wc_DownloadDataCompleted;
            wc.DownloadProgressChanged += Wc_DownloadProgressChanged;

            if (theIP == null)
            {
                theIP = new IPAddress(new byte[] { 0, 0, 0, 0 });
            }
        }

        long downloadProgress = 0;
        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            downloadProgress = e.BytesReceived;
            //throw new NotImplementedException();
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
            theIPString = theIP.ToString();
        }
        public override void SetUpAfterLoad()
        {
            Initialize();
            if (theIPString != "")
                theIP = IPAddress.Parse(theIPString);
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (na == null) return; //this is called the first time before the module actually exists
            foreach (Neuron n in na.Neurons1)
                n.Model = Neuron.modelType.Color;
        }

        public override MenuItem GetCustomMenuItems()
        {
            StackPanel s2 = new StackPanel { Orientation = Orientation.Vertical };

            StackPanel s = new StackPanel { Orientation = Orientation.Horizontal };
            s.Children.Add(new Label { Content = "IP:", Width = 60, HorizontalContentAlignment = HorizontalAlignment.Right });
            TextBox tb1 = new TextBox { Name = "x", Width = 100, Height = 20, Text = theIP.ToString() };
            tb1.TextChanged += Tb1_TextChanged;
            s.Children.Add(tb1);
            s2.Children.Add(s);

            return new MenuItem { Header = s2, StaysOpenOnClick = true };
        }

        private void Tb1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                try
                {
                    theIP = IPAddress.Parse(tb.Text);
                    tb.Background = new SolidColorBrush(Colors.LightGreen);
                }
                catch
                {
                    tb.Background = new SolidColorBrush(Colors.Pink);
                }
            }
        }
    }
}
