//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace BrainSimulator
{
    public class Neuron
    {


        public enum modelType { Std, Color, FloatValue, LIF, Random };

        //this is only used in NeuronView but is here so you can add the tooltip when you add a neuron type and 
        //the tooltip will automatically appear in the neuron type selector combobox
        public static string[] modelToolTip = { "Integrate & Fire",
            "RGB value (no processing)",
            "Float value (no procesing",
            "Leaky Integrate & Fire",
            "Fires at random intervals"
        };

        modelType model = modelType.Std;
        public modelType Model { get => model; set => model = value; }

        string label = ""; //for convenience only...not used in computation
        public string Label { get { return label; } set { label = value; } }
        private bool keepHistory = false;

        public Neuron() { Model = modelType.Std; }
        public Neuron(int id1, modelType t = modelType.Std)
        {
            Id = id1;
            Model = t;
        }

        [XmlIgnore]
        public static readonly int threshold = 10000;
        [XmlIgnore]
        public static readonly float scaleFactor = (float)threshold;

        private long lastFired = 0;

        int id = -1;
        public int Id { get => id; set => id = value; }

        //the accumulating value of a neuron
        private int currentCharge = 0;
        public float CurrentCharge
        {
            get { return (float)currentCharge / scaleFactor; }
            set { this.currentCharge = (int)(value * scaleFactor); }
        }
        //get/set currentcharge as raw integer
        public int CurrentChargeInt { get { return currentCharge; } set { this.currentCharge = value; } }

        //the ending value of a neuron (possibly after reset)
        private int lastCharge = 0;
        public float LastCharge { get { return (float)lastCharge / scaleFactor; } set { lastCharge = (int)(value * scaleFactor); } }

        //get/set last charge as raw integer
        public int LastChargeInt { get { return lastCharge; } set { lastCharge = value; } }

        //This is the way to set up a list so it saves and loads properly from an XML file
        internal List<Synapse> synapses = new List<Synapse>();
        internal List<Synapse> synapsesFrom = new List<Synapse>();
        public List<Synapse> Synapses { get { return synapses; } }
        public List<Synapse> SynapsesFrom { get { return synapsesFrom; } }

        private long nextFiring = 0; //used only by random neurons
        private Random rand = new Random();

        public float LeakRate = 0.1f; //used only by LIF model
        public bool KeepHistory { get => keepHistory; set => keepHistory = value; }
        public long LastFired { get => lastFired; set => lastFired = value; }

        public bool Fired() { return (LastCharge > .9); }
        public void SetValue(float value) { CurrentCharge = value; }
        public void SetValueInt(int value) { LastChargeInt = CurrentChargeInt = value; }

        //a neuron is defined as in use if it has any synapses connected from/to it or it has a label
        public bool InUse()
        {
            return (synapses.Count != 0 || synapsesFrom.Count != 0 || Label != "");
        }

        public void Reset()
        {
            Label = "";
            model = modelType.Std;
            SetValue(0);
        }

        public void AddSynapse(int targetNeuron, float weight)
        {
            AddSynapse(targetNeuron, weight, null, false);
        }
        public void AddSynapse(int targetNeuron, float weight, NeuronArray theNeuronArray, bool addUndoInfo)
        {
            if (theNeuronArray == null) theNeuronArray = MainWindow.theNeuronArray;
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
            lock (n.synapsesFrom)
            {
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
        }
        public void DeleteAllSynapes()
        {
            //delete synapses out
            foreach (Synapse s in Synapses)
            {
                Neuron n = MainWindow.theNeuronArray.neuronArray[s.TargetNeuron];
                n.synapsesFrom.Remove(n.FindSynapseFrom(Id));
            }
            synapses.Clear();

            //delete synapses in
            //should delete the synapses at the source
            foreach (Synapse s in SynapsesFrom)
            {
                Neuron nTarget = MainWindow.theNeuronArray.neuronArray[s.TargetNeuron];
                nTarget.synapses.Remove(nTarget.FindSynapse(Id));
            }
            synapsesFrom.Clear();
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
            Synapse s;
            for (int i = 0; i < synapsesFrom.Count; i++)
            {
                if (synapsesFrom[i].TargetNeuron == fromNeuron)
                    return synapsesFrom[i];
            }
            return null;
        }

        bool alreadyInQueue = false;
        public void Fire1(int taskID, ref int nextQueuePtr,int[]nextQueue)
        {
            NeuronArray theNeuronArray = MainWindow.theNeuronArray;
            Neuron[] neuronArray = theNeuronArray.neuronArray;
            if (currentCharge < threshold) return;
           // Interlocked.Increment(ref MainWindow.theNeuronArray.fireCount);

            for (int i = 0; i < synapses.Count; i++)
            {
                Synapse s = synapses[i];
                //s.N.currentCharge += s.IWeight;
                int charge = Interlocked.Add(ref s.N.lastCharge, s.IWeight);
                if (charge >= threshold && !s.N.alreadyInQueue)
                {
                    nextQueue[nextQueuePtr++] = s.N.Id;
                    //theNeuronArray.AddToFiringQueue(s.N.Id, taskID);
                    s.N.alreadyInQueue = true;
                }
                if (s.N.lastCharge < 0) s.N.lastCharge = 0;
            }
            currentCharge = 0;
        }

        //check for firing
        public void Fire2(int taskID)
        {
            alreadyInQueue = false;
            NeuronArray theNeuronArray = MainWindow.theNeuronArray;
            //                case modelType.Std:
            if (lastCharge < 0) lastCharge = 0;
            currentCharge = lastCharge;
            if (lastCharge >= threshold)
            {
                lastCharge = 0;
                if (KeepHistory)
                    FiringHistory.AddFiring(Id, theNeuronArray.Generation);
                LastFired = theNeuronArray.Generation;
            }
        }
        //process the synapses
        public void Fire1()
        {
            NeuronArray theNeuronArray = MainWindow.theNeuronArray;
            switch (model)
            {
                //color and floatvalue have no cases so don't actually process TODO Change to allow diff
                case modelType.Random:
                    if (nextFiring == 0)
                    {
                        nextFiring = theNeuronArray.Generation + rand.Next(10, 30);
                    }
                    if (nextFiring == theNeuronArray.Generation)
                    {
                        currentCharge = threshold;
                        nextFiring = 0;
                    }
                    goto case modelType.Std; //continue processing as normal

                case modelType.LIF:
                case modelType.Std:
                    if (lastCharge < threshold) return;
                    Interlocked.Add(ref theNeuronArray.fireCount, 1);
                    foreach (Synapse s in synapses)
                    {
                        Neuron n = theNeuronArray.neuronArray[s.TargetNeuron];
                        Interlocked.Add(ref n.currentCharge, s.IWeight);
                    }
                    break;
            }
        }

        
        //check for firing
        public void Fire2()
        {
            NeuronArray theNeuronArray = MainWindow.theNeuronArray;
            switch (model)
            {
                case modelType.LIF:
                    if (currentCharge < 0) currentCharge = 0;
                    lastCharge = currentCharge;
                    if (currentCharge > threshold)
                    {
                        currentCharge = 0;
                        if (KeepHistory)
                            FiringHistory.AddFiring(Id, theNeuronArray.Generation);
                        LastFired = theNeuronArray.Generation;
                    }
                    if (currentCharge > 100)
                    {
                        currentCharge = (int)(currentCharge * (1 - LeakRate));
                    }
                    else
                        currentCharge = 0;
                    break;

                case modelType.Random:
                case modelType.Std:
                    if (currentCharge < 0) currentCharge = 0;
                    lastCharge = currentCharge;
                    if (currentCharge >= threshold)
                    {
                        currentCharge = 0;
                        if (KeepHistory)
                            FiringHistory.AddFiring(Id, theNeuronArray.Generation);
                        LastFired = theNeuronArray.Generation;
                    }
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

        public Neuron Clone()
        {
            Neuron n = (Neuron)this.MemberwiseClone();
            n.synapses = new List<Synapse>();
            n.synapsesFrom = new List<Synapse>(); ;
            return n;
        }
        public void Copy(Neuron n)
        {
            n.label = this.label;
            n.lastCharge = this.lastCharge;
            n.currentCharge = this.currentCharge;
            n.keepHistory = this.keepHistory;
            n.LeakRate = this.LeakRate;
            n.model = this.model;
            n.synapses = new List<Synapse>();
            n.synapsesFrom = new List<Synapse>(); ;
        }
        public void Clear()
        {
            label = "";
            currentCharge = 0;
            lastCharge = 0;
            model = modelType.Std;
            LeakRate = 0.1f;
            keepHistory = false;
            synapses = new List<Synapse>();
            synapsesFrom = new List<Synapse>(); ;
        }
    }
}
