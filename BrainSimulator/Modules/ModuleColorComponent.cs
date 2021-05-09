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

        public ModuleColorComponent()
        {
            minHeight = 4;
            minWidth = 1;
            maxHeight = 4;
            maxWidth = 1;
        }

        public override string ShortDescription { get => "Module ColorComponent breaks a color into components."; }
        public override string LongDescription
        {
            get =>
                "The ColorComponent module has four labeled nerons that have the values of the red, green, blue and " +
                "intensity values of a color that is fed in. This somewhat emulates the signals which would be generated " +
                "by cells in the retina.";
        }

        public override void Fire()
        {
            float min = 4; // replace with refractory period
            float steps = 7;
            float variation = 0.2F;
            Init();  //be sure to leave this here

            int theColor = na.GetNeuronAt(0, 0).LastChargeInt;
            float r = (theColor & 0xff0000) >> 16;
            float g = (theColor & 0xff00) >> 8;
            float b = (theColor & 0x000ff) >> 0;
            float luminance = 0.2126f * r + 0.7152f * g + 0.0722f * b;
            int i = (int)luminance;
            //here rgbi have values 0-255

            r /= 255;
            g /= 255;
            b /= 255;
            i /= 255;
            //here rgbi have values of 0-1
            r *= r;
            g *= g;
            b *= b;
            r = 1 - r;
            g = 1 - g;
            b = 1 - b;
            i = 1 - i;

            Neuron nR = na.GetNeuronAt("Red");
            Neuron nG = na.GetNeuronAt("Grn");
            Neuron nB = na.GetNeuronAt("Blu");
            Neuron nI = na.GetNeuronAt("Int");
            nR.AxonDelay = (int)(min + r * steps);
            nR.LeakRate = variation;
            nG.AxonDelay = (int)(min + g * steps);
            nG.LeakRate = variation;
            nB.AxonDelay = (int)(min + b * steps);
            nB.LeakRate = variation;
            nI.AxonDelay = (int)(min + i * steps);
            nI.LeakRate = variation;
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
