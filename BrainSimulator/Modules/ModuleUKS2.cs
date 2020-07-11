//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleUKS2 : ModuleUKS
    {
        static public long immediateMemory = 2; //items are more-or-less simultaneous
        static public long shortTermMemory = 10;
        public override string ShortDescription => "A Knowledge Graph UKS module expanded with a neuron arrays for intputs and outputs.";
        public override string LongDescription => "This is like a UKS module but expanded to be accessible via neuron firings instead of just programmatically. " + base.LongDescription;

        List<Thing> activeSequences = new List<Thing>();

        //once for each cycle of the engine
        public override void Fire()
        {
            base.Fire();
            PlayActiveSequences();
        }

        // did a neuron fire?
        private bool NeuronFired(string label)
        {
            Neuron n = na.GetNeuronAt(label);
            if (n == null) return false;
            return n.Fired();
        }


        //this returns the most recent firing regardless of how long ago it was
        Thing MostRecentFired(Thing t)
        {
            if (t == null) return null;
            Thing retVal = null;
            long firedAt = 0;
            foreach (Thing child in t.Children)
            {
                int i = UKS.IndexOf(child);
                Neuron n = GetNeuron(i);
                if (n != null)
                {
                    if (n.LastFired > firedAt)
                    {
                        retVal = child;
                        firedAt = n.LastFired;
                    }
                }
            }
            return retVal;
        }

        public List<Thing> AnyChildrenFired(Thing t, long maxCycles = 1, long minCycles = 1, bool checkInput = true, bool checkDescendents = false)
        {
            List<Thing> retVal = new List<Thing>();
            if (t == null) return retVal;
            foreach (Thing child in t.Children)
            {
                if (Fired(child, maxCycles, checkInput, minCycles))
                    retVal.Add(child);
                if (checkDescendents)
                    retVal.AddRange(AnyChildrenFired(child, maxCycles, minCycles));
            }
            return retVal;
        }

        public override Thing Valued(object value, List<Thing> KBt = null, float toler = 0)
        {
            Thing retVal = base.Valued(value, KBt, toler);
            if (retVal != null)
            {
                Fire(retVal);
            }
            return retVal;
        }


        private void UpdateNeuronLabels()
        {
            if (na == null) return;
            for (int i = 0; i < na.NeuronCount / 2 && i < UKS.Count; i++)
            {
                if (UKS[i] == null) continue;
                GetNeuron(i).Label = UKS[i].Label;
            }
            for (int i = UKS.Count; i < na.NeuronCount / 2; i++)
            {
                GetNeuron(i).Label = "";
            }
        }
        public override Thing AddThing(string label, Thing parent, object value = null, Thing[] references = null)
        {
            Thing retVal = base.AddThing(label, parent, value, references);
            UpdateNeuronLabels();
            return retVal;
        }
        public override Thing AddThing(string label, Thing[] parents, object value = null, Thing[] references = null)
        {
            Thing retVal = base.AddThing(label, parents, value, references);
            UpdateNeuronLabels();
            return retVal;
        }

        private Neuron PrevNeuron(Neuron n)
        {
            na.GetNeuronLocation(n, out int col, out int row);
            row--;
            if (row < 0)
            {
                col -= 2;
                row += na.Height;
            }
            return na.GetNeuronAt(col, row);
        }

        public override void DeleteThing(Thing t)
        {
            int i = UKS.IndexOf(t);
            base.DeleteThing(t);
            //because we removed a node, any external synapses to related neurons need to be adjusted to point to the right place
            //on any neurons which might have shifted.
            if (i > -1)
            {
                for (int j = i + 1; j < UKS.Count; j++)
                {
                    Neuron sourceNeuron = GetNeuron(j,false);
                    Neuron targetNeuron = PrevNeuron(sourceNeuron);
                    MainWindow.thisWindow.theNeuronArrayView.MoveOneNeuron(sourceNeuron, targetNeuron);
                    sourceNeuron = GetNeuron(j, true);
                    targetNeuron = PrevNeuron(sourceNeuron);
                    MainWindow.thisWindow.theNeuronArrayView.MoveOneNeuron(sourceNeuron, targetNeuron);
                }
            }
            UpdateNeuronLabels();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            base.Initialize();
            ClearNeurons();
            foreach (Neuron n in na.Neurons())
            {
                n.DeleteAllSynapes();
            }

            //since we are inserting at 0, these are in reverse order

            UpdateNeuronLabels();
        }


        //return neuron associated with KB thing
        public Neuron GetNeuron(Thing t, bool input = true)
        {
            int i = UKS.IndexOf(t);
            if (i == -1) return null;
            return GetNeuron(i, input);
        }

        Neuron GetNeuron(int i, bool input = true)
        {
            int col = i / na.Height;
            int row = i % na.Height;
            col = col * 2;
            if (!input) col++;
            return na.GetNeuronAt(col, row);
        }


        //fire the neuron associated with a KB thing
        public void Fire(Thing t, bool fireInput = true) //false fires the neuron in the output array
        {
            int i = UKS.IndexOf(t);
            if (i == -1) return;
            Neuron n = GetNeuron(i, fireInput);
            n.SetValue(1);
            t.useCount++;
        }

        //did the neuron associated with a thing fire?  
        //firing at least minPastCycles but not more than maxPastCycles ago
        public bool Fired(Thing t, long maxPastCycles = 1, bool firedInput = true, long minPastCycles = 0)
        {
            int i = UKS.IndexOf(t);
            Neuron n = GetNeuron(i, firedInput);
            if (n == null) return false;
            long timeSinceLastFire = MainWindow.theNeuronArray.Generation - n.LastFired;
            bool retVal = timeSinceLastFire <= maxPastCycles;
            if (retVal)
                retVal = timeSinceLastFire >= minPastCycles;
            return retVal;
        }

        public void Play(Thing t)
        {
            if (!activeSequences.Contains(t))
                activeSequences.Add(t);
        }
        public bool DoneWithPlay()
        {
            return activeSequences.Count == 0;
        }

        //this handles playing sequences of actions (like saying a phrase)
        //These are fired one-per-cycle until the end of the sequence
        //If a reference has a weight of -1, the system waits until it fires.
        private void PlayActiveSequences()
        {
            if (activeSequences.Count == 0) return;
//            for (int i = 0; i >= 0 && i < activeSequences.Count; i++)
            {
                Thing activeSequence = activeSequences[0];
                if (activeSequence.currentReference == activeSequence.References.Count)
                {
                    activeSequence.currentReference = 0;
                    activeSequences.RemoveAt(0);
                }
                else if (activeSequence.References[activeSequence.currentReference].weight == -1)
                { //wait for firing of the specified thing
                    if (Fired(activeSequence.References[activeSequence.currentReference].T))
                    {
                        activeSequence.currentReference++;
                    }
                }
                else
                {
                    //fire the specified thing
                    Thing thingToFire = activeSequence.References[activeSequence.currentReference++].T;
                    if (thingToFire != null)
                        Fire(thingToFire, false);
                }
            }
        }

        //we'll want to change this to consider use-count and recency
        public void Forget(Thing t, int countToSave = 3)
        {
            if (t == null) return;
            while (t.Children.Count > countToSave)
            {
                DeleteThing(t.Children[0]);
            }
        }

        public void BuildAssociation(Thing source, Thing dest, int maxCycles = 1, int minCycles = 0, int delta = 0)
        {
            if (source == null || dest == null) return;
            List<Thing> sources = AnyChildrenFired(source, maxCycles, minCycles, true);
            List<Thing> dests = AnyChildrenFired(dest, maxCycles + delta, minCycles + delta, true);
            foreach (Thing t in sources)
                foreach (Thing t1 in dests)
                    t.AdjustReference(t1);
        }

        //this learns associations between words and Events
        private void LearnWordLinks()
        {
            List<Thing> words = GetChildren(Labeled("Word"));
            foreach (Thing word in words)
            {
                if (Fired(word, immediateMemory))
                {
                    List<Thing> colors = GetChildren(Labeled("Color"));
                    foreach (Thing t in colors)
                    {
                        if (Fired(t, immediateMemory))
                            word.AdjustReference(t, 1); //Hit
                        else
                            word.AdjustReference(t, -1); //Miss
                    }
                }
            }
        }
    }
}
