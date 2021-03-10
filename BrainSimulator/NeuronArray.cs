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
        public long Generation = 0;
        public int EngineSpeed = 250;
        public bool EngineIsPaused = false;
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

        private int refractoryDelay = 0;
        public int RefractoryDelay 
        { get => refractoryDelay; set { refractoryDelay = value; SetRefractoryDelay(refractoryDelay); } }

        public void Initialize(int count, int inRows)
        {
            rows = inRows;
            arraySize = count;
            if (!MainWindow.useServers)
                base.Initialize(count);
        }

        //these lists keep track of changed synapses and neurons for undo
        List<UndoPoint> undoList = new List<UndoPoint>(); //checkpoints for multi-undos 
        List<SynapseUndo> synapseUndoInfo = new List<SynapseUndo>();
        List<NeuronUndo> neuronUndoInfo = new List<NeuronUndo>();

        //what can be undone?
        //synapse change/add/delete
        //neuron change
        //multi synapse actions
        //multi neuron actions
        //cut => combo (what was in the selection before?)
        //paste => combo (what was in the target before?)
        //move => combo (what was in the target AND selection before?)


        struct SynapseUndo
        {
            public int source, target;
            public float weight;
            public bool newSynapse;
            public bool delSynapse;
            public Synapse.modelType model;
        }
        struct NeuronUndo
        {
            public Neuron previousNeuron;
        }
        struct UndoPoint
        {
            public int synapsePoint;
            public int neuronPoint;
        }



        public NeuronArray()
        {
        }

        public new Neuron GetNeuron(int id)
        {
            Neuron n = GetCompleteNeuron(id);
            return n;
        }
        public Neuron GetNeuron(string label)
        {
            for (int i = 0; i < arraySize; i++)
            {
                Neuron n = GetNeuron(i);
                if (n.label == label) return n;
            }
            return null;
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
            if (MainWindow.useServers)
            {
                NeuronClient.Fire();
                lastFireCount = 0;
                for (int i = 0; i < NeuronClient.serverList.Count; i++)
                    lastFireCount += NeuronClient.serverList[i].firedCount;
                Generation = NeuronClient.serverList[0].generation;
            }
            else
            {
                base.Fire();
                Generation = GetGeneration();
                lastFireCount = GetFiredCount();
            }
            HandleProgrammedActions();
            FiringHistory.UpdateFiringHistory();
        }
        new public void AddSynapse(int src, int dest, float weight, Synapse.modelType model, bool noBackPtr)
        {
            if (MainWindow.useServers)
                NeuronClient.AddSynapse(src, dest, weight, model, noBackPtr);
            else
                base.AddSynapse(src, dest, weight, (int)model, noBackPtr);
        }
        new public void DeleteSynapse(int src, int dest)
        {
            if (MainWindow.useServers)
                NeuronClient.DeleteSynapse(src, dest);
            else
                base.DeleteSynapse(src, dest);
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

        public void SetNeuron(int i, Neuron n)
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


        /// UNDO Handling from here on out

        public void AddSynapseUndo(int source, int target, float weight,Synapse.modelType model, bool newSynapse,bool delSynapse)
        {
            SynapseUndo s;
            s = new SynapseUndo
            {
                source = source,
                target = target,
                weight = weight,
                model = model,
                newSynapse = newSynapse,
                delSynapse = delSynapse,
            };
            synapseUndoInfo.Add(s);
        }
        public void AddNeuronUndo(Neuron n)
        {
            Neuron n1 = n.Copy();
            neuronUndoInfo.Add(new NeuronUndo { previousNeuron = n1 });
        }

        public void Undo()
        {
            if (undoList.Count == 0) return;
            int synapsePoint = undoList.Last().synapsePoint;
            int neuronPoint = undoList.Last().neuronPoint;
            undoList.RemoveAt(undoList.Count - 1);

            while (neuronUndoInfo.Count > neuronPoint)
                UndoNeuron();
            while (synapseUndoInfo.Count > synapsePoint)
                UndoSynapse();
        }

        public void SetUndoPoint()
        {
            undoList.Add(new UndoPoint { synapsePoint = synapseUndoInfo.Count, neuronPoint=neuronUndoInfo.Count});
        }
        public bool UndoPossible()
        {
            return undoList.Count != 0;
        }
        private void UndoNeuron()
        {
            if (neuronUndoInfo.Count == 0) return;
            NeuronUndo n = neuronUndoInfo.Last();
            neuronUndoInfo.RemoveAt(neuronUndoInfo.Count-1);
            Neuron n1 = n.previousNeuron.Copy();
            n1.Update();
        }
        private void UndoSynapse()
        {
            if (synapseUndoInfo.Count == 0) return;
            SynapseUndo s = synapseUndoInfo.Last();
            synapseUndoInfo.RemoveAt(synapseUndoInfo.Count-1);

            Neuron n = GetNeuron(s.source);
            if (s.newSynapse) //the synapse was added so delete it
            {
                n.DeleteSynapse(s.target);
            }
            else if (s.delSynapse) //the synapse was deleted so add it back
            {
                n.AddSynapse(s.target, s.weight,s.model);
            }
            else //weight/type changed 
            {
                n.AddSynapse(s.target, s.weight,s.model);
            }
            n.Update();
        }
    }
}
