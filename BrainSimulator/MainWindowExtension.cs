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
using System.Net.Http;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        static HttpClient theHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2), };
        private async static void CheckForVersionUpdate(bool alwaysShow = false)
        {
            if (!alwaysShow && !Properties.Settings.Default.CheckForUpdates) return;

            try
            {
                string onlineVersionString = "";
                var response = await theHttpClient.GetAsync(webURL+"/LatestBrainSimVersion.txt");
                if (response.IsSuccessStatusCode)
                {
                    DateTime? fileDate = response.Content.Headers.LastModified?.DateTime;
                    
                    var theStream = await response.Content.ReadAsByteArrayAsync();
                    using (var contentStream = new MemoryStream(theStream))
                    {
                        onlineVersionString = new StreamReader(contentStream, Encoding.UTF8).ReadToEnd();
                    }
                    Version onlineVersion = new Version(onlineVersionString);
                    var runningVersionx = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                    Version runningVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                    if (onlineVersion > runningVersion || alwaysShow)
                    {
                        string displayableOnlineVersion = onlineVersion.ToString() + "    (" + fileDate.ToString() + ")";

                        string s = "Currently Running:\n " + HelpAbout.GetBuildString();
                        s += "\n\nAvailable for Download: \n" + displayableOnlineVersion;
                        if (onlineVersion <= runningVersion)
                            s += "\nYou have the latest version";

                        GetUpdateDialog dlg = new GetUpdateDialog();
                        dlg.Owner = thisWindow;
                        dlg.UpdateInfo.Content = s;
                        dlg.cbDontAsk.IsChecked = !Properties.Settings.Default.CheckForUpdates;
                        dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        dlg.ShowDialog();
                    }
                }
            }
            catch
            {
                //it's not critical that we detect this, so just give up on any error
            }
        }


    }
}
