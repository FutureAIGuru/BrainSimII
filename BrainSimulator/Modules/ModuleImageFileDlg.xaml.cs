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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BrainSimulator.Modules
{
    public partial class ModuleImageFileDlg : ModuleBaseDlg
    {
        public ModuleImageFileDlg()
        {
            InitializeComponent();
        }
        public override bool Draw(bool checkDrawTimer)
        {
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second
            if (!base.Draw(checkDrawTimer)) return false;
            ModuleImageFile parent = (ModuleImageFile)base.ParentModule;
            textBoxPath.Text = parent.filePath;

            //use a line like this to gain access to the parent's public variables
            //ModuleEmpty parent = (ModuleEmpty)base.Parent1;

            //here are some other possibly-useful items
            //theCanvas.Children.Clear();
            //Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            //Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            //float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            //if (scale == 0) return false;

            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(true);
        }

        private void Button_Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "Image Files|*.bmp;*.png",
                Title = "Select a Brain Simulator Command File",
            };
            // Show the Dialog.  
            // If the user clicked OK in the dialog  
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                textBoxPath.Text = openFileDialog1.FileName;
                ModuleImageFile parent = (ModuleImageFile)base.ParentModule;
                parent.SetNewPath(textBoxPath.Text, false);
            }
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            ModuleImageFile parent = (ModuleImageFile)base.ParentModule;
            parent.SetNewPath(textBoxPath.Text, (bool)cbEntireFolder.IsChecked);
        }

        private void Button_Click_Next(object sender, RoutedEventArgs e)
        {
            ModuleImageFile parent = (ModuleImageFile)base.ParentModule;
            parent.NextFile();
        }

        private void Button_Click_Prev(object sender, RoutedEventArgs e)
        {
            ModuleImageFile parent = (ModuleImageFile)base.ParentModule;
            parent.PrevFile();
        }
    }
}
