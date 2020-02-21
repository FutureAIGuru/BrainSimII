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

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for PropertiesDlg.xaml
    /// </summary>
    public partial class PropertiesDlg : Window
    {
        public PropertiesDlg()
        {
            InitializeComponent();
            if (MainWindow.theNeuronArray == null)
            { Close();return; }
            txtFileName.Text = MainWindow.currentFileName;
            txtRows.Text = MainWindow.theNeuronArray.rows.ToString();
            txtColumns.Text = (MainWindow.theNeuronArray.arraySize / MainWindow.theNeuronArray.rows).ToString();
            txtNeurons.Text = MainWindow.theNeuronArray.arraySize.ToString();
            int synapseCount = 0;
            int neuronInUseCount = 0;
            for (int i = 0; i < MainWindow.theNeuronArray.arraySize; i++)
            {
                if (MainWindow.theNeuronArray.neuronArray[i].InUse()) neuronInUseCount++;
                synapseCount += MainWindow.theNeuronArray.neuronArray[i].synapses.Count;
            }
            txtNeuronsInUse.Text = neuronInUseCount.ToString();
            txtSynapses.Text = synapseCount.ToString();
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            int.TryParse(txtColumns.Text, out int newColumns);
            int oldColumns = MainWindow.theNeuronArray.arraySize / MainWindow.theNeuronArray.rows;

            if (newColumns != oldColumns)
            {
                MainWindow.SuspendEngine();
                int oldArraySize = MainWindow.theNeuronArray.arraySize;
                int newArraySize = oldArraySize / oldColumns * newColumns;
                Array.Resize(ref  MainWindow.theNeuronArray.neuronArray, newArraySize);
                for (int i = oldArraySize;i < newArraySize; i++)
                {
                    MainWindow.theNeuronArray.neuronArray[i] = new Neuron();
                }
                MainWindow.theNeuronArray.arraySize = newArraySize;
                MainWindow.ResumeEngine();
            }
            MainWindow.Update();
            Close();
        }
    }
}
