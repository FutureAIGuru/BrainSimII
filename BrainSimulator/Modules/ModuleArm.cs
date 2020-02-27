//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleArm : ModuleBase
    {
        //any public variable you create here will automatically be stored with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        public ModuleArm()
        {
            minHeight = 1;
            minWidth = 6;
        }
        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();
            na.BeginEnum();
            for (Neuron n = na.GetNextNeuron(); n != null; n = na.GetNextNeuron())
            {
                if (n.Fired())
                {
                    switch (n.Label)
                    {
                        case "^":
                            SetNeuronValue(null, "X", GetNeuronValue(null, "X") + 0.1f);
                            break;
                        case "V":
                            SetNeuronValue(null, "X", GetNeuronValue(null, "X") - 0.1f);
                            break;
                        case "<":
                            SetNeuronValue(null, "Y", GetNeuronValue(null, "Y") + 0.1f);
                            break;
                        case ">":
                            SetNeuronValue(null, "Y", GetNeuronValue(null, "Y") - 0.1f);
                            break;
                    }
                    if (GetNeuronValue(null, "X") < 0) SetNeuronValue(null, "X", 0);
                    if (GetNeuronValue(null, "X") > 0.8) SetNeuronValue(null, "X", 0.8f);
                    if (na.Label.Contains("L"))
                    {
                        if (GetNeuronValue(null, "Y") < 0) SetNeuronValue(null, "Y", 0);
                        if (GetNeuronValue(null, "Y") > 0.8) SetNeuronValue(null, "Y", 0.8f);
                    }
                    else
                    {
                        if (GetNeuronValue(null, "Y") > 0) SetNeuronValue(null, "Y", 0);
                        if (GetNeuronValue(null, "Y") < -0.8) SetNeuronValue(null, "Y", -0.8f);
                    }
                }
            }
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        //move this to the neuronarray
        public Neuron AddLabel(string newLabel)
        {
            na.BeginEnum();
            for (Neuron n = na.GetNextNeuron(); n != null; n=na.GetNextNeuron())
            {
                if (n == null) return null;
                if (n.Label == newLabel) return n;
                if (n.Label == "")
                {
                    n.Label = newLabel;
                    return n;
                }
            }
            return null;
        }
        private void AddLabels (string[] labels)
        {
            foreach (string label in labels)
                AddLabel(label);
        }

        public override void Initialize()
        {
            na.GetNeuronAt(0, 0).Model = Neuron.modelType.FloatValue;
            na.GetNeuronAt(1, 0).Model = Neuron.modelType.FloatValue;
            AddLabels(new string[] { "X","Y", "<", ">", "^", "V" });
        }
    }
}
