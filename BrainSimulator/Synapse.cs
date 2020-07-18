//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

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
            N = MainWindow.theNeuronArray.GetNeuron(targetNeuron);
            Weight = weightIn;
        }
        private float weight;
        public  bool IsHebbian = false;


        [XmlIgnore]
        public Neuron N; //this is used by the engine

        [XmlIgnore]
        public int IWeight; //this is used by the engine

        public float Weight
        {
            get => weight;
            set {
                weight = value; IWeight = (int)(weight * Neuron.threshold); }
        }
        //public int IWeight
        //{
        //    get => iWeight;
        //}

        public int targetNeuron;
        public int TargetNeuron
        {
            get => targetNeuron;
            set
            {
                targetNeuron = value;
            }
        }
    }
}
