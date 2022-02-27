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
        public enum modelType { Fixed, Binary, Hebbian1, Hebbian2 };

        public int targetNeuron;
        public float weight;
        public modelType model = modelType.Fixed;

        //this is only used in SynapseView but is here so you can add the tooltip when you add a synapse type and 
        //the tooltip will automatically appear in the synapse type selector combobox
        public static string[] modelToolTip = { "Fixed Weight",
            "Binary, one-shot learning",
            "Hebbian w/ range [0,1]",
            "Hebbian w/ range [-1,1]",
            "UNDER DEVELOPMENT, Do not use! Hebbian w/ range [-1/n,1/n] where n is number of synapses",
        };

        //a synapse connects two neurons and has a weight
        //the neuron is "owned" by one neuron and the targetNeuron is the index  of the destination neuron
        public Synapse()
        {
            targetNeuron = -1;
            weight = 1;
        }
        public Synapse(int targetNeuronIn, float weightIn, modelType modelIn)
        {
            targetNeuron = targetNeuronIn;
            weight = weightIn;
            model = modelIn;
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
    }
}
