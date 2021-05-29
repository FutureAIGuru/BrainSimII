//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Net;
using System.Text;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static void CheckForVersionUpdate(bool alwaysShow = false)
        {
            if (!alwaysShow && !Properties.Settings.Default.CheckForUpdates) return;

            try
            {
                WebRequest wr = WebRequest.Create("https://futureai.guru/LatestBrainSimVersion.txt");
                wr.Timeout = 1000; //give up after 1 sec
                WebResponse wrp = wr.GetResponse();
                Stream contentStream = wrp.GetResponseStream();
                string onlineVersionString = new StreamReader(contentStream, Encoding.UTF8).ReadToEnd();
                Version onlineVersion = new Version(onlineVersionString);
                Version runningVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                if (onlineVersion > runningVersion || alwaysShow)
                {
                    DateTime runningBuildDate = new DateTime(2000, 1, 1)
                        .AddDays(runningVersion.Build).AddSeconds(runningVersion.Revision * 2);
                    DateTime onlineBuildDate = new DateTime(2000, 1, 1)
                        .AddDays(onlineVersion.Build).AddSeconds(onlineVersion.Revision * 2);

                    string displayableRunningVersion = $"{runningVersion.Major}.{runningVersion.Minor}.{runningVersion.Build}   ({runningBuildDate})";
                    string displayableOnlineVersion = $"{onlineVersion.Major}.{onlineVersion.Minor}.{onlineVersion.Build}   ({onlineBuildDate})";

                    string s = "Currently Running:\n " + displayableRunningVersion;
                    s += "\n\nAvailable for Download: \n" + displayableOnlineVersion;
                    if (onlineVersion <= runningVersion)
                        s += "\nYou have the latest version";

                    GetUpdateDialog dlg = new GetUpdateDialog();
                    dlg.UpdateInfo.Content = s;
                    dlg.cbDontAsk.IsChecked = !Properties.Settings.Default.CheckForUpdates;
                    dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    dlg.ShowDialog();
                }
            }
            catch
            {
                //it's not critical that we detect this, so just give up on any error
            }
        }


    }
}
