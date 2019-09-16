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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Navigation;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for NewArray.xaml
    /// </summary>
    public partial class NewArrayDlg : Window
    {
        string crlf = "\r\n\r\n";
        public bool returnValue = false;
        ulong approxSynapseSize = 55;
        ulong assumedSynapseCount = 10;

        public NewArrayDlg()
        {
            InitializeComponent();
            ulong StartBytes = (ulong)System.GC.GetTotalMemory(true);
            Neuron[] n = new Neuron[100];
            for (int i = 0; i < 100; i++)
                n[i] = new Neuron();
            ulong StopBytes = (ulong)System.GC.GetTotalMemory(true);
            ulong neuronSize1 = (StopBytes - StartBytes)/100;

            ulong availablePhysicalMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory;
            ulong totalPhysicalMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
            long memoryCurrentlyInUse = GC.GetTotalMemory(true);
            //ulong neuronSize = approxNeuronSize + (approxSynapseSize * assumedSynapseCount);
            ulong neuronSize = neuronSize1 + (approxSynapseSize * assumedSynapseCount);
            ulong maxNeurons = availablePhysicalMemory / neuronSize;

            string text = "";
            text += "Total Pysical Memory: " + totalPhysicalMemory.ToString("##,#") + crlf;
            text += "Available Physical Memory: " + availablePhysicalMemory.ToString("##,#") + crlf;
            text += "Max Neurons Possible in RAM: "+maxNeurons.ToString("##,#") + crlf;
            text += "Assuming average "+assumedSynapseCount+" synapses per neuron" + crlf;
            textBlock.Text = text;

            foreach (Neuron.modelType model in (Neuron.modelType[])Enum.GetValues(typeof(Neuron.modelType)))
            { comboBoxModel.Items.Add(model.ToString()); }
            comboBoxModel.SelectedIndex = 0;

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        Random rand = new Random();
        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(textBoxColumns.Text, out int cols)) return;
            if (!int.TryParse(textBoxRows.Text, out int rows)) return;
            Neuron.modelType t = (Neuron.modelType)System.Enum.Parse(typeof(Neuron.modelType), comboBoxModel.SelectedItem.ToString());

            MainWindow.theNeuronArray = new NeuronArray(rows * cols, rows,t);
            if (checkBoxSynapses.IsChecked ?? true)
            {
                //allocate randome neurons for testing
                int rows1 = MainWindow.theNeuronArray.rows;
                //Parallel.For(0, MainWindow.theNeuronArray.arraySize, i => CreateRandomSynapses(rows1, i));
                for (int i = 0; i < MainWindow.theNeuronArray.arraySize; i++)
                {
                    CreateRandomSynapses(rows1, i);
                }
            }
            returnValue = true;
            Close();
            return;
        }

        private void CreateRandomSynapses(int rows, int i)
        {
            Neuron n = MainWindow.theNeuronArray.neuronArray[i];
            int row = i % rows;
            int col = i / rows;

            for (int j = 0; j < (int)assumedSynapseCount; j++)
            {
                int newRow = row + rand.Next(10);
                int newCol = col + rand.Next(10);
                int dest = newCol * rows + newRow;
                if (dest >= MainWindow.theNeuronArray.arraySize) dest -= MainWindow.theNeuronArray.arraySize;
                if (dest < 0) dest += MainWindow.theNeuronArray.arraySize;
                float weight = 1 - (float)rand.Next(0, 1000) / 500f;
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
                n.AddSynapse(dest, weight, MainWindow.theNeuronArray,false);
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
