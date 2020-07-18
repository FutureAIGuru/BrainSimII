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
using System.Windows.Controls;
using System.Windows.Documents;

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

        //older unpaged neuron array
        public Neuron[] neuronArray;

        //big paged memory  Because a bug prevents big single array allocations
        bool usePagedNeuronArray = true;
        const int pageSize = 250 * 250;
        List<List<Neuron>> pages = new List<List<Neuron>>();

        public long Generation { get; set; } = 0;

        public DisplayParams displayParams;

        //notes about this network
        public string networkNotes = "";
        public bool hideNotes = false;

        //these have nothing to do with the NeuronArray but are here so it will be saved and restored with the network
        private int engineSpeed = 250;
        public int EngineSpeed { get => engineSpeed; set => engineSpeed = value; }
        private bool showSynapses = false;
        public bool ShowSynapses { get => showSynapses; set => showSynapses = value; }


        //keeps track of the number of neurons which fired in this generation
        [XmlIgnore]
        public long fireCount = 0;
        [XmlIgnore]
        public long lastFireCount = 0;


        //Variables for the Neuron Firing Queue
        static int queuesize = 10000000;

        //how many parallel tasks and the array to hold them
        const int taskCount = 256;
        int taskBusyCount = 0;
        Task[] engineTask = new Task[taskCount];

        List<int> firingQueue = new List<int>(queuesize);
        List<List<int>> nextQueue = new List<List<int>>(taskCount);
        List<int> manualFire = new List<int>();


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
            usePagedNeuronArray = false;
        }


        public void Initialize(int theSize, int theRows, Neuron.modelType t = Neuron.modelType.Std)
        {
            arraySize = theSize;
            rows = theRows;
            pages.Clear();
            showSynapses = MainWindow.showSynapses;
            int pagesNeeded = arraySize / pageSize;
            int remainder = arraySize % pageSize;
            if (remainder != 0) pagesNeeded++;
            if (arraySize > pageSize)
            {
                usePagedNeuronArray = true;
                for (int i = 0; i < pagesNeeded; i++)
                    pages.Add(new List<Neuron>(pageSize));
                neuronArray = null;
                Parallel.For(0, pagesNeeded, j =>
                {
                    for (int i = 0; i < pageSize; i++)
                        pages[j].Add(new Neuron(i + j * pageSize, t));
                });
            }
            else
            {
                usePagedNeuronArray = false;
                neuronArray = new Neuron[arraySize];
                for (int i = 0; i < pages.Count; i++)
                    pages[i] = null;
                for (int i = 0; i < arraySize; i++)
                    neuronArray[i] = new Neuron(i, t);
            }
        }

        public int GetInterimNeuronCount()
        {
            int retVal = 0;
            if (usePagedNeuronArray)
            {
                for (int i = 0; i < pages.Count; i++)
                    retVal += pages[i].Count;
            }
            else
                retVal = arraySize;
            return retVal;
        }

        public Neuron GetNeuron(int i)
        {
            if (usePagedNeuronArray)
            {
                int page = i / pageSize;
                int offset = i % pageSize;
                return pages[page][offset];
            }
            return neuronArray[i];
        }
        public void SetNeuron(int i, Neuron n)
        {
            if (usePagedNeuronArray)
            {
                int page = i / pageSize;
                int offset = i % pageSize;
                if (offset < pages[page].Count)
                    pages[page][offset] = n;
                else
                    pages[page].Add(n);
                return;
            }
            else
                neuronArray[i] = n;
        }
        public int GetArraySize()
        {
            return arraySize;
        }
        public IEnumerable<Neuron> Neurons()
        {
            for (int i = 0; i < GetArraySize(); i++)
                yield return GetNeuron(i);
        }

        //OBSOLETE
        public void ChangeArraySize(int newSize)
        {
            Array.Resize(ref MainWindow.theNeuronArray.neuronArray, newSize);
        }

        public void GetCounts(out int synapseCount, out int useCount)
        {
            synapseCount = 0;
            useCount = 0;
            if (usePagedNeuronArray)
            {
                List<int> subSynapse = new List<int>();
                List<int> subInUse = new List<int>();
                for (int i = 0; i < pages.Count; i++)
                {
                    subSynapse.Add(0);
                    subInUse.Add(0);
                }
                Parallel.For(0, pages.Count, i =>
                {
                    for (int j = 0; j < pages[i].Count; j++)
                    {
                        if (pages[i][j].synapses != null)
                            subSynapse[i] += pages[i][j].Synapses.Count;
                        if (pages[i][j].InUse()) subInUse[i]++;
                    }
                });
                for (int i = 0; i < pages.Count; i++)
                {
                    synapseCount += subSynapse[i];
                    useCount += subInUse[i];
                }
            }
            else
            {
                for (int i = 0; i < arraySize; i++)
                {
                    synapseCount += neuronArray[i].synapses.Count;
                    if (neuronArray[i].InUse()) useCount++;
                }
            }
        }

        void ProcessFiringQueue(int id)
        {
            Utils.MessWithAffinity(id);

            //determine the portion of the firing queue this thread should handle
            int taskID = id;
            int start, end;
            GetBounds(taskID, out start, out end);

            //first-phase neuron processing
            List<int> queue = nextQueue[taskID];

            for (int i = start; i < end; i++)
            {
                if (firingQueue[i] != -1)
                    GetNeuron(firingQueue[i]).Fire2(taskID, queue);
            }
        }
        void ProcessFiringQueue1(int id)
        {
            Utils.MessWithAffinity(id);
            //determine the portion of the firing queue this thread should handle
            int taskID = id;
            int start, end;
            GetBounds(taskID, out start, out end);

            //first-phase neuron processing
            List<int> queue = nextQueue[taskID];

            for (int i = start; i < end; i++)
            {
                if (firingQueue[i] != -1)
                {
                    GetNeuron(firingQueue[i]).Fire1(taskID, queue);
                }
            }
        }

        private void GetBounds(int taskID, out int start, out int end)
        {
            int numberToProcess = firingQueue.Count / taskCount;
            int remainder = firingQueue.Count % taskCount;
            start = numberToProcess * taskID;
            end = start + numberToProcess;
            if (taskID < remainder)
            {
                start += taskID;
                end = start + numberToProcess + 1;
            }
            else
            {
                start += remainder;
                end += remainder;
            }
        }

        public void AddToFiringQueue(int neuronID)
        {
            if (!usePagedNeuronArray) return;
            manualFire.Add(neuronID);
            MainWindow.theNeuronArray.GetNeuron(neuronID).LastCharge = 1;
            MainWindow.theNeuronArray.GetNeuron(neuronID).CurrentCharge = 1;
        }

        //gives every neuron a chance to fire once
        public void Fire()
        {
            HandleProgrammedActions();
            lastFireCount = fireCount;
            fireCount = 0;
            if (usePagedNeuronArray)
            {
                //allocate the next generation firing queues
                if (nextQueue.Count == 0)
                {
                    for (int i = 0; i < taskCount; i++)
                        nextQueue.Add(new List<int>(queuesize / taskCount));
                }

                //put any manually-fired neurons onto the firing queue
                firingQueue.Clear();
                firingQueue.AddRange(manualFire);
                manualFire.Clear();

                //add all the neurons which fired in the previous generation to the firing queue
                for (int i = 0; i < taskCount; i++)
                {
                    firingQueue.AddRange(nextQueue[i]);
                    nextQueue[i].Clear();
                }

                //TODO...replace with alreadyUsed bit
                ////sort the array so we can discard duplicates
                //firingQueue.Sort();
                //int dupcount = 0;
                //for (int i = 0; i < firingQueue.Count - 1; i++)
                //{
                //    if (firingQueue[i] == firingQueue[i + 1])
                //    {
                //        firingQueue[i] = -1;
                //        dupcount++;
                //    }
                //}
                fireCount = firingQueue.Count;// - dupcount;

                //spin off the paralel tasks to run the engine on the firing queue
                ParallelOptions x = new ParallelOptions { MaxDegreeOfParallelism = 128 };
                Parallel.For(0, taskCount,x, i => ProcessFiringQueue(i));
                Parallel.For(0, taskCount,x, i => ProcessFiringQueue1(i));

            }
            else
            {
                //when debugging the Fire1 & Fire2 modules disable parallel operation and use the sequential loops below;
                if (arraySize >= pageSize)
                {
                    int lastpage = arraySize % pageSize;
                    Parallel.For(0, pages.Count - 1, i =>
                    {
                        for (int j = 0; j < pages[i].Count; j++)
                            pages[i][j].Fire1();
                    });
                    Parallel.For(0, pageSize, j => neuronArray[j].Fire1());
                    Parallel.For(0, pages.Count - 1, i =>
                    {
                        for (int j = 0; j < pages[i].Count; j++)
                            pages[i][j].Fire2();
                    });
                    Parallel.For(0, pageSize, j => neuronArray[j].Fire2());
                }
                else
                {
                    Parallel.For(0, arraySize, i => neuronArray[i].Fire1());
                    Parallel.For(0, arraySize, i => neuronArray[i].Fire2());
                }
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
            Neuron n = GetNeuron(s.source);
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
            for (int i = 0; i < GetArraySize(); i++)
            {
                Neuron n = GetNeuron(i);
                n.Id = i;
                if (n.synapses != null)
                {
                    for (int j = 0; j < n.synapses.Count; j++)
                    {
                        Synapse s = n.synapses[j];
                        if (s.TargetNeuron == -1)
                        {
                            n.synapses.RemoveAt(j);
                            j--;
                        }
                    }
                }
                if (n.SynapsesFrom != null)
                {
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
            }
            for (int i = 0; i < GetArraySize(); i++)
            {
                Neuron n = GetNeuron(i);
                if (n.Synapses != null)
                {
                    foreach (Synapse s in n.Synapses)
                    {
                        Neuron nTarget = GetNeuron(s.TargetNeuron);
                        Synapse s1 = nTarget.SynapsesFrom.Find(s2 => s2.TargetNeuron == i);
                        if (s1 == null)
                            nTarget.SynapsesFrom.Add(new Synapse(i, s.Weight));
                    }
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
