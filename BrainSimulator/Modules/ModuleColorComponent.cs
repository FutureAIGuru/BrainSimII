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
using System.Windows.Controls;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleColorComponent : ModuleBase
    {

        //PRESENT VALUES:
        //assumes that highest intensity is 4 and lowest is 28 with 8 steps

        public ModuleColorComponent()
        {
            minHeight = 5;
            minWidth = 1;
            maxHeight = 5;
            maxWidth = 1;
        }


        float min = 4; // replace with refractory period
        float steps = 4;
        float variation = 0.000f;// .2F;
        public override void Fire()
        {
            Init();  //be sure to leave this here

            int theColor = na.GetNeuronAt(0, 0).LastChargeInt;
            float r = (theColor & 0xff0000) >> 16;
            float g = (theColor & 0xff00) >> 8;
            float b = (theColor & 0x000ff) >> 0;

            float luminance = 0.2126f * r + 0.7152f * g + 0.0722f * b;
            float i = luminance;
            //here rgbi have values 0-255

            r /= 255;
            g /= 255;
            b /= 255;
            i /= 255;
            //here rgbi have values of 0-1
            r = 1 - r;
            g = 1 - g;
            b = 1 - b;
            i = 1 - i;
           

            r = min + r * steps;
            g = min + g * steps;
            b = min + b * steps;
            i = min + i * steps;

            Neuron nR = na.GetNeuronAt("Red");
            Neuron nG = na.GetNeuronAt("Grn");
            Neuron nB = na.GetNeuronAt("Blu");
            Neuron nI = na.GetNeuronAt("Int");
            if (nR == null) return;
            if (nG == null) return;
            if (nB == null) return;
            if (nI == null) return;
            nR.AxonDelay = (int)r;
            nR.LeakRate = variation;
            nG.AxonDelay = (int)g;
            nG.LeakRate = variation;
            nB.AxonDelay = (int)b;
            nB.LeakRate = variation;
            nI.AxonDelay = (int)i;
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

        public override MenuItem GetCustomMenuItems()
        {
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new Label { Content = "Steps:" });
            TextBox tbSteps = new TextBox {Name="Steps", Text = steps.ToString(), VerticalAlignment = VerticalAlignment.Center, Width = 40 };
            tbSteps.TextChanged += TbSteps_TextChanged;
            sp.Children.Add(tbSteps);
            sp.Children.Add(new Label { Content = "Variation:" });
            TextBox tbVariation = new TextBox {Name="Variation", Text = variation.ToString(), VerticalAlignment = VerticalAlignment.Center, Width = 40 };
            tbVariation.TextChanged += TbSteps_TextChanged;
            sp.Children.Add(tbVariation);
            MenuItem mi = new MenuItem { Header = sp,StaysOpenOnClick=true };
            return mi;
        }

        private void TbSteps_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (tb.Name == "Steps")
                {
                    int.TryParse(tb.Text, out int steps1);
                    steps = (int)steps1;
                }
                if (tb.Name == "Variation")
                {
                    float.TryParse(tb.Text, out variation);
                }
            }
        }
    }
}
