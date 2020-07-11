//
// Copyright (c) [Name]. All rights reserved.  
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
    public partial class ModuleSpeakPhonemes2Dlg : ModuleBaseDlg
    {
        public ModuleSpeakPhonemes2Dlg()
        {
            InitializeComponent();
        }
        public override bool Draw(bool checkDrawTimer)
        {
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second
            if (!base.Draw(checkDrawTimer)) return false;

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

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            string text = textBox.Text;
            string phonemes = ModuleSpeakPhonemes2.GetPronunciationFromText(text);
            labelIn.Text = phonemes;
            ((ModuleSpeakPhonemes2)ParentModule).FirePhonemes(phonemes);
        }

        public void SetLabel(string s)
        {
            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
            {
                labelOut.Text = s;
            });
        }

        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                ButtonOK_Click(null, null);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string textFileName = "readingsample1.txt";
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "TXT Command Files|*.txt",
                Title = "Select a Brain Simulator Command File"
            };
            // Show the Dialog.  
            // If the user clicked OK in the dialog  
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                textFileName = openFileDialog1.FileName;

                if (File.Exists(textFileName) && textFileName.IndexOf("Phonemes") == -1)
                {
                    string outFileName = System.IO.Path.GetFileNameWithoutExtension(textFileName);
                    outFileName = textFileName.Replace(outFileName, outFileName + "-Phonemes");
                    List<string> outFileStrings = new List<string>();
                    string[] commands = File.ReadAllLines(textFileName);
                    foreach (string s in commands)
                    {
                        if (s == "STOP") break;
                        string s1 = s.Replace("\"", "");
                        s1 = s1.Replace("“", "");
                        s1 = s1.Replace("”", "");
                        s1 = s1.Replace("'", "");
                        s1 = s1.Replace(",", ".");
                        string[] s2 = s1.Split('.');
                        foreach (string s3 in s2)
                        {
                            string phonemes = ModuleSpeakPhonemes2.GetPronunciationFromText(s3.Trim());
                            if (phonemes != "")
                            {
                                //((ModuleSpeakPhonemes)ParentModule).FirePhonemes(phonemes);
                                outFileStrings.Add(phonemes);
                            }
                        }
                    }
                    File.WriteAllLines(outFileName, outFileStrings);
                }
                if (textFileName.IndexOf("-Phonemes") == -1)
                {
                    string outFileName = System.IO.Path.GetFileNameWithoutExtension(textFileName);
                    textFileName = textFileName.Replace(outFileName, outFileName + "-Phonemes");
                }
                if (File.Exists(textFileName))
                {
                    string[] commands = File.ReadAllLines(textFileName);
                    //note, this is a reverse sort, for rebular, swap x and y
                    if (textFileName.ToLower().IndexOf("vocab") != -1)
                        Array.Sort(commands, (x, y) => y.Length.CompareTo(x.Length));
                    foreach (string phonemes in commands)
                    {
                        ((ModuleSpeakPhonemes2)ParentModule).FirePhonemes(phonemes);
                    }
                }
            }
        }
    }
}
