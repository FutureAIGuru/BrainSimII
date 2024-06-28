//
// Copyright (c) Charles Simon. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BrainSimulator.Modules
{
    public class ModuleBaseDlg : Window
    {
        public ModuleBase ParentModule;
        private DateTime dt;
        private DispatcherTimer timer;
        public int UpdateMS = 100;

        public virtual bool Draw(bool checkDrawTimer)
        {
            if (!checkDrawTimer) return true;
            //only actually update the screen every 100ms
            TimeSpan ts = DateTime.Now - dt;
            if (ts < new TimeSpan(0, 0, 0, 0, 100))
            {
                //if we're not drawing this time, start a timer which will do a final draw
                //after a 1/4 second of inactivity
                if (timer == null)
                {
                    timer = new DispatcherTimer();
                    timer.Interval = new TimeSpan(0, 0, 0, 0, 250);
                    timer.Tick += Timer_Tick;
                }
                timer.Stop();
                timer.Start();
                return false;
            }
            dt = DateTime.Now;
            if (timer != null) timer.Stop();
            return true;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            if (Application.Current == null) return;
            if (this != null)
                Draw(false);
        }

        //this picks up a final draw after 1/4 second
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
            if (Application.Current == null) return;
            if (this != null)
                Draw(false);
        }

        public Bitmap theBitMap1 = null;
        public Bitmap theBitMap2 = null;

        public void GetBitMap()
        {
            System.Windows.Size size = new System.Windows.Size(ActualWidth, ActualHeight);
            Measure(size);
            Arrange(new Rect(size));
            // Create a render bitmap and push the surface to it
            RenderTargetBitmap renderBitmap =
              new RenderTargetBitmap(
                (int)size.Width,
                (int)size.Height,
                96d,
                96d,
                PixelFormats.Default);
            renderBitmap.Render(this);

            //Convert the RenderBitmap to a real bitmap
            MemoryStream stream = new MemoryStream();
            BitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            encoder.Save(stream);

            if (theBitMap1 == null)
                theBitMap1 = new Bitmap(stream);
            else if (theBitMap2 == null)
                theBitMap2 = new Bitmap(stream);
            //((Module3DSim)ParentModule).renderDone = true;
        }
    }
}
