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
    public class ModuleColorComponent : ModuleBase
    {
        //any public variable you create here will automatically be stored with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            int theColor = na.GetNeuronAt(0, 0).LastChargeInt;
            int b = (theColor & 0x000f0) >> 4;
            int g = (theColor & 0xf000) >> 12;
            int r = (theColor & 0xf00000) >> 20;
            int i = b > g ? b : g;
            i = i > r ? i : r;
            na.GetNeuronAt(0, 1).AxonDelay = 16 - b;
            na.GetNeuronAt(0, 2).AxonDelay = 16 - g;
            na.GetNeuronAt(0, 3).AxonDelay = 16 - r;
            na.GetNeuronAt(0, 4).AxonDelay = 16 - i;
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            na.GetNeuronAt(0, 1).Label = "Blu";
            na.GetNeuronAt(0, 2).Label = "Grn";
            na.GetNeuronAt(0, 3).Label = "Red";
            na.GetNeuronAt(0, 4).Label = "Int";
            na.GetNeuronAt(0, 0).Model = Neuron.modelType.Color;
            na.GetNeuronAt(0, 1).Model = Neuron.modelType.Random;
            na.GetNeuronAt(0, 2).Model = Neuron.modelType.Random;
            na.GetNeuronAt(0, 3).Model = Neuron.modelType.Random;
            na.GetNeuronAt(0, 4).Model = Neuron.modelType.Random;
        }
    }
}
