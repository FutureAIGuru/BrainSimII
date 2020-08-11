//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Windows;

namespace BrainSimulator.Modules
{
    public partial class ModuleCameraDlg : ModuleBaseDlg
    {
        public ModuleCameraDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            ModuleCamera parent = (ModuleCamera)base.ParentModule;
            if (parent.theDlgBitMap != null)
            {
                image.Source = parent.theDlgBitMap;
            }
            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(true);
        }

    }
}
