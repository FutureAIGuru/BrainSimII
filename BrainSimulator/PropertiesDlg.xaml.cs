//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Linq;
using System.Threading;
using System.Windows;

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
            { Close(); return; }

            txtFileName.Text = MainWindow.currentFileName;
            txtFileName.ToolTip = MainWindow.currentFileName;
            txtRows.Text = MainWindow.theNeuronArray.rows.ToString("N0");
            txtColumns.Text = (MainWindow.theNeuronArray.arraySize / MainWindow.theNeuronArray.rows).ToString("N0");
            txtNeurons.Text = MainWindow.theNeuronArray.arraySize.ToString("N0");
            if (MainWindow.useServers)
            {
                NeuronClient.GetServerList();
                Thread.Sleep(1000);
                txtNeuronsInUse.Text = NeuronClient.serverList.Sum(x => x.neuronsInUse).ToString("N0");
                txtSynapses.Text = NeuronClient.serverList.Sum(x => x.totalSynapses).ToString("N0");
            }
            else
            {
                MainWindow.theNeuronArray.GetCounts(out long synapseCount, out int neuronInUseCount);
                txtNeuronsInUse.Text = neuronInUseCount.ToString("N0");
                txtSynapses.Text = synapseCount.ToString("N0");
            }
//            Owner = MainWindow.thisWindow;
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            int.TryParse(txtColumns.Text, out int newCols);
            int.TryParse(txtRows.Text, out int newRows);
            int oldCols = MainWindow.theNeuronArray.arraySize / MainWindow.theNeuronArray.rows;
            int oldRows = MainWindow.theNeuronArray.rows;
            if (newCols < oldCols || newRows < oldRows)
            {
                MessageBox.Show("Can only make neuron array bigger.");
                return;
            }
            if (newCols != oldCols || newRows != oldRows)
            {
                MainWindow.arrayView.ClearSelection();
                NeuronSelectionRectangle rr = new NeuronSelectionRectangle(0, oldCols,oldRows);
                MainWindow.arrayView.theSelection.selectedRectangles.Add(rr);
                MainWindow.arrayView.CopyNeurons();
                MainWindow.arrayView.ClearSelection();
                MainWindow.theNeuronArray = new NeuronArray();
                MainWindow.theNeuronArray.Initialize(newRows * newCols,newRows);
                MainWindow.theNeuronArray.rows = newRows;
                MainWindow.arrayView.targetNeuronIndex = 0;
                MainWindow.arrayView.PasteNeurons();
                MainWindow.theNeuronArray.ShowSynapses = true;
                MainWindow.thisWindow.SetShowSynapsesCheckBox(true);
                MainWindow.arrayView.ClearShowingSynapses();
                FiringHistory.ClearAll();
                MainWindow.CloseHistoryWindow();
            }
            this.Close();
        }
        private void btnDialogCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
