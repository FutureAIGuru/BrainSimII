using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace BrainSimulator
{
    public class Neuron
    {


        public enum modelType { Std, Color, FloatValue, Perceptron, Antifeedback, Hebbian, OneTime };
        modelType model = modelType.Std;
        public modelType Model { get => model; set => model = value; }

        string label = "";
        public string Label{get { return label; }set { label = value; }}
        
        public Neuron() { Model = modelType.Std; }
        public Neuron(int id1, modelType t = modelType.Std)
        {
            Id = id1;
            Model = t;
        }

        int id = -1;
        public int Id { get => id; set => id = value; }

        //the accumulating value of a neuron
        private int currentCharge = 0;
        public float CurrentCharge { get { return (float)currentCharge / 1000f; } set { this.currentCharge = (int)(value * 1000); } }
        //get/set currentcharge as raw integer
        public int CurrentChargeInt { get { return currentCharge; } set { this.currentCharge = value; } }

        //the ending value of a neuron (possibly after reset)
        private int lastCharge = 0;
        public float LastCharge { get { return (float)lastCharge / 1000f; } set { lastCharge = (int)(value * 1000); } }

        //get/set last charge as raw integer
        public int LastChargeInt { get { return lastCharge; } set { lastCharge = value; } }

        //This is the way to set up a list so it saves and loads properly from an XML file
        internal List<Synapse> synapses = new List<Synapse>();
        internal List<Synapse> synapsesFrom = new List<Synapse>();
        public List<Synapse> Synapses { get { return synapses; } }
        public List<Synapse> SynapsesFrom { get { return synapsesFrom; } }


        public bool Fired(){return (LastCharge > .9);}
        public void SetValue(float value){LastCharge = CurrentCharge = value;}
        public void SetValueInt(int value) { LastChargeInt = CurrentChargeInt = value; }

        //a neuron is defined as in use if it has any synapses connected from/to it or it has a label
        public bool InUse()
        {
            return (synapses.Count != 0 || synapsesFrom.Count != 0 || Label != "");
        }

        public void AddSynapse(int targetNeuron, float weight, NeuronArray theNeuronArray, bool addUndoInfo = true)
        {
            if (targetNeuron > theNeuronArray.arraySize) return;
            Synapse s = FindSynapse(targetNeuron);
            if (s == null)
            {
                s = new Synapse(targetNeuron, weight);
                synapses.Add(s);
                if (addUndoInfo)
                    theNeuronArray.AddSynapseUndo(Id, targetNeuron, weight, true);
            }
            else
            {
                if (addUndoInfo)
                    theNeuronArray.AddSynapseUndo(Id, targetNeuron, s.Weight, false);
                s.Weight = weight;
            }
            //keep a list of synapses pointing to this one
            Neuron n = theNeuronArray.neuronArray[targetNeuron];
            s = n.FindSynapseFrom(Id);
            if (s == null)
            {
                s = new Synapse(Id, weight);
                n.synapsesFrom.Add(s);
            }
            else
            {
                s.Weight = weight;
            }
        }

        public void DeleteAllSynapes()
        {
            synapses.Clear();
            synapsesFrom.Clear();
            //should delete the synapses at the source!
        }

        public void DeleteSynapse(int targetNeuron)
        {
            synapses.Remove(FindSynapse(targetNeuron));
            Neuron n = MainWindow.theNeuronArray.neuronArray[targetNeuron];
            n.synapsesFrom.Remove(n.FindSynapseFrom(Id));
        }

        public Synapse FindSynapse(int targetNeuron)
        {
            Synapse s = synapses.Find(s1 => s1.TargetNeuron == targetNeuron);
            return s;
        }
        public Synapse FindSynapseFrom(int fromNeuron)
        {
            Synapse s = synapsesFrom.Find(s1 => s1.TargetNeuron == fromNeuron);
            return s;
        }

        //process the synapses
        public int LastSynapse = -1;
        public void Fire1(NeuronArray theNeuronArray)
        {
            switch (Model)
            {
                //color floatvalue have not cases so don't actually process
                case modelType.Std:
                    if (lastCharge < 990) return;
                    Interlocked.Add(ref theNeuronArray.fireCount, 1);
                    foreach (Synapse s in synapses)
                    {
                        Neuron n = theNeuronArray.neuronArray[s.TargetNeuron];
                        Interlocked.Add(ref n.currentCharge, (int)(s.Weight * 1000));
                    }
                    break;

                case modelType.Antifeedback:
                    // just like a std neuron but can't stimulate the neuron chich stimulated it
                    if (lastCharge < 990) return;
                    Interlocked.Add(ref theNeuronArray.fireCount, 1);
                    foreach (Synapse s in synapses)
                    {
                        if (s.TargetNeuron == LastSynapse)
                        { LastSynapse = -1; }
                        else
                        {
                            Neuron n = theNeuronArray.neuronArray[s.TargetNeuron];
                            Interlocked.Add(ref n.currentCharge, (int)(s.Weight * 1000));
                            if (n.currentCharge > 990 && s.Weight > 0.5f && n.Id != Id)
                                n.LastSynapse = Id;
                        }
                    }
                    break;

                case modelType.Hebbian:
                    //as the synapse weights are representative of correlation counts, choose the largest.
                    if (lastCharge < 990) return;
                    Interlocked.Add(ref theNeuronArray.fireCount, 1);
                    float maxWeight = 0;
                    if (synapses.Count > 0)
                        maxWeight = synapses.Max(t => t.Weight);
                    if (maxWeight > 0)
                    {
                        foreach (Synapse s in synapses)
                        {
                            if (s.Weight == maxWeight)
                            {
                                Neuron n = theNeuronArray.neuronArray[s.TargetNeuron];
                                Interlocked.Add(ref n.currentCharge, 1000); ;
                            }
                        }
                    }
                    break;

                //fire if sufficient weights on this cycle or cancel...do not accumulate weight across multiple cyceles
                case modelType.OneTime:
                    if (lastCharge < 990) return;
                    Interlocked.Add(ref theNeuronArray.fireCount, 1);
                    foreach (Synapse s in synapses)
                    {
                        Neuron n = theNeuronArray.neuronArray[s.TargetNeuron];
                        Interlocked.Add(ref n.currentCharge, (int)(s.Weight * 1000));// (int)(weight * 1000));
                    }
                    break;

                case modelType.Perceptron:
                    //implement this!!!!
                    if (lastCharge < 990) return;
                    Interlocked.Add(ref theNeuronArray.fireCount, 1);
                    foreach (Synapse s in synapses)
                    {
                        Neuron n = theNeuronArray.neuronArray[s.TargetNeuron];
                        Interlocked.Add(ref n.currentCharge, (int)(s.Weight * 1000));
                    }
                    break;
            }

        }
        //check for firing
        public void Fire2(long generation)
        {
            switch (Model)
            {
                case modelType.Std:
                case modelType.Antifeedback:
                    if (currentCharge < 0) currentCharge = 0;
                    lastCharge = currentCharge;
                    if (currentCharge > 990) currentCharge = 0;
                    break;

                //fire if sufficient weights on this cycle or cancel...do not accumulate weight across multiple cyceles
                case modelType.OneTime:
                    lastCharge = currentCharge;
                    currentCharge = 0;
                    break;

                case modelType.Perceptron:
                    break;
                case modelType.Hebbian: //as the synapse weights are representative of correlation counts, choose the largest.
                    break;
            }

        }

        public int SynapsesTo(NeuronArray theNeuronArray)
        {
            int count = 0;
            int thisNeuron = -1;
            for (int i = 0; i < theNeuronArray.arraySize; i++)
                if (theNeuronArray.neuronArray[i] == this)
                    thisNeuron = i;
            foreach (Neuron n in theNeuronArray.neuronArray)
                foreach (Synapse s in n.synapses)
                    if (s.TargetNeuron == thisNeuron)
                        count++;
            return count;
        }
    }
}
