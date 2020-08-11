//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Runtime.InteropServices;

namespace BrainSimulator
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Synapse
    {
        public int targetNeuron;
        public float weight;
        public bool isHebbian;

        //a synapse connects two neurons and has a weight
        //the neuron is "owned" by one neuron and the targetNeuron is the index  of the destination neuron
        public Synapse()
        {
            targetNeuron = -1;
            weight = 1;
        }
        public Synapse(int targetNeuronIn, float weightIn, bool isHebbianIn)
        {
            targetNeuron = targetNeuronIn;
            weight = weightIn;
            isHebbian = isHebbianIn;

        }

        public float Weight
        {
            get => weight;
            set => weight = value;
        }

        public int TargetNeuron
        {
            get => targetNeuron;
            set => targetNeuron = value;
        }
        public bool IsHebbian
        {
            get => isHebbian;
            set => isHebbian = value;
        }
    }
}
