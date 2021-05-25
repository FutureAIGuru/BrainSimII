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
        private static void CheckForVersionUpdate()
        {
            try
            {
                WebRequest wr = WebRequest.Create("https://futureai.guru/LatestBrainSimVersion.txt");
                wr.Timeout = 1000; //give up after 1 sec
                WebResponse wrp = wr.GetResponse();
                Stream contentStream = wrp.GetResponseStream();
                string onlineVersionString= new StreamReader(contentStream, Encoding.UTF8).ReadToEnd();
                Version onlineVersion = new Version(onlineVersionString);
                Version runningVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                if (onlineVersion > runningVersion)  
                {
                    //TODO: put the messagebox/action in here

                }
            }
            catch (Exception e)
            {
                //it's not critical that we detect this, so just give up on any error
            }
        }


    }
}
