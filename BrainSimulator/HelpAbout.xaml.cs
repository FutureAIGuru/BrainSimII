//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
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
            string displayableVersion = GetBuildString();
            labelVersion.Content = "Version: " + displayableVersion;
            labelContributors.Content = "Charles J. Simon\nAndré Slabber\n\n\n";
        }

        public static string GetBuildString()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Version assemblyVersion = assembly.GetName().Version;
            string version = GetPackageVersion()
                ?? assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                    .FirstOrDefault(attribute => attribute.Key == "StorePackageVersion")?.Value
                ?? assemblyVersion.ToString(3);
            DateTime buildDate = GetBuildDate(assembly);
            return $"{version}   ({buildDate})";
        }

        private static string GetPackageVersion()
        {
            const int ErrorInsufficientBuffer = 122;
            if (!OperatingSystem.IsWindowsVersionAtLeast(6, 2))
                return null;

            int length = 0;
            if (GetCurrentPackageFullName(ref length, null) != ErrorInsufficientBuffer)
                return null;

            var packageFullName = new StringBuilder(length);
            if (GetCurrentPackageFullName(ref length, packageFullName) != 0)
                return null;

            // Package full names use Name_Version_Architecture_ResourceId_PublisherId.
            string[] parts = packageFullName.ToString().Split('_', 3);
            return parts.Length == 3 ? parts[1] : null;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int GetCurrentPackageFullName(ref int length, StringBuilder packageFullName);

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
