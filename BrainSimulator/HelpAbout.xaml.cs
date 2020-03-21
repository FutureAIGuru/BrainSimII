//
// Copyright (c) Charles Simon. All rights reserved.  
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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for HelpAbout.xaml
    /// </summary>
    public partial class HelpAbout : Window
    {
        public HelpAbout()
        {
            InitializeComponent();
            labelContributors.Content = "[add your name here if\nyou add to this project]";
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
