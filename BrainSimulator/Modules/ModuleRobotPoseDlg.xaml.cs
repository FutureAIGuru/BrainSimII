//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Windows;

namespace BrainSimulator.Modules
{
    public partial class ModuleRobotPoseDlg : ModuleBaseDlg
    {
        public ModuleRobotPoseDlg()
        {
            InitializeComponent();

            this.DataContext = this;
        }

        string theConfigString;
        ModuleRobotPose parent = null;
        public string TheConfigString
        {
            get
            {
                if (parent == null)
                {
                    parent = (ModuleRobotPose)base.ParentModule;
                    theConfigString = parent.PosesString;
                }
                return theConfigString;
            }
            set
            {
                theConfigString = value;
            }
        }


        public override bool Draw(bool checkDrawTimer)
        {
            //not used
            if (!base.Draw(checkDrawTimer)) return false;
            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleRobotPose parent = (ModuleRobotPose)base.ParentModule;
            parent.PosesString = theConfigString;
            Close();
        }
    }
}
