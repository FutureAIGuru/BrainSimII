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

        //PRESENT VALUES:
        //assumes that highest intensity is 4 and lowest is 28 with 8 steps

        public ModuleColorComponent()
        {
            minHeight = 5;
            minWidth = 1;
        }

        public override string ShortDescription { get => "Module ColorComponent breaks a color into components."; }
        public override string LongDescription
        {
            get =>
                "The ColorComponent module has four labeled nerons that have the values of the red, green, blue and " +
                "intensity values of a color that is fed in.";
        }

        public override void Fire()
        {
            float min = 4;
            float  max = 24;
            float steps = 7;
            float variation = 2;
            Init();  //be sure to leave this here
            int theColor = na.GetNeuronAt(0, 0).LastChargeInt;
            int b = (theColor & 0x000e0) >> 5;
            int g = (theColor & 0xe000) >> 13;
            int r = (theColor & 0xe00000) >> 21;
            int i = b > g ? b : g;
            i = i > r ? i : r;
            //here rgbi have values of 0-7

            Neuron n = na.GetNeuronAt("Blu");
            na.GetNeuronAt(0, 1).AxonDelay = (int)(min + (max - b * (max / steps)));
            na.GetNeuronAt(0, 1).LeakRate = variation;
            na.GetNeuronAt(0, 2).AxonDelay = (int)(min + (max - g * (max / steps)));
            na.GetNeuronAt(0, 2).LeakRate = variation;
            na.GetNeuronAt(0, 3).AxonDelay = (int)(min + (max - r * (max / steps)));
            na.GetNeuronAt(0, 3).LeakRate = variation;
            na.GetNeuronAt(0, 4).AxonDelay = (int)(min + (max - i * (max / steps)));
            na.GetNeuronAt(0, 4).LeakRate = variation;
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
