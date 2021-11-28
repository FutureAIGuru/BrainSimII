//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Windows;

namespace BrainSimulator.Modules
{
    public partial class ModuleRobotActionDlg : ModuleBaseDlg
    {
        public ModuleRobotActionDlg()
        {
            InitializeComponent();

            this.DataContext = this;
        }

        string theConfigString;
        ModuleRobotAction parent = null;
        public string TheConfigString
        {
            get
            {
                if (parent == null)
                {
                    parent = (ModuleRobotAction)base.ParentModule;
                    theConfigString = parent.ActionsString;
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
            ModuleRobotAction parent = (ModuleRobotAction)base.ParentModule;
            parent.ActionsString = theConfigString;
            Close();
        }
    }
}
