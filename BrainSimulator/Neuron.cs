//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Windows;
using System.Xml.Serialization;

namespace BrainSimulator
{
    public class Neuron : NeuronPartial
    {
        public string label = "";

        public List<Synapse> synapses = new List<Synapse>();
        public List<Synapse> synapsesFrom = new List<Synapse>();
        NeuronArray ownerArray = MainWindow.theNeuronArray;

        //this is only used in NeuronView but is here so you can add the tooltip when you add a neuron type and 
        //the tooltip will automatically appear in the neuron type selector combobox
        public static string[] modelToolTip = { "Integrate & Fire",
            "RGB value (no processing)",
            "Float value (no procesing",
            "Leaky Integrate & Fire",
            "Fires at random intervals",
            "Fires a burst,",
            "Always fire",
        };

        public NeuronArray Owner { get => ownerArray; set => ownerArray = value; }

        //IMPORTANT:
        //Lastcharge is a stable readable value of the output of a neuron
        //CurrentCharge is an internal accumulation variable which must be set for the engine to act on a neuron's value
        //"Color" neurons and "FloatValue" neurons are special in that last and current should always be the same.langu
        public float CurrentCharge
        {
            get { return (float)lastCharge; }
            set { currentCharge = value; }
        }

        //get/set last charge. Setting also sets current charge

        //TODO: change this to SetValue
        public float LastCharge { get { return (float)lastCharge; } set { lastCharge = value; } }// Update(); } }

        //get/set last charge as raw integer Used by COLOR nueurons
        public int LastChargeInt { get { return (int)lastCharge; } set { lastCharge = value; Update(); } }
        public void SetValueInt(int value) { LastChargeInt = value; Update(); }

        public List<Synapse> Synapses { get => synapses; }
        [XmlIgnore]
        public List<Synapse> SynapsesFrom { get => synapsesFrom; }

        public long LastFired { get => lastFired; }

        public bool Fired() { return (LastCharge >= 1); }
        //public void SetValue(float value) { lastCharge = value; currentCharge = value; Update(); }
        public void SetValue(float value)
        {
            currentCharge = value;
            if (model == modelType.FloatValue)
                lastCharge = value;
            Update();
        }

        public enum modelType { IF, Color, FloatValue, LIF, Random, Burst, Always };

        public int Id { get => id; set => id = value; }

