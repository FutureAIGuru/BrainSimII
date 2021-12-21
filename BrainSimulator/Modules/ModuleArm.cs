//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

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
            maxHeight = 1;
            maxWidth = 6;
        }


        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();
            foreach (Neuron n in mv.Neurons)
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
                    if (mv.Label.Contains("L"))
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

        public override void Initialize()
        {
            mv.GetNeuronAt(0, 0).Model = Neuron.modelType.FloatValue;
            mv.GetNeuronAt(1, 0).Model = Neuron.modelType.FloatValue;
            AddLabels(new string[] { "X", "Y", "<", ">", "^", "V" });
        }
    }
}
