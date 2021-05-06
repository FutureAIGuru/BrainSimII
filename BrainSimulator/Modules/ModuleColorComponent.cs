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

        //PRESENT VALUES:
        //assumes that highest intensity is 4 and lowest is 28 with 8 steps

        public override string ShortDescription { get => "Converts a color neuron to 4 rate-encoded neurons"; }
        public override string LongDescription
        {
            get =>
                "This module receives input from the Touch and Vision modules and merges the information to maintain a representation of " +
                "physical objects in the entity's environment. It also supports imaingation via the temporary addition of imagined objects " + "" +
                "and the temporary change in point of view.\r\n" +
                "\r\n" +
                "";
        }


        public ModuleColorComponent()
        {
            minHeight = 5;
            minWidth = 1;
        }

        public override void Fire()
        {
            float min = 4;
            float  max = 24;
            float steps = 7;
            float variation = 0.5F;
            Init();  //be sure to leave this here
            int theColor = na.GetNeuronAt(0, 0).LastChargeInt;
            int b = (theColor & 0x000ff) >> 0;
            int g = (theColor & 0xff00) >> 8;
            int r = (theColor & 0xff0000) >> 16;

            float luminance = 0.2126f * r + 0.7152f * g + 0.0722f * b;
            int i = (int) luminance;
            //here rgbi have values 0-255

            b /= 32;
            g /= 32;
            r /= 32;
            i /= 32;
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
