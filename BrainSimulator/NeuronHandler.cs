using NeuronEngine.CLI;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BrainSimulator
{

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class NeuronPartial
    {
        public int id;
        public bool inUse;
        public float lastCharge;
        public float currentCharge;
        public float leakRate;
        public int axonDelay;
        public Neuron.modelType model;
        public long lastFired;
    };

    public class NeuronHandler : NeuronArrayBase
    {
        public NeuronPartial GetPartialNeuron(int i)
        {
            return ConvertToNeuron(GetNeuron(i));
        }
        public void SetCompleteNeuron(Neuron n)
        {
            if (MainWindow.useServers)
            {
                NeuronClient.SetNeuron(n);
            }
            else
            {
                int i = n.id;
                SetNeuronCurrentCharge(i, n.currentCharge);
                SetNeuronLastCharge(i, n.lastCharge);
                SetNeuronLabel(i, n.label);
                SetNeuronLeakRate(i, n.leakRate);
                SetNeuronModel(i, (int)n.model);
                SetNeuronAxonDelay(i, n.axonDelay);
            }
        }
        public Neuron GetNeuronForDrawing(int i)
        {
            if (MainWindow.useServers)
            {
                Neuron retVal = NeuronClient.GetNeuron(i);
                return retVal;
            }
            else
            {
                Neuron retVal = new Neuron();
                retVal.id = i;
                retVal.LastCharge = GetNeuronLastCharge(i);
                retVal.inUse = GetNeuronInUse(i);
                retVal.label = GetNeuronLabel(i);
                retVal.model = (Neuron.modelType)GetNeuronModel(i);
                retVal.leakRate = GetNeuronLeakRate(i);
                retVal.axonDelay = GetNeuronAxonDelay(i);
                return retVal;
            }
        }
        public Neuron AddSynapses(Neuron n)
        {
            if (MainWindow.useServers)
            {
                n.synapses = NeuronClient.GetSynapses(n.id);
                n.synapsesFrom = NeuronClient.GetSynapsesFrom(n.id);
            }
            return n;
        }
        public Neuron GetCompleteNeuron(int i)
        {
            if (MainWindow.useServers)
            {
                Neuron retVal = NeuronClient.GetNeuron(i);
                //retVal.synapses = NeuronClient.GetSynapses(i);
                //retVal.synapsesFrom = NeuronClient.GetSynapsesFrom(i);
                return retVal;
            }
            else
            {
                NeuronPartial n = GetPartialNeuron(i);

                Neuron retVal = new Neuron();

                retVal.id = n.id;
                retVal.currentCharge = n.currentCharge;
                retVal.lastCharge = n.lastCharge;
                retVal.lastFired = n.lastFired;
                retVal.inUse = n.inUse;
                retVal.leakRate = n.leakRate;
                retVal.model = n.model;
                retVal.axonDelay = n.axonDelay;

                retVal.label = GetNeuronLabel(i);
                retVal.synapses = GetSynapsesList(i);
                retVal.synapsesFrom = GetSynapsesFromList(i);
                return retVal;
            }
        }
        public List<Synapse> GetSynapsesList(int i)
        {
            return ConvertToSynapseList(GetSynapses(i));
        }
        public List<Synapse> GetSynapsesFromList(int i)
        {
            return ConvertToSynapseList(GetSynapsesFrom(i));
        }
        List<Synapse> ConvertToSynapseList(byte[] input)
        {
            List<Synapse> retVal = new List<Synapse>();
            Synapse s = new Synapse();
            int sizeOfSynapse = Marshal.SizeOf(s);
            int numberOfSynapses = input.Length / sizeOfSynapse;
            byte[] oneSynapse = new byte[sizeOfSynapse];
            for (int i = 0; i < numberOfSynapses; i++)
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

        NeuronPartial ConvertToNeuron(byte[] input)
        {
            NeuronPartial n = new NeuronPartial();
            GCHandle handle = GCHandle.Alloc(input, GCHandleType.Pinned);
            n = (NeuronPartial)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(NeuronPartial));
            handle.Free();
            return n;
        }
    }
}