        public const string toolTipSeparator = "||?";
        public string Label
        {
            get
            {
                string theLabel = ownerArray.GetLabelFromCache(Id);
                if (theLabel != "")
                {
                    int tooltipStart = theLabel.IndexOf(toolTipSeparator);
                    if (tooltipStart != -1)
                        theLabel = theLabel.Substring(0, tooltipStart);
                }
                return theLabel;
            }
            set
            {
                string theLabel = ownerArray.GetLabelFromCache(Id);
                int tooltipStart = theLabel.IndexOf(toolTipSeparator);
                if (tooltipStart != -1)
                    label = value + toolTipSeparator + theLabel.Substring(tooltipStart + toolTipSeparator.Length);
                else
                    label = value;

                if (label == toolTipSeparator) label = "";
                if (label != "")
                    ownerArray.AddLabelToCache(Id, label);
                else
                    ownerArray.RemoveLabelFromCache(Id);

                //changing a label forces a neuron to update on the screen even if it is not inUse
                if (label != "")
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                       {
                           MainWindow.arrayView.AddNeuronToUpdateList(id);
                       });
                }
            }
        }
        public string ToolTip
        {
            get
            {
                string theToolTip = ownerArray.GetLabelFromCache(Id);
                int tooltipStart = theToolTip.IndexOf(toolTipSeparator);
                if (tooltipStart != -1)
                    theToolTip = theToolTip.Substring(tooltipStart + toolTipSeparator.Length);
                else
                    theToolTip = "";
                return theToolTip;
            }
            set
            {
                string theToolTip = ownerArray.GetLabelFromCache(Id);
                string theLabel = theToolTip;
                if (theLabel.Contains(toolTipSeparator))
                    theLabel = theLabel.Substring(0, theLabel.IndexOf(toolTipSeparator));

                if (value != "")
                    label = theLabel + toolTipSeparator + value;
                else
                    label = theLabel;

                if (label == toolTipSeparator) label = "";
                if (label != "")
                    ownerArray.AddLabelToCache(Id, label);
                else
                    ownerArray.RemoveLabelFromCache(Id);
            }
        }

        public bool RecordHistory
        {
            get
            {
                return FiringHistory.NeuronIsInFiringHistory(id);
            }
            set
            {
                if (value)
                {
                    FiringHistory.AddNeuronToHistoryWindow(id);
                    NeuronView.OpenHistoryWindow();
                }
                else
                    FiringHistory.RemoveNeuronFromHistoryWindow(id);
            }
        }

        public bool ShowSynapses
        {
            get
            {
                return MainWindow.arrayView.IsShowingSynapses(id);
            }
            set
            {
                if (value)
                    MainWindow.arrayView.AddShowSynapses(id);
                else
                    MainWindow.arrayView.RemoveShowSynapses(id);
            }
        }

        public float LeakRate { get => leakRate; set { leakRate = value; Update(); } }
        public int AxonDelay
        {
            get => axonDelay;
            set { axonDelay = value; Update(); }
        }

        public modelType Model { get => (Neuron.modelType)model; set { model = (modelType)value; Update(); } }

        public void Update()
        {
            ownerArray.SetCompleteNeuron(this);
        }
        public Neuron()
        {
            if (ownerArray == null)
                ownerArray = MainWindow.myClipBoard;
        }

        //a neuron is defined as in use if it has any synapses connected from/to it or it has a label
        public bool InUse()
        {
            return ((Synapses != null && Synapses.Count != 0) || (SynapsesFrom != null && SynapsesFrom.Count != 0) || Label != "");
        }

        public void Reset()
        {
            Label = "";
            model = modelType.IF;
            SetValue(0);
        }

        public void AddSynapse(int targetNeuron, float weight, Synapse.modelType model)
        {
            ownerArray.AddSynapse(Id, targetNeuron, weight, model, false);
        }
        public void AddSynapse(int targetNeuron, float weight)
        {
            ownerArray.AddSynapse(Id, targetNeuron, weight, Synapse.modelType.Fixed, false);
        }

        public void AddSynapseWithUndo(int targetNeuron, float weight, Synapse.modelType model = Synapse.modelType.Fixed)
        {
            //TODO, first check to see if a synapse already exists and save the old weight
            Synapse s = FindSynapse(targetNeuron);
            if (s != null)
                MainWindow.theNeuronArray.AddSynapseUndo(Id, targetNeuron, s.weight, s.model, false, false);
            else
                MainWindow.theNeuronArray.AddSynapseUndo(Id, targetNeuron, 0, Synapse.modelType.Fixed, true, false);

            ownerArray.AddSynapse(Id, targetNeuron, weight, model, false);

        }

        public void AddUndoInfo()
        {
            MainWindow.theNeuronArray.AddNeuronUndo(this);
        }

        public void DeleteAllSynapes(bool deleteOutgoing = true, bool deleteIncoming = true)
        {
            if (deleteOutgoing)
            {
                foreach (Synapse s in Synapses)
                    DeleteSynapse(s.targetNeuron);
                Synapses.Clear();
            }
            if (deleteIncoming)
            {
                foreach (Synapse s in synapsesFrom)
                {
                    ownerArray.DeleteSynapse(s.targetNeuron, id);
                }
                synapsesFrom.Clear();
            }
        }

        public override string ToString()
        {
            return "n:" + Id;
        }
        public void DeleteSynapseWithUndo(int targetNeuron)
        {
            Synapse s = FindSynapse(targetNeuron);
            if (s != null)
                MainWindow.theNeuronArray.AddSynapseUndo(id, targetNeuron, s.weight, s.model, false, true);

            DeleteSynapse(targetNeuron);
        }
        public void DeleteSynapse(int targetNeuron)
        {
            ownerArray.DeleteSynapse(Id, targetNeuron);
        }

        public Synapse FindSynapse(int targetNeuron)
        {
            if (Synapses == null) return null;
            for (int i = 0; i < Synapses.Count; i++)
            {
                if (((Synapse)Synapses[i]).TargetNeuron == targetNeuron)
                    return (Synapse)Synapses[i];
            }
            return null;
        }
        public Synapse FindSynapseFrom(int fromNeuron)
        {
            if (SynapsesFrom == null) return null;
            for (int i = 0; i < SynapsesFrom.Count; i++)
            {
                if (((Synapse)SynapsesFrom[i]).TargetNeuron == fromNeuron)
                    return (Synapse)SynapsesFrom[i];
            }
            return null;
        }

        public Neuron Clone()
        {
            Neuron n = (Neuron)this.MemberwiseClone();
            n.label = Label;
            n.synapses = new List<Synapse>();
            n.synapsesFrom = new List<Synapse>();
            n.RecordHistory = RecordHistory;
            n.ShowSynapses = ShowSynapses;
            return n;
        }
        //copy this content to n
        public void Copy(Neuron n)
        {
            n.Label = this.Label;
            n.ToolTip = this.ToolTip;
            n.lastCharge = this.lastCharge;
            n.currentCharge = this.currentCharge;
            n.LeakRate = this.LeakRate;
            n.axonDelay = this.axonDelay;
            n.model = this.model;
            n.RecordHistory = this.RecordHistory;
            n.ShowSynapses = this.ShowSynapses;
            n.synapsesFrom = new List<Synapse>(); ;
        }
        public Neuron Copy()
        {
            Neuron n = new Neuron();
            n.id = this.id;
            n.label = this.label;
            n.lastCharge = this.lastCharge;
            n.currentCharge = this.currentCharge;
            n.LeakRate = this.LeakRate;
            n.axonDelay = this.axonDelay;
            n.model = this.model;
            n.RecordHistory = this.RecordHistory;
            n.ShowSynapses = this.ShowSynapses;
            return n;
        }
        public void ClearWithUndo()
        {
            MainWindow.theNeuronArray.AddNeuronUndo(this);
            for (int i = 0; i < synapses.Count; i++)
            {
                DeleteSynapseWithUndo(synapses[i].targetNeuron);
            }
            for (int i = 0; i < synapsesFrom.Count; i++)
            {
                MainWindow.theNeuronArray.GetNeuron(synapsesFrom[i].targetNeuron).
                    DeleteSynapseWithUndo(this.id);
            }
            Clear();
        }
        public void Clear()
        {
            Label = "";
            ToolTip = "";
            currentCharge = 0;
            lastCharge = 0;
            model = modelType.IF;
            LeakRate = 0.1f;
            AxonDelay = 0;
            DeleteAllSynapes();
            MainWindow.theNeuronArray.SetCompleteNeuron(this);
            synapses = new List<Synapse>();
            synapsesFrom = new List<Synapse>();
            RecordHistory = false;
            ShowSynapses = false;
        }
    }
}
