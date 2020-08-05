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
            txtFileName.ToolTip = MainWindow.currentFileName;
            txtRows.Text = MainWindow.theNeuronArray.rows.ToString("N0");
            txtColumns.Text = (MainWindow.theNeuronArray.arraySize / MainWindow.theNeuronArray.rows).ToString("N0");
            txtNeurons.Text = MainWindow.theNeuronArray.arraySize.ToString("N0");
            MainWindow.theNeuronArray.GetCounts(out int synapseCount, out int neuronInUseCount);
            txtNeuronsInUse.Text = neuronInUseCount.ToString("N0");
            txtSynapses.Text = synapseCount.ToString("N0");
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
