//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Windows;
using System.Windows.Input;

namespace BrainSimulator.Modules
{
    public partial class ModuleSpeakPhonemesDlg : ModuleBaseDlg
    {
        public ModuleSpeakPhonemesDlg()
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
            string phonemes = ModuleSpeakPhonemes.GetPronunciationFromText(text);
            labelIn.Text = phonemes;
            ((ModuleSpeakPhonemes)ParentModule).FirePhonemes(phonemes);
        }

        public void SetLabel(string s)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                labelOut.Text = s;
            });
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                ButtonOK_Click(null, null);
        }
    }
}
