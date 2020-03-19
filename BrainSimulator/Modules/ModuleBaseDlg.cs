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
        private System.Timers.Timer timer;
        public int UpdateMS = 100;
        virtual public bool Draw(bool checkDrawTimer)
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
                    timer = new System.Timers.Timer(250);
                    timer.Elapsed += Timer_Elapsed;
                    timer.AutoReset = false;
                }
                timer.Stop();
                timer.Start();
                return false;
            }
            dt = DateTime.Now;
            if (timer != null) timer.Stop();
            return true;
        }

        //this picks up a final draw after 1/4 second 
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                Draw(false);
            });
        }
    }
}
