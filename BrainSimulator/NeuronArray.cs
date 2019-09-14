//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public partial class NeuronArray
    {

#if DEBUG
        public int arraySize = 10000; // ten thousand neurons to start
        public int rows = 100;
#else
        public int arraySize = 1000000; // a million neurons to start
        public int rows = 1000;
#endif
        internal List<NeuronArea> areas = new List<NeuronArea>();
        public List<NeuronArea> Areas
        {
            get { return areas; }
        }

        public Neuron[] neuronArray;
        public long Generation { get; set; } = 0;

        //keeps track of the number of neurons which fired in this generation
        public long fireCount = 0;
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
        {
            neuronArray = new Neuron[arraySize];
            Parallel.For(0, arraySize, i => neuronArray[i] = new Neuron(i));

            //for (int i = 0; i < neuronArray.Length; i++)
            //{
            //    Neuron n = new Neuron(i);
            //    neuronArray[i] = n;
            //}
        }

        public NeuronArray(int theSize, int theRows,Neuron.modelType t=Neuron.modelType.Std)
        {
            arraySize = theSize;
            rows = theRows;

            neuronArray = new Neuron[arraySize];
            Parallel.For(0, arraySize, i => neuronArray[i] = new Neuron(i,t)) ;
        }

 
        public void Fire()
        {
            HandleProgrammedActions();
            lastFireCount = fireCount;
            fireCount = 0;

            //when debugging the Fire1 & Fire2 modules disable parallel operation and use the sequential loops below;
            Parallel.For(0, arraySize, i => neuronArray[i].Fire1(this));
            Parallel.For(0, arraySize, i => neuronArray[i].Fire2(Generation));

            //use these instead
            //foreach (Neuron n in neuronArray)
            //    n.Fire1(this);
            //foreach (Neuron n in neuronArray)
            //    n.Fire2(Generation);
            Generation++;
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
                n.AddSynapse(s.target, s.weight, this);
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
