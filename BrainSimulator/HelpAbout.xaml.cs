//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Windows;

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
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            DateTime buildDate = new DateTime(2000, 1, 1)
                                    .AddDays(version.Build).AddSeconds(version.Revision * 2);
            string displayableVersion = $"{version}\n({buildDate})";
            labelVersion.Content = "Version: " + displayableVersion;
            labelContributors.Content = "cjs\n[add your name]\n\n\n";
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
