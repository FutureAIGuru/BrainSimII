//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Collections.Generic;
using System.Linq;

namespace BrainSimulator
{
    public partial class NeuronArray : NeuronHandler
    {
        public string networkNotes = "";
        public bool hideNotes = false;
        public bool ShowSynapese = false;
        public long Generation = 0;
        public int EngineSpeed = 250;
        public int arraySize;
        public int rows;

        public int lastFireCount = 0;
        internal List<ModuleView> modules = new List<ModuleView>();
        public DisplayParams displayParams;

        //these have nothing to do with the NeuronArray but are here so it will be saved and restored with the network
        private bool showSynapses = false;
        public bool ShowSynapses { get => showSynapses; set => showSynapses = value; }
        public int Cols { get => arraySize / rows; }

        public List<ModuleView> Modules
        {
            get { return modules; }
        }
        public void Initialize(int count, int inRows)
        {
            rows = inRows;
            arraySize = count;
            base.Initialize(count);
        }

        //this list keeps track of changed synapses for undo
        struct SynapseUndo
        {
            public int source, target;
            public float weight;
            public bool newSynapse;
        }
        List<SynapseUndo> synapseUndoInfo = new List<SynapseUndo>();


        public NeuronArray()
        {
        }

        public new Neuron GetNeuron(int id)
        {
            Neuron n = GetCompleteNeuron(id);
            return n;
        }

        //OBSOLETE
        public void ChangeArraySize(int newSize)
        {
            //            Array.Resize(ref MainWindow.theNeuronArray.neuronArray, newSize);
        }

        public void GetCounts(out long synapseCount, out int useCount)
        {
            synapseCount = GetTotalSynapses(); ;
            useCount = GetTotalNeuronsInUse();
        }

        public new void Fire()
        {
            base.Fire();
            Generation = GetGeneration();
            lastFireCount = GetFiredCount();
            HandleProgrammedActions();
            FiringHistory.UpdateFiringHistory();
        }

        //fires all the modules
        private void HandleProgrammedActions()
        {
            //lock (modules)
            {
                for (int i = 0; i < modules.Count; i++)// each (ModuleView na in modules)
                {
                    ModuleView na = modules[i];
                    if (na.TheModule != null)
                    {
                        na.TheModule.Fire();
                    }
                }
            }
        }


        //needs a complete match
        public ModuleView FindAreaByLabel(string label)
        {
            return modules.Find(na => na.Label.Trim() == label);
        }


        public void AddSynapseUndo(int source, int target, float weight, bool newSynapse)
        {
            SynapseUndo s;
            s = new SynapseUndo
            {
                source = source,
                target = target,
                weight = weight,
                newSynapse = newSynapse
            };
            synapseUndoInfo.Add(s);
        }
        public void UndoSynapse()
        {
            if (synapseUndoInfo.Count == 0) return;
            SynapseUndo s = synapseUndoInfo.Last();
            synapseUndoInfo.Remove(s);
            Neuron n = GetNeuron(s.source);
            if (s.newSynapse)
            {
                n.DeleteSynapse(s.target);
            }
            else //TODO: not used
            {
                n.AddSynapse(s.target, s.weight, this, true);
                synapseUndoInfo.RemoveAt(synapseUndoInfo.Count - 1);
            }
        }

        public void SetNeuron(int i, Neuron n) //TODO Implement
        {
            SetCompleteNeuron(n);
        }
        public int GetNeuronIndex(int x, int y)
        {
            return x * rows + y;
        }

        public void GetNeuronLocation(int index, out int x, out int y)
        {
            x = index / rows;
            y = index % rows;
        }
    }
}
