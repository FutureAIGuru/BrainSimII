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

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for NewArray.xaml
    /// </summary>
    public partial class NewArrayDlg : Window
    {
        string crlf = "\r\n";
        public bool returnValue = false;
        ulong approxNeuronSize = 200;
        ulong approxSynapseSize = 55;
        ulong assumedSynapseCount = 10;

        public NewArrayDlg()
        {
            InitializeComponent();
            ulong availablePhysicalMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory;
            ulong totalPhysicalMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
            long memoryCurrentlyInUse = GC.GetTotalMemory(true);
            ulong neuronSize = approxNeuronSize + (approxSynapseSize * assumedSynapseCount);
            ulong maxNeurons = availablePhysicalMemory / neuronSize;

            string text = "Available Physical Memory: " + availablePhysicalMemory.ToString("##,#") + crlf;
            text += "Total Pysical Memory: " + totalPhysicalMemory.ToString("##,#") + crlf;
            text += "Memory Currently in Use: " + memoryCurrentlyInUse.ToString("##,#") + crlf;
            text += "Max Neurons Possible in RAM: "+maxNeurons.ToString("##,#") + crlf;
            text += "Assuming average "+assumedSynapseCount+" synapses per neuron" + crlf;
            textBlock.Text = text;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        Random rand = new Random();
        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(textBoxColumns.Text, out int cols)) return;
            if (!int.TryParse(textBoxRows.Text, out int rows)) return;
            MainWindow.theNeuronArray = new NeuronArray(rows * cols, rows);
            if (checkBoxSynapses.IsChecked ?? true)
            {
                //allocate randome neurons for testing
                for (int i = 0; i < MainWindow.theNeuronArray.arraySize; i++)
                {
                    Neuron n = MainWindow.theNeuronArray.neuronArray[i];
                    for (int j = 0; j < (int)assumedSynapseCount; j++)
                    {
                        int dest = rand.Next(MainWindow.theNeuronArray.arraySize - 1);
                        float weight = 1 - (float)rand.Next(0, 1000) / 500f;
                        n.AddSynapse(dest, weight, MainWindow.theNeuronArray, false);
                    }
                }
            }
            returnValue = true;
            Close();
            return;
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

        private void TextBoxColumns_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBoxRows_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
