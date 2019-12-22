//
// Copyright (c) [Name]. All rights reserved.  
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
using System.Windows.Threading;

namespace BrainSimulator.Modules
{
    public partial class ModuleKBDebugDlg : ModuleBaseDlg
    {
        public ModuleKBDebugDlg()
        {
            InitializeComponent();
        }
        public override bool Draw()
        {
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second
            if (!base.Draw()) return false;

            //use a line like this to gain access to the parent's public variables
            ModuleKBDebug parent = (ModuleKBDebug)base.ParentModule;
            string theContent = "";
            lock (parent.history)
            {
                foreach (string s in parent.history)
                {
                    theContent += s + "\r\n";
                }
            }
            if (theScroller.VerticalOffset == theScroller.ScrollableHeight)
            { theScroller.ScrollToBottom(); }

            theTextBox.Text = theContent;
            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw();
        }

    }
}
