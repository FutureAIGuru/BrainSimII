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
    public class ModuleRateDecoder3 : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited

        float theLeakRate = 0.13f;

        public ModuleRateDecoder3()
        {
            minHeight = 2;
            maxHeight = 15;
            minWidth = 6;
            maxWidth = 6;
        }

        public override string ShortDescription { get => "Similar to RateDecoder module but twice as fast"; }
        public override string LongDescription { get => "Assuming 1ms cycle and 4ms refractory, this module can differentiate serial input "+
                "by measuring the time between adjascent spikes in an input stream. The number of different levels detected is controlled " +
                "by the height of the module and it detects different interspike timings in 1ms intervals. " +
                "This module measuring the inter-spike timing for every pair of spikes whereas RateDecoder measures for every other pair of spikes." 
                ;
        }


        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            // call UpdateDialog
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            Init();
            SetUpNeurons(na.Height - 1);
        }

        private void SetUpNeurons(int levelCount)
        {
            Neuron nRd = na.GetNeuronAt(0, 0);
            nRd.Label = "Rd";  
            Neuron nIn = na.GetNeuronAt(1, 0);
            nIn.Label = "In";
            Neuron nIn1 = na.GetNeuronAt(2, 0);
            nIn1.Label = "In1";
            Neuron nClr = na.GetNeuronAt(3, 0);
            nClr.Label = "Clr";

            nRd.AddSynapse(nClr.id, 1);
            nIn.AddSynapse(nIn1.id, 1);

            for (int i = 0; i < levelCount; i++)
            {
                Neuron ni = na.GetNeuronAt(0, i + 1);
                ni.Clear();
                Neuron ni1 = na.GetNeuronAt(1, i + 1);
                ni1.Clear();
                Neuron nm = na.GetNeuronAt(2, i + 1);
                ni.Clear();
                Neuron no = na.GetNeuronAt(3, i + 1);
                no.Clear();
                no.model = Neuron.modelType.LIF;
                no.LeakRate = 0.13f;
                Neuron noP = na.GetNeuronAt(4, i + 1);
                noP.Clear();
                Neuron noN = na.GetNeuronAt(5, i + 1);
                noN.Clear();
            }

            Neuron nLast = na.GetNeuronAt(0, na.Height - 1);
            Neuron nLast1 = na.GetNeuronAt(1, na.Height - 1);
            nLast.AddSynapse(nIn1.id, 0.5f);
            nLast1.AddSynapse(nIn1.id, 0.5f);

            for (int i = 0; i < levelCount; i++)
            {
                Neuron ni = na.GetNeuronAt(0, i + 1);
                ni.Model = Neuron.modelType.LIF;
                ni.LeakRate = theLeakRate;
                Neuron ni1 = na.GetNeuronAt(1, i + 1);
                ni1.Model = Neuron.modelType.LIF;
                ni1.LeakRate = theLeakRate;
                Neuron nm = na.GetNeuronAt(2, i + 1);
                Neuron no = na.GetNeuronAt(3, i + 1);
                Neuron noP = na.GetNeuronAt(4, i + 1);
                Neuron noN = na.GetNeuronAt(5, i + 1);
                noP.Label = "O" + i;
                noN.Label = "O-" + i;


                ni.AddSynapse(nm.id, 1);
                ni1.AddSynapse(nm.id, 1);
                nm.AddSynapse(no.id, 0.1f);
                no.AddSynapse(nm.id, 1);
                nClr.AddSynapse(noN.id, 1f);
                no.AddSynapse(noN.id, -1f);
                no.AddSynapse(noP.id, 1f);


                nClr.AddSynapse(no.id, -1f);
                nRd.AddSynapse(no.id, 0.99f);

                float weight = GetWeight(4 + i);
                weight += .001f; //differentiates between < and = 
                nIn.AddSynapse(ni.id, weight);
                nIn1.AddSynapse(ni1.id, weight);

                for (int j = 0; j < levelCount; j++)
                {
                    if (j != i)
                    {
                        ni.AddSynapse(na.GetNeuronAt(0, j + 1).id, -1f);
                        ni1.AddSynapse(na.GetNeuronAt(1, j + 1).id, -1f);
                    }
                }
            }
        }

        float GetWeight(int count)
        {
            float decayFactor = (float)Math.Pow((1 - theLeakRate), count);
            float w = 1 / (1 + decayFactor);
            return w;
        }

        
        //called whenever the size of the module rectangle changes, delete if not needed
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (na == null) return; //things aren't initialized yet
            SetUpNeurons(na.Height - 1);
        }
    }
}
