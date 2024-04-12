//
// Copyright (c) Charles Simon. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System.Windows;

namespace BrainSimulator.Modules
{
    public partial class ModuleKBDebugDlg : ModuleBaseDlg
    {
        public ModuleKBDebugDlg()
        {
            InitializeComponent();
        }
        public override bool Draw(bool checkDrawTimer)
        {
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second
            if (!base.Draw(checkDrawTimer)) return false;

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
            Draw(true);
        }

    }
}
