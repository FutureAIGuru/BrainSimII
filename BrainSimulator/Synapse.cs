using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public class Synapse
    {
        //a synapse connects two neurons and has a weight
        //the neuron is "owned" by one neuron and the targetNeuron is the index  of the destination neuron
        public Synapse()
        {
            targetNeuron = -1;
            weight = -1;
        }
        public Synapse(int targetNeuronIn, float weightIn)
        {
            targetNeuron = targetNeuronIn;
            weight = weightIn;
        }
        private float weight;
        public float Weight
        {
            get => weight;
            set => weight = value;
        }

        private int targetNeuron;
        public int TargetNeuron
        {
            get => targetNeuron;
            set => targetNeuron = value;
        }
    }
}
