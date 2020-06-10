//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using BrainSimulator.Modules;
using System.Collections.Concurrent;
using System.Threading;
using System;

namespace BrainSimulator
{
    public partial class NeuronArray
    {

        public int arraySize = 10000; // ten thousand neurons to start
        public int rows = 100;

        public int Cols { get => arraySize / rows; }

        internal List<ModuleView> modules = new List<ModuleView>();
        public List<ModuleView> Modules
        {
            get { return modules; }
        }

        public Neuron[] neuronArray;
        public long Generation { get; set; } = 0;

        public DisplayParams displayParams;

        //notes about this network
        public string networkNotes = "";
        public bool hideNotes = false;

        //these have nothing to do with the NeuronArray but is here so it will be saved and restored with the network
        private int engineSpeed = 250;
        public int EngineSpeed { get => engineSpeed; set => engineSpeed = value; }
        private bool showSynapses = false;
        public bool ShowSynapses { get => showSynapses; set => showSynapses = value; }


        //keeps track of the number of neurons which fired in this generation
        [XmlIgnore]
        public long fireCount = 0;
        [XmlIgnore]
        public long lastFireCount = 0;

        //this list keeps track of changing synapses
        struct SynapseUndo
        {
            public int source, target;
            public float weight;
            public bool newSynapse;
        }
        List<SynapseUndo> synapseUndoInfo = new List<SynapseUndo>();

        public NeuronArray()
        { }


        public NeuronArray(int theSize, int theRows, Neuron.modelType t = Neuron.modelType.Std)
        {
            arraySize = theSize;
            rows = theRows;
            showSynapses = MainWindow.showSynapses;
            neuronArray = new Neuron[arraySize];
            Parallel.For(0, arraySize, i => neuronArray[i] = new Neuron(i, t));
        }

        void Fire1(int id)
        {
            int taskID = id;
            for (int i = taskID; i < insPtr; i += taskCount)
            {
                if (firingQueue[i] != -1)
                    neuronArray[firingQueue[i]].Fire2(taskID);
            }
            Interlocked.Add(ref taskBusyCount, -1);
            var spin = new SpinWait();
            while (true)
            {
                if (taskBusyCount == 0) break;
                spin.SpinOnce();
            }
            for (int i = taskID; i < insPtr; i += taskCount)
            {
                if (firingQueue[i] != -1)
                    neuronArray[firingQueue[i]].Fire1(taskID,ref nextQueuePtr[taskID],nextQueue[taskID]);
            }
        }

        //this is not used because it is now included in the neuron code
        public void AddToFiringQueue(int neuronID, int taskID)
        {
            nextQueue[taskID][nextQueuePtr[taskID]++] = neuronID;
        }
        //this is used 
        public void AddToFiringQueue(int neuronID)
        {
            if (!useFiringList)return;
            manualFire.Add(neuronID);
        }

        bool useFiringList = false;
        const int queuesize = 4000000;
        const int taskCount = 16;
        int taskBusyCount = 0;
        Task[] engineTask = new Task[taskCount];
        int[] firingQueue = new int[queuesize];
        int insPtr = 0;
        int[][] nextQueue = new int[taskCount][];
        int[] nextQueuePtr = new int[taskCount];
        List<int> manualFire = new List<int>();

        public void Fire()
        {
            HandleProgrammedActions();
            lastFireCount = fireCount;
            fireCount = 0;
            int dupcount = 0;
            if (useFiringList)
            {
                if (nextQueue[0] == null)
                {
                    for (int i = 0; i < taskCount; i++)
                        nextQueue[i] = new int[2*queuesize/taskCount];
                }
                insPtr = manualFire.Count;
                Array.Copy(manualFire.ToArray(), firingQueue, manualFire.Count);
                manualFire.Clear();
                for (int i = 0; i < taskCount; i++)
                {
                    for (int j = 0; j < nextQueuePtr[i]; j++)
                        firingQueue[insPtr++] = nextQueue[i][j];
                }
                Array.Sort(firingQueue, 0, insPtr);
                for (int i = 0; i < insPtr - 1; i++)
                {
                    if (firingQueue[i] == firingQueue[i + 1])
                    {
                        firingQueue[i] = -1;
                        dupcount++;
                    }
                }
                fireCount = insPtr - dupcount;
                //for (int i = 0; i < neuronArray.Length; i++)
                //    firingQueue.Add(i);

                taskBusyCount = taskCount;
                for (int i = 0; i < taskCount; i++)
                {
                    nextQueuePtr[i] = 0;
                    int tempID = i;
                    engineTask[i] = Task.Factory.StartNew(() => { Fire1(tempID); });
                }
                Task.WaitAll(engineTask);
            }
            else
            {
                //when debugging the Fire1 & Fire2 modules disable parallel operation and use the sequential loops below;
                Parallel.For(0, arraySize, i => neuronArray[i].Fire1());
                Parallel.For(0, arraySize, i => neuronArray[i].Fire2());

                //use these instead
                //foreach ()
                //    n.Fire1(this,Generation);
                //foreach ()
                //    n.Fire2(Generation);
            }
            Generation++;
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

        //looks for a beginning match only
        private ModuleView FindAreaByCommand(string command)
        {
            return modules.Find(na => na.CommandLine.IndexOf(command) == 0);
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
            Neuron n = neuronArray[s.source];
            if (s.newSynapse)
            {
                n.DeleteSynapse(s.target);
            }
            else
            {
                n.AddSynapse(s.target, s.weight, this, true);
                synapseUndoInfo.RemoveAt(synapseUndoInfo.Count - 1);
            }
        }

        public void CheckSynapseArray()
        {
            for (int i = 0; i < neuronArray.Length; i++)
            {
                Neuron n = neuronArray[i];
                n.Id = i;
                for (int j = 0; j < n.synapses.Count; j++)
                {
                    Synapse s = n.synapses[j];
                    if (s.TargetNeuron == -1)
                    {
                        n.synapses.RemoveAt(j);
                        j--;
                    }
                }
                for (int j = 0; j < n.synapsesFrom.Count; j++)
                {
                    Synapse s = n.synapsesFrom[j];
                    if (s.TargetNeuron == -1)
                    {
                        n.synapsesFrom.RemoveAt(j);
                        j--;
                    }
                }
            }
            for (int i = 0; i < neuronArray.Length; i++)
            {
                Neuron n = neuronArray[i];
                foreach (Synapse s in n.Synapses)
                {
                    Neuron nTarget = neuronArray[s.TargetNeuron];
                    Synapse s1 = nTarget.SynapsesFrom.Find(s2 => s2.TargetNeuron == i);
                    if (s1 == null)
                        nTarget.SynapsesFrom.Add(new Synapse(i, s.Weight));
                }
            }
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
