//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

namespace BrainSimulator.Modules
{
    public class ModuleNull : ModuleBase
    {
        //Any public variable you create here will automatically be stored with the network
        //unless you precede it with the [XmlIgnore] directive like this
        //[XlmIgnore] 
        //public theStatus = 1;
        public bool doWork = true; //this will be saved to the xml file because it's public

        //Fll this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();
            Neuron n = GetNeuron("FirstNeuron");
            if (n != null)
            {
                if (n.CurrentCharge > 0.5f)
                    n.SetValue(1);
            }
        }

        //Fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            //some dummy test dat
            //Neuron n = na.GetNeuronAt(0, 0);
            //n.Label = "FirstNeuron";
            //Neuron n1 = na.GetNeuronAt(0, 1);
            //n.AddSynapse(n1.Id, 0.5f);
        }

        public override void SetUpAfterLoad()
        {} //add code here to execute once after the module is loaded from a file
        public override void SetUpBeforeSave()
        {} //add code here to execute once before the module is saved to a file

    }
}
