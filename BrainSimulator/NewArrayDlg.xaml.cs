//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
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
        DispatcherTimer barUpdateTimier = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 5) };


        public NewArrayDlg()
        {
            InitializeComponent();
            ulong StartBytes = (ulong)System.GC.GetTotalMemory(true);
            Neuron[] n = new Neuron[sizeCount];
            for (int i = 0; i < sizeCount; i++)
                n[i] = new Neuron();
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

            foreach (Neuron.modelType model in (Neuron.modelType[])Enum.GetValues(typeof(Neuron.modelType)))
            { comboBoxModel.Items.Add(model.ToString()); }
            comboBoxModel.SelectedIndex = 0;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.CloseAllModuleDialogs();
            MainWindow.CloseHistoryWindow();
            MainWindow.CloseNotesWindow();
            if (MainWindow.theNeuronArray != null)
                MainWindow.theNeuronArray.Modules.Clear();
            MainWindow.arrayView.ClearSelection();
            if (!int.TryParse(textBoxColumns.Text, out int cols)) return;
            if (!int.TryParse(textBoxRows.Text, out int rows)) return;
            Neuron.modelType t = (Neuron.modelType)System.Enum.Parse(typeof(Neuron.modelType), comboBoxModel.SelectedItem.ToString());

            arraySize = rows * cols;
            MainWindow.theNeuronArray = new NeuronArray(arraySize, rows, t);
            MainWindow.arrayView.Dp.NeuronDisplaySize = 62;
            MainWindow.arrayView.Dp.DisplayOffset = new Point(0, 0);

            if (checkBoxSynapses.IsChecked ?? true)
            {
                progressBar.Maximum = MainWindow.theNeuronArray.arraySize;
                //allocate randome neurons for testing
                int rows1 = MainWindow.theNeuronArray.rows;
                Task.Factory.StartNew(() =>
                {
                    Parallel.For(0, MainWindow.theNeuronArray.arraySize, i => CreateRandomSynapses(rows1, i));
                });
                //Parallel.For(0, MainWindow.theNeuronArray.arraySize, i => CreateRandomSynapses(rows1, i));
                barUpdateTimier.Tick += Dt_Tick;
                barUpdateTimier.Start();
            }
            else
            {
                Close();
                returnValue = true;
            }
            return;
        }

        private void Dt_Tick(object sender, EventArgs e)
        {
            progressBar.Value = CountSynapses();;
            if (MainWindow.theNeuronArray != null && progressBar.Value >= MainWindow.theNeuronArray.arraySize*.9f)
            {
                barUpdateTimier.Stop();
                returnValue = true;
                Close();
            }
        }
        private int CountSynapses()
        {
            int retVal = 0;
            Parallel.For(0, MainWindow.theNeuronArray.arraySize, i =>
            {
                if (MainWindow.theNeuronArray.neuronArray[i].synapses.Count > 0)
                    retVal++;
            });
            //for (int i = 0; i < MainWindow.theNeuronArray.arraySize; i++)
            //{
            //        if (MainWindow.theNeuronArray.neuronArray[i].InUse())
            //            retVal++;
            //}
            return retVal;
        }

        private void CreateRandomSynapses(int rows, int i)
        {

            Neuron n = MainWindow.theNeuronArray.neuronArray[i];
            //int nextNeuron = n.Id;
            //nextNeuron++;
            //if (nextNeuron == MainWindow.theNeuronArray.arraySize) nextNeuron = 0;
            //n.AddSynapse(nextNeuron, 1.0f);
            //if (nextNeuron < MainWindow.theNeuronArray.arraySize / 2)
            //{
            //    n.AddSynapse(nextNeuron + MainWindow.theNeuronArray.arraySize / 2, 1.0f);
            //    n.AddSynapse(nextNeuron + MainWindow.theNeuronArray.arraySize / 3, 1.0f);
            //    n.AddSynapse(nextNeuron + MainWindow.theNeuronArray.arraySize / 4, 1.0f);
            //    n.AddSynapse(nextNeuron + MainWindow.theNeuronArray.arraySize / 5, 1.0f);
            //}

            int row = i % rows;
            int col = i / rows;

            for (int j = 0; j < (int)assumedSynapseCount; j++)
            {
                int newRow;
                int newCol;
                if (rand == null) rand = new Random();
                lock (rand)
                {
                    newRow = row + rand.Next(10);
                    newCol = col + rand.Next(20);
                }
                int dest = newCol * rows + newRow;
                if (dest >= MainWindow.theNeuronArray.arraySize) dest -= MainWindow.theNeuronArray.arraySize;
                if (dest < 0) dest += MainWindow.theNeuronArray.arraySize;
                //float weight = .95f - (float)rand.Next(0, 1500) / 900f;
                float weight = .07f;
                n.AddSynapse(dest, weight, MainWindow.theNeuronArray, false);
            }
        }

        private void AddSynapses(int i)
        {
            Neuron n = MainWindow.theNeuronArray.neuronArray[i];
            for (int j = 0; j < (int)assumedSynapseCount; j++)
            {
                int dest = rand.Next(MainWindow.theNeuronArray.arraySize - 1);
                float weight = 1 - (float)rand.Next(0, 1000) / 500f;
                n.AddSynapse(dest, weight, MainWindow.theNeuronArray, false);
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
