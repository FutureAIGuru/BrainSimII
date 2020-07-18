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
using static System.Math;
using System.Runtime.CompilerServices;

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

        public Neuron()
        {
            synapses = new List<Synapse>();
            synapsesFrom = new List<Synapse>();
            Model = modelType.Std;
        }
        public Neuron(bool allocateSynapses = false)
        {
            if (allocateSynapses)
            {
                synapses = new List<Synapse>();
                synapsesFrom = new List<Synapse>();
            }
            Model = modelType.Std;
        }
        public Neuron(int id1, modelType t = modelType.Std)
        {
            Id = id1;
            Model = t;
        }

        [XmlIgnore]
        public const int threshold = 10000;
        [XmlIgnore]
        public const float scaleFactor = (float)threshold;

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

        bool alreadyInQueue = false; //used by the queue-based engine 


        //get/set last charge as raw integer
        public int LastChargeInt { get { return lastCharge; } set { lastCharge = value; } }

        //This is the way to set up a list so it saves and loads properly from an XML file
        internal List<Synapse> synapses;// = new List<Synapse>();
        internal List<Synapse> synapsesFrom;// = new List<Synapse>();
        public List<Synapse> Synapses { get { return synapses; } }
        [XmlIgnore]
        public List<Synapse> SynapsesFrom { get { return synapsesFrom; } }

        //used only by random neurons
        private long nextFiring = 0;
        [ThreadStatic]
        static Random rand = new Random();

        public float LeakRate = 0.1f; //used only by LIF model
        public bool KeepHistory { get => keepHistory; set => keepHistory = value; }
        public long LastFired { get => lastFired; set => lastFired = value; }

        public bool Fired() { return (LastCharge > .9); }
        public void SetValue(float value) { CurrentCharge = value; }
        public void SetValueInt(int value) { LastChargeInt = CurrentChargeInt = value; }

        //a neuron is defined as in use if it has any synapses connected from/to it or it has a label
        public bool InUse()
        {
            return ((synapses != null && synapses.Count != 0) || (synapsesFrom != null && synapsesFrom.Count != 0) || Label != "");
        }

        public void Reset()
        {
            Label = "";
            model = modelType.Std;
            SetValue(0);
        }

        public Synapse AddSynapse(int targetNeuron, float weight)
        {
            return AddSynapse(targetNeuron, weight, null, false);
        }
        public Synapse AddSynapse(int targetNeuron, float weight, NeuronArray theNeuronArray, bool addUndoInfo)
        {

            if (theNeuronArray == null) theNeuronArray = MainWindow.theNeuronArray;
            if (targetNeuron > theNeuronArray.arraySize)
                return null;
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
            Neuron n = theNeuronArray.GetNeuron(targetNeuron);
            lock (n)
            {
                if (n.synapsesFrom == null) n.synapsesFrom = new List<Synapse>();
                Synapse s1 = n.FindSynapseFrom(Id);
                if (s1 == null)
                {
                    s1 = new Synapse(Id, weight);
                    n.synapsesFrom.Add(s1);
                }
                else
                {
                    s1.Weight = weight;
                }
            }
            return s;
        }
        public void DeleteAllSynapes()
        {
            //delete synapses out
            if (Synapses != null)
            {
                foreach (Synapse s in Synapses)
                {
                    Neuron n = MainWindow.theNeuronArray.GetNeuron(s.TargetNeuron);
                    n.synapsesFrom.Remove(n.FindSynapseFrom(Id));
                }
                synapses.Clear();
            }
            //delete synapses in
            //should delete the synapses at the source
            if (synapsesFrom != null)
            {
                foreach (Synapse s in SynapsesFrom)
                {
                    Neuron nTarget = MainWindow.theNeuronArray.GetNeuron(s.TargetNeuron);
                    nTarget.synapses.Remove(nTarget.FindSynapse(Id));
                }
                synapsesFrom.Clear();
            }
        }

        public override string ToString()
        {
            return "n" + Id;
        }
        public void DeleteSynapse(int targetNeuron)
        {
            synapses.Remove(FindSynapse(targetNeuron));
            Neuron n = MainWindow.theNeuronArray.GetNeuron(targetNeuron);
            n.synapsesFrom.Remove(n.FindSynapseFrom(Id));
        }

        public Synapse FindSynapse(int targetNeuron)
        {
            if (synapses == null) synapses = new List<Synapse>();
            Synapse s = synapses.Find(s1 => s1.TargetNeuron == targetNeuron);
            return s;
        }
        public Synapse FindSynapseFrom(int fromNeuron)
        {
            if (synapsesFrom == null) synapsesFrom = new List<Synapse>();
            for (int i = 0; i < synapsesFrom.Count; i++)
            {
                if (synapsesFrom[i].TargetNeuron == fromNeuron)
                    return synapsesFrom[i];
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fire1(int taskID, List<int> nextQueue)
        {
            NeuronArray theNeuronArray = MainWindow.theNeuronArray;
            Neuron[] neuronArray = theNeuronArray.neuronArray;
            if (lastCharge < threshold) return;

            //process all the synapses sourced by this neuron
            if (synapses != null)
                for (int i = 0; i < synapses.Count; i++)
                {
                    Synapse s = synapses[i];
                    Neuron n = s.N;
                    //Interlocked.Add(ref s.N.currentCharge, s.IWeight);
                    n.currentCharge += s.IWeight;

                    //if the target neuron needs processing, add it to the firing queue
                    if (!n.alreadyInQueue && (n.currentCharge >= threshold || n.currentCharge < 0))
                    {
                        nextQueue.Add(s.N.Id);
                        n.alreadyInQueue = true;
                    }
                    if (s.IsHebbian)
                    {
                        if (s.N.LastChargeInt >= threshold)
                        {
                            //strengthen the synapse
                            if (s.Weight < 1)
                            {
                                if (s.Weight == 0) s.Weight = .34f;
                                if (s.Weight == .34f) s.Weight = .5f;
                                if (s.Weight == .5f) s.Weight = 1f;
                            }
                        }
                        else
                        {
                            //weaken the synapse
                        }
                    }

                }
        }

        //check for firing
        public void Fire2(int taskID, List<int> nextQueue)
        {
            alreadyInQueue = false;
            NeuronArray theNeuronArray = MainWindow.theNeuronArray;
            if (currentCharge < 0) currentCharge = 0;
            lastCharge = currentCharge;
            if (lastCharge >= threshold)
            {
                currentCharge = 0;
                if (KeepHistory)
                    FiringHistory.AddFiring(Id, theNeuronArray.Generation);
                LastFired = theNeuronArray.Generation;
                nextQueue.Add(Id); //add yourself to the firing queue for next time
                alreadyInQueue = true;
            }
            //handle charge reduction of LIF model
            if (model == modelType.LIF)
            {
                if (currentCharge > 99)
                {
                    currentCharge = (int)(currentCharge * (1 - LeakRate));
                }
                else
                    currentCharge = 0;
                nextQueue.Add(Id); //alwayse keep LIF neurons on the queue
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
                        if (rand == null) rand = new Random();
                        lock (rand)
                        {
                            nextFiring = theNeuronArray.Generation + rand.Next(10, 30);
                        }
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
                    if (synapses != null)
                    {
                        foreach (Synapse s in synapses)
                        {
                            Neuron n = theNeuronArray.GetNeuron(s.TargetNeuron);
                            Interlocked.Add(ref n.currentCharge, s.IWeight);
                            if (s.IsHebbian)
                            {
                                if (n.LastChargeInt >= threshold)
                                {
                                    //strengthen the synapse
                                    if (s.Weight < 1)
                                    {
                                        if (s.Weight == 0) s.Weight = .34f;
                                        if (s.Weight == .34f) s.Weight = .5f;
                                        if (s.Weight == .5f) s.Weight = 1f;
                                    }
                                }
                                else
                                {
                                    //weaken the synapse
                                }
                            }
                        }
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
                    if (currentCharge > 99)
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
                if (theNeuronArray.GetNeuron(i) == this)
                    thisNeuron = i;
            foreach (Neuron n in theNeuronArray.Neurons())
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
