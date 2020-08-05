using System;
using System.Collections.Generic;
using System.Threading;
namespace BrainSimulator
{
    public class NeuronBase
    {
        public enum baseModelType { Std, Color, FloatValue, LIF, Random };
        public const int threshold = 10000;
        public const float scaleFactor = (float)threshold;
        public long lastFired = 0;
        int id = -1;
        public int Id { get => id; set => id = value; }

        //the accumulating value of a neuron
        public int currentCharge = 0;

        public baseModelType model = baseModelType.Std;
        public baseModelType Model { get => model; set => model = value; }
        public string label = ""; //for convenience only...not used in computation
        public string Label { get { return label; } set { label = value; } }
        public bool keepHistory = false;
        //This is the way to set up a list so it saves and loads properly from an XML file
        public List<SynapseBase> synapses;// = new List<Synapse>();
        public List<SynapseBase> synapsesFrom;// = new List<Synapse>();
        public int lastCharge = 0;  //the ending value of a neuron (possibly after reset)
        public float LastCharge { get { return (float)lastCharge / NeuronBase.scaleFactor; } set { lastCharge = (int)(value * NeuronBase.scaleFactor); } }
        public int LastChargeInt { get { return lastCharge; } set { lastCharge = value; } }
        protected bool alreadyInQueue = false; //used by the queue-based engine 
        protected int alreadyProcessedSynapses = 0;
        private long nextFiring = 0;
        public float LeakRate = 0.1f; //used only by LIF model
        public bool InUse()
        {
            return ((synapses != null && synapses.Count != 0) || (synapsesFrom != null && synapsesFrom.Count != 0) || Label != "");
        }
        public NeuronBase()
        {
            synapses = new List<SynapseBase>();
            synapsesFrom = new List<SynapseBase>();
            Model = baseModelType.Std;
        }
        public NeuronBase(bool allocateSynapses = false)
        {
            if (allocateSynapses)
            {
                synapses = new List<SynapseBase>();
                synapsesFrom = new List<SynapseBase>();
            }
            Model = baseModelType.Std;
        }
        public NeuronBase(int id1, baseModelType t = baseModelType.Std)
        {
            Id = id1;
            Model = t;
        }
        public void Fire1(NeuronArrayBase theNeuronArray, List<int> nextQueue, List<int> zeroQueue)
        {
            NeuronBase[] neuronArray = theNeuronArray.baseNeuronArray;
            if (lastCharge < threshold) return;
            if (Interlocked.CompareExchange(ref alreadyProcessedSynapses, 1, 0) == 1) return;    //this prevents duplicates in the firing queue from being processed twice
            if (synapses != null)
                for (int i = 0; i < synapses.Count; i++) //process all the synapses sourced by this neuron
                {
                    SynapseBase s = synapses[i];
                    NeuronBase n = s.N;
                    //Interlocked.Add(ref n.currentCharge, s.IWeight);
                    n.currentCharge += s.IWeight;
                    //if the target neuron needs processing, add it to the firing queue
                    if (!n.alreadyInQueue)
                    {
                        if (n.currentCharge >= threshold)// || n.currentCharge < 0))
                        {
                            n.alreadyInQueue = true; //a few duplicates are no problem because of the alreadyprocessed flag
                            nextQueue.Add(s.N.Id);  //we don't have to lock because this tread owns the queue
                        }
                    }
                    if (n.currentCharge < 0)
                    {
                        zeroQueue.Add(s.N.Id);
                    }
                }
        }
        public void Fire2(NeuronArrayBase theNeuronArray, List<int> firedQueue)
        {//check for firing
            alreadyInQueue = false;
            alreadyProcessedSynapses = 0;
            if (currentCharge < 0) currentCharge = 0;
            lastCharge = currentCharge;
            if (lastCharge >= threshold)
            {
                currentCharge = 0;
                lastFired = theNeuronArray.Generation;
                //add yourself to the firing queue for next time to reset lastCharge on the next generation
                firedQueue.Add(Id);
                alreadyInQueue = true;
            }
        }
    }
}

