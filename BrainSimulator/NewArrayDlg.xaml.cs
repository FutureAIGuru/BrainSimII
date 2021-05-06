//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows.Controls;
using System.Windows.Media;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for NewArray.xaml
    /// </summary>
    public partial class NewArrayDlg : Window
    {
        private const int sizeCount = 1000;
        string crlf = "\r\n";
        public bool returnValue = false;
        ulong approxSynapseSize = 16;
        ulong assumedSynapseCount = 20;
        ulong maxNeurons = 0;
        [ThreadStatic]
        static Random rand = new Random();

        int arraySize;
        bool previousUseNeurons = false;

        public NewArrayDlg()
        {
            InitializeComponent();

            cbUseServers.IsChecked = MainWindow.useServers;
            buttonSpeedTest.IsEnabled = MainWindow.useServers;
            buttonRefresh.IsEnabled = MainWindow.useServers;
            //textBoxRows.Text = "1000";
            //textBoxColumns.Text = "1000";
            textBoxRows.Text = "15";
            textBoxColumns.Text = "30";

            ulong neuronSize1 = 55;

            ulong availablePhysicalMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory;
            ulong totalPhysicalMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
            long memoryCurrentlyInUse = GC.GetTotalMemory(true);
            //ulong neuronSize = approxNeuronSize + (approxSynapseSize * assumedSynapseCount);
            ulong neuronSize = neuronSize1 + (approxSynapseSize * assumedSynapseCount);
            maxNeurons = availablePhysicalMemory / neuronSize;

            string text = "";
            text += "Total Memory: " + totalPhysicalMemory.ToString("##,#") + crlf;
            text += "Available Memory: " + availablePhysicalMemory.ToString("##,#") + crlf;
            text += "Max Neurons Possible: " + maxNeurons.ToString("##,#") + crlf;
            text += "Assuming average " + assumedSynapseCount + " synapses per neuron" + crlf;
            textBlock.Text = text;

            previousUseNeurons = MainWindow.useServers;
            cbUseServers.IsChecked = MainWindow.useServers;
            UpdateServerTextBox();
        }

        private void UpdateServerTextBox()
        {
            if (cbUseServers.IsChecked == true)
            {
                NeuronClient.GetServerList();
                Thread.Sleep(1000);
                if (NeuronClient.serverList.Count == 0)
                {
                    ServerList.Text = "No Servers Detected";
                    buttonSpeedTest.IsEnabled = false;
                }
                else
                {
                    int.TryParse(textBoxColumns.Text, out cols);
                    int.TryParse(textBoxRows.Text, out rows);
                    ServerList.Text = "";
                    MainWindow.useServers = true;
                    int numServers = NeuronClient.serverList.Count;
                    int neuronsNeeded = rows * cols;
                    for (int i = 0; i < numServers; i++)
                    {
                        NeuronClient.Server s = NeuronClient.serverList[i];
                        s.firstNeuron = i * neuronsNeeded / numServers;
                        s.lastNeuron = (i + 1) * neuronsNeeded / numServers;
                        ServerList.Text += s.ipAddress.ToString() + " " + s.name + " " + s.firstNeuron + " " + s.lastNeuron + "\n";
                    }
                    buttonSpeedTest.IsEnabled = true;
                }
            }
            else
            {
                ServerList.Text = "";
            }
        }

        int rows;
        int cols;
        int refractory = 0;
        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.CloseAllModuleDialogs();
            MainWindow.CloseHistoryWindow();
            MainWindow.CloseNotesWindow();
            MainWindow.arrayView.ClearShowingSynapses();
            if (MainWindow.theNeuronArray != null)
            {
                lock (MainWindow.theNeuronArray.Modules)
                {
                    MainWindow.theNeuronArray.Modules.Clear();
                }
            }
            MainWindow.arrayView.ClearSelection();
            MainWindow.theNeuronArray = new NeuronArray();

            if (!int.TryParse(textBoxColumns.Text, out cols)) return;
            if (!int.TryParse(textBoxRows.Text, out rows)) return;
            if (cols <= 0) return;
            if (rows <= 0) return;
            //if (checkBoxSynapses.IsChecked == true) doSynapses = true;
            if (!int.TryParse(Refractory.Text, out refractory)) return;

            arraySize = rows * cols;
            //progressBar.Maximum = arraySize;

            //int.TryParse(textBoxSynapses.Text, out synapsesPerNeuron);
            MainWindow.arrayView.Dp.NeuronDisplaySize = 62;
            MainWindow.arrayView.Dp.DisplayOffset = new Point(0, 0);

            if (MainWindow.useServers && NeuronClient.serverList.Count > 0)
            {
                //TODO: Replace this with a multicolumn UI
                MainWindow.theNeuronArray.Initialize(arraySize, rows);
                string[] lines = ServerList.Text.Split('\n');
                NeuronClient.serverList.Clear();
                foreach (string line in lines)
                {
                    if (line == "") continue;
                    string[] command = line.Split(' ');
                    NeuronClient.Server s = new NeuronClient.Server();
                    s.ipAddress = IPAddress.Parse(command[0]);
                    s.name = command[1];
                    int.TryParse(command[2], out s.firstNeuron);
                    int.TryParse(command[3], out s.lastNeuron);
                    NeuronClient.serverList.Add(s);
                }

                int totalNeuronsInServers = 0;
                for (int i = 0; i < NeuronClient.serverList.Count; i++)
                    totalNeuronsInServers += NeuronClient.serverList[i].lastNeuron - NeuronClient.serverList[i].firstNeuron;
                if (totalNeuronsInServers != arraySize)
                {
                    MessageBox.Show("Server neuron allocation does not equal total neurons!");
                    buttonOK.IsEnabled = true;
                    returnValue = false;
                    return;
                }

                NeuronClient.InitServers(0, arraySize);
                NeuronClient.WaitForDoneOnAllServers();
                returnValue = true;
                Close();
                returnValue = true;
            }
            else
            {
                GC.Collect(3, GCCollectionMode.Forced, true);
                MainWindow.theNeuronArray.Initialize(arraySize, rows);
                MainWindow.theNeuronArray.RefractoryDelay = refractory;
         
                MainWindow.theNeuronArray.ShowSynapses = false;
                MainWindow.thisWindow.SetShowSynapsesCheckBox(false);
                Close();
                returnValue = true;
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.useServers = previousUseNeurons;
            this.Close();
        }

 
        private void Button_Refresh(object sender, RoutedEventArgs e)
        {
            UpdateServerTextBox();
        }

        //PING speed test
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string s = ServerList.SelectedText;
            if (!IPAddress.TryParse(s, out IPAddress targetIp))
            {
                MessageBox.Show("Highlight an IP address");
                return;
            }
            NeuronClient.pingCount = 0;
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            string payload = NeuronClient.CreatePayload(1450);
            for (int i = 0; i < 100000; i++)
            {
                NeuronClient.SendToServer(targetIp, "Ping");
            }
            sw.Stop();
            double packetSendNoPayload = ((double)sw.ElapsedMilliseconds) / 100000.0;
            Thread.Sleep(1000);

            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
                NeuronClient.SendToServer(targetIp, "Ping " + payload);
            }
            sw.Stop();
            double packetSendBigPayload = ((double)sw.ElapsedMilliseconds) / 100000.0;
            Thread.Sleep(1000);

            List<long> rawData = new List<long>();
            for (int i = 0; i < 1000; i++)
                rawData.Add(NeuronClient.Ping(targetIp, ""));
            double latencyNoPayload = ((double)rawData.Average()) / 10000.0;
            rawData.Clear();
            for (int i = 0; i < 1000; i++)
                rawData.Add(NeuronClient.Ping(targetIp, payload));
            double latencyBigPayload = ((double)rawData.Average()) / 10000.0;

            PingLabel.Content = "Packet Spd: " + packetSendNoPayload.ToString("F4") + "ms-" + packetSendBigPayload.ToString("F4") + "ms  R/T Latency:  "
                + latencyNoPayload.ToString("F4") + "ms-" + latencyBigPayload.ToString("F4") + "ms " + NeuronClient.pingCount;
            PingLabel1.Visibility = Visibility.Visible;
        }

        private void CheckBoxUseServers_Checked(object sender, RoutedEventArgs e)
        {
            NeuronClient.Init();
            UpdateServerTextBox();
            if (NeuronClient.serverList.Count > 0)
                MainWindow.useServers = true;
            buttonRefresh.IsEnabled = true; ;
        }

        private void CheckBoxUseServers_Unchecked(object sender, RoutedEventArgs e)
        {
            MainWindow.useServers = false;
            buttonRefresh.IsEnabled = MainWindow.useServers;
            UpdateServerTextBox();
        }


        //engine Refractory up/dn  buttons 
        private void Button_RefractoryUpClick(object sender, RoutedEventArgs e)
        {
            refractory++;
            Refractory.Text = refractory.ToString();
        }

        private void Button_RefractoryDnClick(object sender, RoutedEventArgs e)
        {
            refractory--;
            if (refractory < 0) refractory = 0;
            Refractory.Text = refractory.ToString();
        }

        private void textBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (textBoxColumns == null || textBoxRows == null || LabelNeuronCount == null) return;
            if (sender is TextBox tb)
            {
                if (!int.TryParse(tb.Text, out int value) || value <= 0)
                    tb.Background = new SolidColorBrush(Colors.Pink);
                else
                    tb.Background = new SolidColorBrush(Colors.LightGreen);
            }
            if (int.TryParse(textBoxColumns.Text, out int cols) &&
                int.TryParse(textBoxRows.Text, out int rows))
            {
                long total = (long)rows * (long)cols;
                LabelNeuronCount.Content = "Neuron Count: " + total.ToString("##,#");
                if ((ulong)(rows * cols) > maxNeurons)
                {
                    LabelNeuronCount.Content = "Neuron Count > Estimated Maximum!";
                    textBoxColumns.Background = new SolidColorBrush(Colors.Red);
                    textBoxRows.Background = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    textBoxColumns.Background = new SolidColorBrush(Colors.LightGreen);
                    textBoxRows.Background = new SolidColorBrush(Colors.LightGreen);
                }
            }
            else
                LabelNeuronCount.Content = "Neuron Count: ERROR";
        }
    }
}
