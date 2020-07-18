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

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for NewArray.xaml
    /// </summary>
    public partial class NewArrayDlg : Window
    {
        private const int sizeCount = 1000;
        string crlf = "\r\n\r\n";
        public bool returnValue = false;
        ulong approxSynapseSize = 16;
        ulong assumedSynapseCount = 20;
        [ThreadStatic]
        static Random rand = new Random();

        int arraySize;

        //for the progress bar
        DispatcherTimer barUpdateTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 1) };


        public NewArrayDlg()
        {
            InitializeComponent();
            ulong StartBytes = (ulong)System.GC.GetTotalMemory(true);
            Neuron[] n = new Neuron[sizeCount];
            for (int i = 0; i < sizeCount; i++)
                n[i] = new Neuron(false);
            ulong StopBytes = (ulong)System.GC.GetTotalMemory(true);
            ulong neuronSize1 = (StopBytes - StartBytes) / sizeCount;

            ulong availablePhysicalMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory;
            ulong totalPhysicalMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
            long memoryCurrentlyInUse = GC.GetTotalMemory(true);
            //ulong neuronSize = approxNeuronSize + (approxSynapseSize * assumedSynapseCount);
            ulong neuronSize = neuronSize1 + (approxSynapseSize * assumedSynapseCount);
            ulong maxNeurons = availablePhysicalMemory / neuronSize;

            string text = "";
            text += "Total Pysical Memory: " + totalPhysicalMemory.ToString("##,#") + crlf;
            text += "Available Physical Memory: " + availablePhysicalMemory.ToString("##,#") + crlf;
            text += "Max Neurons Possible in RAM: " + maxNeurons.ToString("##,#") + crlf;
            text += "Assuming average " + assumedSynapseCount + " synapses per neuron" + crlf;
            textBlock.Text = text;
        }

        BackgroundWorker bgw = new BackgroundWorker();
        int rows;
        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            buttonOK.IsEnabled = false;
            MainWindow.CloseAllModuleDialogs();
            MainWindow.CloseHistoryWindow();
            MainWindow.CloseNotesWindow();
            if (MainWindow.theNeuronArray != null)
                MainWindow.theNeuronArray.Modules.Clear();
            MainWindow.arrayView.ClearSelection();
            MainWindow.theNeuronArray = new NeuronArray();

            if (!int.TryParse(textBoxColumns.Text, out int cols)) return;
            if (!int.TryParse(textBoxRows.Text, out rows)) return;
            if (checkBoxSynapses.IsChecked == true) doSynapses = true;

            arraySize = rows * cols;
            progressBar.Maximum = arraySize;
            //force garbage collection before starting

            bgw.DoWork += AsyncCreateNeurons;
            bgw.RunWorkerAsync();

            barUpdateTimer.Tick += Dt_Tick;
            barUpdateTimer.Start();

            MainWindow.arrayView.Dp.NeuronDisplaySize = 62;
            MainWindow.arrayView.Dp.DisplayOffset = new Point(0, 0);
        }
        bool done = false;
        bool doingSynapses = false;
        bool doSynapses = false;
        private void Dt_Tick(object sender, EventArgs e)
        {
            if (!doingSynapses) 
                progressBar.Value = MainWindow.theNeuronArray.GetInterimNeuronCount();
            else
            {
                progressBar.Maximum = MainWindow.theNeuronArray.arraySize * 100;
                MainWindow.theNeuronArray.GetCounts(out int synapseCount, out int useCount);
                progressBar.Value = synapseCount;
            }
            if (done)
            {
                barUpdateTimer.Stop();
                returnValue = true;
                Close();
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AsyncCreateNeurons(object sender, DoWorkEventArgs e)
        {
            GC.Collect(3, GCCollectionMode.Forced, true);
            MainWindow.theNeuronArray.Initialize(arraySize, rows);
            if (doSynapses)
            {
                doingSynapses = true;
                GC.Collect(3, GCCollectionMode.Forced, true);
                Parallel.For(0, MainWindow.theNeuronArray.arraySize, i => CreateRandomSynapses(i));
            }
            done = true;
        }

        private void CreateRandomSynapses(int i)
        {
            if (rand == null) rand = new Random();

            Neuron n = MainWindow.theNeuronArray.GetNeuron(i);

            int nextNeuron = n.Id;
            for (int j = 0; j < 100; j++)
            {
                nextNeuron = rand.Next(MainWindow.theNeuronArray.arraySize-1);
                float weight = (rand.Next(1000)/1000f) * .2f - .09f;

                n.AddSynapse(nextNeuron, weight);
            }
        }

    }
}
