//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Windows;

namespace BrainSimulator.Modules
{
    public partial class ModuleRobotDlg : ModuleBaseDlg
    {
        public ModuleRobotDlg()
        {
            InitializeComponent();

            this.DataContext = this;
        }

        string theConfigString;
        ModuleRobot parent = null;
        public string TheConfigString
        {
            get
            {
                if (parent == null)
                {
                    parent = (ModuleRobot)base.ParentModule;
                    theConfigString = parent.configString;
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
            ModuleRobot parent = (ModuleRobot)base.ParentModule;
            parent.configString = theConfigString;
            Close();
        }
    }
}
