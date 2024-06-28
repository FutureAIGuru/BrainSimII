//
// Copyright (c) Charles Simon. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System.Windows;

namespace BrainSimulator.Modules
{
    public partial class ModuleBoundaryDescriptionDlg : ModuleBaseDlg
    {
        public ModuleBoundaryDescriptionDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second

            //use a line like this to gain access to the parent's public variables
            //ModuleEmpty parent = (ModuleEmpty)base.Parent1;

            //here are some other possibly-useful items
            //theCanvas.Children.Clear();
            //Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            //Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            //float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            //if (scale == 0) return false;

            ModuleBoundaryDescription mbd = (ModuleBoundaryDescription)ParentModule;
            currentDescription.Text = mbd.descriptionString;
            theTextBox.Text = mbd.descriptionStringIn;
            return true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleBoundaryDescription mbd = (ModuleBoundaryDescription)ParentModule;
            string theText = theTextBox.Text;
            mbd.SetDescription(theText);
        }
    }
}
