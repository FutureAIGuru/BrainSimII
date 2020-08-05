using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using NeuronEngine.CLI;
using System.Reflection.Emit;

namespace BrainSimulator
{
    //public class Neuron : NeuronPartial
    //{
    //    public string label = "";
    //    public List<Synapse> synapses = new List<Synapse>();
    //    public List<Synapse> synapsesFrom = new List<Synapse>();
    //}

    //[StructLayout(LayoutKind.Sequential, Pack = 1)]
    //public struct Synapse
    //{
    //    public int target;
    //    public float weight;
    //    public bool isHebbian;
    //};
    // public enum modelType { Std, Color, FloatValue, LIF, Random }; //TODO remove redundancy

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class NeuronPartial
    {
        public int id;
        public bool inUse;
        public float lastCharge;
        public float currentCharge;
        public float leakRate;
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
            int i = n.id;
            SetNeuronCurrentCharge(i, n.currentCharge);
            SetNeuronLastCharge(i, n.lastCharge);
            SetNeuronLabel(i, n.label);
            SetNeuronLeakRate(i, n.leakRate);
            SetNeuronModel(i, (int)n.model);
        }
        public Neuron GetCompleteNeuron(int i)
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

            retVal.label = GetNeuronLabel(i);
            retVal.synapses = GetSynapsesList(i);
            retVal.synapsesFrom = GetSynapsesFromList(i);
            return retVal;
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
