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
using System.Windows.Media;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleGrayScale : ModuleBase
    {
        public override void Fire()
        {
            Init();  //be sure to leave this here
            ModuleView naSource = theNeuronArray.FindAreaByLabel("ModuleImageFile");

            if (naSource != null)
                foreach (Neuron n in na.Neurons())
                {
                    na.GetNeuronLocation(n.id, out int x, out int y);
                    int x1 = (int)(x * (naSource.Width - 1) / (float)(na.Width - 1));
                    int y1 = (int)(y * (naSource.Height - 1) / (float)(na.Height - 1));
                    Neuron n1 = naSource.GetNeuronAt(x1, y1);
                    Color c = Utils.IntToColor(n1.LastChargeInt);
                    float newValue = 0;
                    newValue = c.R * .21f;
                    newValue += c.G * .71f;
                    newValue += c.B * .071f;
                    newValue /= 256;
                    n.SetValue(newValue);

                }

        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            foreach (Neuron n in na.Neurons())
                n.Model = Neuron.modelType.FloatValue;
        }
    }
}
