using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BrainSimulator
{
    public class ModuleBaseDlg : Window
    {
        public ModuleBase Parent1;
        private DateTime dt;

        virtual public bool Draw()
        {
            //only actually update the screen every 100ms
            TimeSpan ts = DateTime.Now - dt;
            if (ts < new TimeSpan(0, 0, 0, 0, 100)) return false;
            dt = DateTime.Now;
            return true;
        }
    }
}
