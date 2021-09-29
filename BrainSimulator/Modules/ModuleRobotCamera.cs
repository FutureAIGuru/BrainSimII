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

        //TODO input IP address

        //fill this method in with code which will execute
        //once for each cycle of the engine
        Bitmap bitmap1 = null;
        Object bitmapLock = new Object();
        WebClient wc = new WebClient();
        DispatcherTimer dt = new DispatcherTimer();
        IPAddress theIP = null;
        public string theIPString = "";

        public override void Fire()
        {
            Init();  //be sure to leave this here

            if (wc.IsBusy) return;
            if (theIP.Equals(new IPAddress(new byte[]{ 0, 0, 0, 0 }))) return;

            lock (bitmapLock)
            {
                //the intent here is that the bitmap is either null or contains a complete image
                //if the test is performed while the callback is in process, this lock should cause this to wait for the write to bitmap to complete
                if (bitmap1 != null)
                {
                    LoadImage(bitmap1);
                    bitmap1 = null;
                }
            }

            if (bitmap1 == null)
            {
                wc.DownloadDataAsync(new Uri("http://" + theIP.ToString()));

                dt.Stop();
                dt.Start();
            }

            //if you want the dlg to update, use the following code whenever any parameter changes
            // UpdateDialog();
        }

        private void webRequest_Timeout(object sender, EventArgs e)
        {
            (sender as DispatcherTimer).Stop();
            if (wc.IsBusy)
                wc.CancelAsync();
            bitmap1 = null;
        }

        private void LoadImage(Bitmap bitmap1)
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

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            if (na == null) return; //this is called the first time before the module actually exists
            foreach (Neuron n in na.Neurons1)
                n.Model = Neuron.modelType.Color;

            dt.Interval = TimeSpan.FromSeconds(1);
            dt.Tick += webRequest_Timeout;

            wc.DownloadDataCompleted += Wc_DownloadDataCompleted;

            if (theIP == null)
            {
                theIP = new IPAddress(new byte[] { 0, 0, 0, 0 });
            }
        }

        private void Wc_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                using (MemoryStream mem = new MemoryStream(e.Result))
                {
                    lock (bitmapLock)
                    {
                        try
                        {//you may have gotten to a website which isn't an image...just ignore it
                            bitmap1 = (Bitmap)System.Drawing.Image.FromStream(mem);
                        }
                        catch
                        {
                        }
                    }
                }
            }
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
