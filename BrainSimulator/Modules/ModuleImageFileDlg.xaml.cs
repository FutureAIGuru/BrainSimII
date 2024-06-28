//
// Copyright (c) Charles Simon. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace BrainSimulator.Modules
{
    public partial class ModuleImageFileDlg : ModuleBaseDlg
    {
        private string defaultDirectory = "";

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
            if (File.Exists(parent.filePath))
                textBoxPath.Text = parent.filePath;
            cbNameIsDescription.IsChecked = parent.useDescription;

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
            if (defaultDirectory == "")
            {
                defaultDirectory = System.IO.Path.GetDirectoryName(MainWindow.currentFileName);
            }
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "Image Files| *.png;*.jpg",
                Title = "Select an image file",
                Multiselect = true,
                InitialDirectory = defaultDirectory,
            };
            // Show the Dialog.
            // If the user clicked OK in the dialog
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                defaultDirectory = System.IO.Path.GetDirectoryName(openFileDialog1.FileName);
                ModuleImageFile parent = (ModuleImageFile)base.ParentModule;

                textBoxPath.Text = openFileDialog1.FileName;
                List<string> fileList;
                string curPath;
                if (openFileDialog1.FileNames.Length > 1)
                {
                    fileList = openFileDialog1.FileNames.ToList();
                    curPath = fileList[0];
                }
                else
                {
                    fileList = GetFileList(openFileDialog1.FileName);
                    curPath = openFileDialog1.FileName;
                }

                parent.SetParameters(fileList, curPath, (bool)cbAutoCycle.IsChecked, (bool)cbNameIsDescription.IsChecked);
            }
        }

        private List<string> GetFileList(string filePath)
        {
            SearchOption subFolder = SearchOption.AllDirectories;
            if (!(bool)cbUseSubfolders.IsChecked)
                subFolder = SearchOption.TopDirectoryOnly;
            string dir = filePath;
            FileAttributes attr = File.GetAttributes(filePath);
            if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
                dir = System.IO.Path.GetDirectoryName(filePath);
            return new List<string>(Directory.EnumerateFiles(dir, "*.png", subFolder));
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            ModuleImageFile parent = (ModuleImageFile)base.ParentModule;
            parent.SetParameters(null, textBoxPath.Text, (bool)cbAutoCycle.IsChecked, (bool)cbNameIsDescription.IsChecked);
        }

        private void Button_Click_Next(object sender, RoutedEventArgs e)
        {
            ModuleImageFile parent = (ModuleImageFile)base.ParentModule;
            cbAutoCycle.IsChecked = false;
            parent.NextFile();
        }

        private void Button_Click_Prev(object sender, RoutedEventArgs e)
        {
            ModuleImageFile parent = (ModuleImageFile)base.ParentModule;
            cbAutoCycle.IsChecked = false;
            parent.PrevFile();
        }

        private void CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            ModuleImageFile parent = (ModuleImageFile)base.ParentModule;
            parent.SetParameters(null, "", (bool)cbAutoCycle.IsChecked, (bool)cbNameIsDescription.IsChecked);
        }

        private void ButtonDescr_Click(object sender, RoutedEventArgs e)
        {
            ModuleImageFile parent = (ModuleImageFile)base.ParentModule;
            parent.ResendDescription();
        }
    }
}
