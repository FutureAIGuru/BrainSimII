
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace CsEngineTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        static NeuronHandler theNeuronArray = null;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int neuronCount = 1000000;
            int synapsesPerNeuron = 1000;
            MessageBox.Show("Starting array allocation");
            theNeuronArray = new NeuronHandler();
            MessageBox.Show("any existing array removed");
            theNeuronArray.Initialize(neuronCount);
            MessageBox.Show("array allocation complete");
            int test = theNeuronArray.GetArraySize();
            int threads = theNeuronArray.GetThreadCount();
            theNeuronArray.SetThreadCount(16);
            threads = theNeuronArray.GetThreadCount();

            theNeuronArray.SetNeuronCurrentCharge(1, 1.4f);
            theNeuronArray.SetNeuronCurrentCharge(2, 0.9f);
            theNeuronArray.Fire(); //should transfer current chargest to last
            float a = theNeuronArray.GetNeuronLastCharge(1);
            float b = theNeuronArray.GetNeuronLastCharge(2);

            string s0 = theNeuronArray.GetNeuronLabel(1);
            theNeuronArray.SetNeuronLabel(1, "Fred");
            string s1 = theNeuronArray.GetNeuronLabel(1);
            theNeuronArray.SetNeuronLabel(1, "George");
            string s2 = theNeuronArray.GetNeuronLabel(1);

            theNeuronArray.AddSynapse(2, 4, .75f, 1, false);
            List<Synapse> synapses2 = theNeuronArray.GetSynapsesList(2);
            theNeuronArray.AddSynapse(1, 2, .5f, 0, false);
            List<Synapse> synapses1 = theNeuronArray.GetSynapsesList(1);
            theNeuronArray.AddSynapse(1, 3, .6f, 0, false);
            synapses1 = theNeuronArray.GetSynapsesList(1);
            theNeuronArray.AddSynapse(1, 4, .75f, 1, false);
            synapses1 = theNeuronArray.GetSynapsesList(1);
            theNeuronArray.AddSynapse(2, 4, .75f, 1, false);
            long count = theNeuronArray.GetTotalSynapses();
            List<Synapse> synapses0 = theNeuronArray.GetSynapsesList(0);
            synapses1 = theNeuronArray.GetSynapsesList(1);
            List<Synapse> synapsesFrom = theNeuronArray.GetSynapsesFromList(4);

            NeuronPartial n = theNeuronArray.GetPartialNeuron(1);
            theNeuronArray.Fire();
            long gen = theNeuronArray.GetGeneration();
            NeuronPartial n1 = theNeuronArray.GetPartialNeuron(1);
            NeuronPartial n2 = theNeuronArray.GetPartialNeuron(2);
            NeuronPartial n3 = theNeuronArray.GetPartialNeuron(3);
            theNeuronArray.DeleteSynapse(1, 3);
            theNeuronArray.DeleteSynapse(1, 2);


            MessageBox.Show("allocating synapses");
            Parallel.For(0, neuronCount, x =>
            {
                //for (int x = 0; x < neuronCount; x++)
                //{
                for (int j = 0; j < synapsesPerNeuron; j++)
                {
                    int target = x + j;
                    if (target >= theNeuronArray.GetArraySize()) target -= theNeuronArray.GetArraySize();
                    if (target < 0) target += theNeuronArray.GetArraySize();
                    theNeuronArray.AddSynapse(x, target, 1.0f, 0, true);
                }
            });
            for (int i = 0; i < neuronCount / 100; i++)
                theNeuronArray.SetNeuronCurrentCharge(100 * i, 1);
            MessageBox.Show("synapses and charge complete");
            Stopwatch sw = new Stopwatch();
            string msg = "";
            for (int i = 0; i < 10; i++)
            {
                sw.Start();
                theNeuronArray.Fire();
                sw.Stop();
                msg += "Gen: " + theNeuronArray.GetGeneration() + "  FireCount: " + theNeuronArray.GetFiredCount() + " time: " + sw.Elapsed.Milliseconds.ToString() + "\n";
                sw.Reset();
            }
            sw.Stop();
            MessageBox.Show("Done firing 10x\n" + msg);
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "XML Network Files|*.xml",
                Title = "Select a Brain Simulator File"
            };
            // Show the Dialog.  
            // If the user clicked OK in the dialog and  
            Nullable<bool> result = openFileDialog1.ShowDialog();
            if (result ?? false)
            {
                string currentFileName = openFileDialog1.FileName;
                bool loadSuccessful = XmlFile.Load(ref theNeuronArray, currentFileName);
                if (!loadSuccessful)
                {
                    currentFileName = "";
                }
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "XML Network Files|*.xml",
                Title = "Select a Brain Simulator File"
            };
            // Show the Dialog.  
            // If the user clicked OK in the dialog and  
            Nullable<bool> result = openFileDialog1.ShowDialog();
            if (result ?? false)
            {
                string currentFileName = openFileDialog1.FileName;
                bool loadSuccessful = XmlFile.Save(ref theNeuronArray, currentFileName);
                if (!loadSuccessful)
                {
                    currentFileName = "";
                }
            }

        }
    }
}
