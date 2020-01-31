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
using System.Windows.Threading;


namespace BrainSimulator.Modules
{
    public class ModuleBaseDlg : Window
    {
        public ModuleBase ParentModule;
        private DateTime dt;
        private DispatcherTimer timer;
        public int UpdateMS = 100;
        virtual public bool Draw()
        {
            //only actually update the screen every 100ms
            TimeSpan ts = DateTime.Now - dt;
            if (ts < new TimeSpan(0, 0, 0, 0, UpdateMS))
            {
                //if we're not drawing this time, start a timer which will do a final draw
                //after a 1/4 second of anactivity
                timer = new DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 0, 0, 250);
                timer.Tick += new EventHandler(Timer_Tick);
                //timer.Start();
                return false;
            }
            dt = DateTime.Now;
            if (timer != null) timer.Stop();
            return true;
        }

        //this picks up a final draw after 1/4 second 
        private void Timer_Tick(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate {
                timer.Stop();
            });

            Application.Current.Dispatcher.Invoke((Action)delegate { Draw(); });
        }
    }
}
