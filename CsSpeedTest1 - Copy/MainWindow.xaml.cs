using NeuronEngine;
using NeuronEngine.CLI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;

namespace CsSpeedTest1
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int neuronCount = 1000000;
            int synapsesPerNeuron = 1000;
            MessageBox.Show("Starting array allocation");
            NeuronArrayBase nab = new NeuronArrayBase();
            MessageBox.Show("any existing array removed");
            nab.Initialize(neuronCount);
            MessageBox.Show("array allocation complete");
            int test = nab.GetArraySize();
            int threads = nab.GetThreadCount();
            nab.SetThreadCount(16);
            threads = nab.GetThreadCount();

            nab.SetNeuronCharge(1, 1.4f);
            nab.SetNeuronCharge(2, 0.9f);
            float a = nab.GetNeuronLastCharge(1);
            float b = nab.GetNeuronLastCharge(2);

            nab.AddSynapse(1, 2, .5f, false);
            nab.AddSynapse(1, 3, .6f, false);
            nab.AddSynapse(1, 4, .75f, true);
            nab.AddSynapse(2, 4, .75f, true);
            long count = nab.GetTotalSynapses();
            List<Synapse> synapses = ConvertToSynapseList(nab.GetSynapses(1));
            Neuron n = ConvertToNeuron(nab.GetNeuron(1));
            nab.Fire();
            long gen = nab.GetGeneration();
            Neuron n1 = ConvertToNeuron(nab.GetNeuron(1));
            Neuron n2 = ConvertToNeuron(nab.GetNeuron(2));
            Neuron n3 = ConvertToNeuron(nab.GetNeuron(3));
            nab.DeleteSynapse(1, 3, 0f);
            nab.DeleteSynapse(1, 2, .1f);
            MessageBox.Show("allocating synapses");
            Parallel.For(0, neuronCount, x =>
            {
            //for (int x = 0; x < neuronCount; x++)
            //{
                for (int j = 0; j < synapsesPerNeuron; j++)
                {
                    int target = x + j;
                    if (target >= nab.GetArraySize()) target -= nab.GetArraySize();
                    if (target < 0) target += nab.GetArraySize();
                    nab.AddSynapse(x, target, 1.0f, false);
                }
            });
            for (int i = 0; i < neuronCount / 100; i++)
                nab.SetNeuronCharge(100 * i, 1);
            MessageBox.Show("synapses and charge complete");
            Stopwatch sw = new Stopwatch();
            string msg = "";
            for (int i = 0; i < 10; i++)
            {
                sw.Start();
                nab.Fire();
                sw.Stop();
                msg += "Gen: " + nab.GetGeneration() + "  FireCount: " + nab.GetFiredCount() + " time: " + sw.Elapsed.Milliseconds.ToString() + "\n";
                sw.Reset();
            }
            sw.Stop();
            MessageBox.Show("Done firing 10x\n" + msg);
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Synapse
        {
            int target;
            float weight;
            bool isHebbian;
        };
        enum modelType { Std, Color, FloatValue, LIF, Random };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Neuron
        {
            int id;
            float lastCharge;
            float currentCharge;
            float leakRate;
            long lastFired;
            modelType model;
        };

        List<Synapse> ConvertToSynapseList(byte[] input)
        {
            List<Synapse> retVal = new List<Synapse>();
            Synapse s = new Synapse();
            int sizeOfSynapse = Marshal.SizeOf(s);
            int numberOfSynapses = input.Length / sizeOfSynapse;
            byte[] oneSynapse = new byte[sizeOfSynapse];
            for (int i = 0; i < numberOfSynapses - 1; i++)
            {
                int offset = i * sizeOfSynapse;
                for (int k = 0; k < sizeOfSynapse; k++)
                    oneSynapse[k] = input[k + offset];
                GCHandle handle = GCHandle.Alloc(oneSynapse, GCHandleType.Pinned);
                s = (Synapse)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Synapse));
                retVal.Add(s);
                handle.Free();
            }
            return retVal;
        }
        Neuron ConvertToNeuron(byte[] input)
        {
            Neuron n = new Neuron();
            Synapse s = new Synapse();
            GCHandle handle = GCHandle.Alloc(input, GCHandleType.Pinned);
            n = (Neuron)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Neuron));
            handle.Free();
            return n;
        }

    }
}
