//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.IO;
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
            for (int i = 0; i < parent.commands.Length; i++)
            {
                if (i == parent.line) theString += ">>>";
                theString += parent.commands[i] + "\r\n";
            }
            textBox.Text = theString;
            if (parent.line > 0)
                textBox.ScrollToLine(parent.line);

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
            theString = theString.Replace(">>>","");

            parent.commands = theString.Split(new string[]{ "\r\n"}, StringSplitOptions.None);
            File.WriteAllLines(parent.textFile, parent.commands);
        }

        private void ButtonBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "XML Network Files|*.txt",
                Title = "Select a Brain Simulator Command File"
            };
            // Show the Dialog.  
            // If the user clicked OK in the dialog  
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK )
            {
                textBoxPath.Text = openFileDialog1.FileName;
            }
        }

        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            ModuleCommand parent = (ModuleCommand)base.ParentModule;
            if (File.Exists(textBoxPath.Text))
            {
                parent.commands = File.ReadAllLines(textBoxPath.Text);
                parent.textFile = textBoxPath.Text;
                parent.line = 0;
                Draw(true);
            }
        }
    }

}
