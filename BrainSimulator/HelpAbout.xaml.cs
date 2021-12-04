//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Reflection;
using System.Windows;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for HelpAbout.xaml
    /// </summary>
    /// 

    public partial class HelpAbout : Window
    {
        private static DateTime GetBuildDate(Assembly assembly)  // gets build date
        {
            const string BuildVersionMetadataPrefix = "+build";

            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attribute?.InformationalVersion != null)
            {
                var value = attribute.InformationalVersion;
                var index = value.IndexOf(BuildVersionMetadataPrefix);
                if (index > 0)
                {
                    value = value.Substring(index + BuildVersionMetadataPrefix.Length);
                    if (DateTime.TryParseExact(value, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture, 
                        System.Globalization.DateTimeStyles.None, out var result))
                    {
                        return result;
                    }
                }
            }

            return default;
        }

        public HelpAbout()
        {
            InitializeComponent();
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            DateTime buildDate = GetBuildDate(System.Reflection.Assembly.GetExecutingAssembly());
            string displayableVersion = $"{version.Major}.{version.Minor}.{version.Build}   ({buildDate})";
            labelVersion.Content = "Version: " + displayableVersion;
            labelContributors.Content = "Charles J. Simon\nAndré Slabber\n\n\n";
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
