//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace BrainSimulator.Modules
{

    public partial class ModuleCommandDlg : ModuleBaseDlg
    {
        public ModuleCommandDlg()
        {
            InitializeComponent();
        }
        public override bool Draw(bool checkDrawTimer)
        {
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second
            if (!base.Draw(checkDrawTimer)) return false;

            //use a line like this to gain access to the parent's public variables
            ModuleCommand parent = (ModuleCommand)base.ParentModule;

            textBoxPath.Text = parent.textFile;
            string theString = "";
            if (parent.commands == null) return true;
            int start = -1;
            int end = -1;
            for (int i = 0; i < parent.commands.Length; i++)
            {
                if (i == parent.line)
                {
                    start = theString.Length;
                    theString += ">";
                    theString += parent.commands[i] + "\r\n";
                    end = theString.Length;
                }
                else
                    theString += parent.commands[i] + "\r\n";
            }
            textBox.Text = theString;
            if (parent.line > 0)
                textBox.ScrollToLine(parent.line);
            if (start != -1 && end != -1)
            {
                textBox.SelectionStart = start;
                textBox.SelectionLength = end - start;
                string xx = textBox.SelectedText;
                textBox.Select(start, end - start);
                textBox.Focus();
            }

            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(true);
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {

            ModuleCommand parent = (ModuleCommand)base.ParentModule;
            parent.textFile = textBoxPath.Text;
            string theString = textBox.Text;
            theString = theString.Replace(">>>", "");

            if (parent.textFile == "")
            {
                //create a new file
                if (!SaveFileName()) return;
            }

            parent.commands = theString.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            File.WriteAllLines(parent.textFile, parent.commands);
        }

        private bool SaveFileName()
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Filter = "Command Files|*.txt",
                Title = "Select/Create a Brain Simulator Command File"
            };
            // Show the Dialog.  
            // If the user clicked OK in the dialog  
            DialogResult result = saveFileDialog1.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ModuleCommand parent = (ModuleCommand)base.ParentModule;
                textBoxPath.Text = saveFileDialog1.FileName;
                parent.textFile = textBoxPath.Text;
                parent.line = -1;
                return true;
            }
            return false;
        }
        private bool OpenFileName()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "Command Files|*.txt",
                Title = "Select a Brain Simulator Command File"
            };
            // Show the Dialog.  
            // If the user clicked OK in the dialog  
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                textBoxPath.Text = openFileDialog1.FileName;
                return true;
            }
            return false;
        }
        private void ButtonBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileName();
        }

        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            ModuleCommand parent = (ModuleCommand)base.ParentModule;
            if (File.Exists(textBoxPath.Text))
            {
                parent.commands = File.ReadAllLines(textBoxPath.Text);
                parent.textFile = textBoxPath.Text;
                parent.line = -1;
                Draw(true);
            }
        }
        private void ButtonRun_Click(object sender, RoutedEventArgs e)
        {
            ModuleCommand parent = (ModuleCommand)base.ParentModule;
            parent.textFile = textBoxPath.Text;
            parent.line = 0;
            Draw(true);
        }
    }

}
