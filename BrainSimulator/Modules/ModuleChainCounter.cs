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
    public class ModuleChainCounter : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleChainCounter()
        {
            minHeight = 3;
            maxHeight = 100;
            minWidth = 3;
            maxWidth = 100;
        }

        public override string ShortDescription { get => "[Short Description Here]"; }
        public override string LongDescription { get => "[Long Description Here]"; }


        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            // UpdateDialog();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            AddSynapses();
        }


        private void AddSynapses()
        {
            Init();
            ClearNeurons(false);
            na.GetNeuronAt(0, 0).Label = "In";

            int chainWidth = 0;
            int chainHeight = 0;

            int theCount = 0;

            if (na.Height > na.Width)
            {
                chainWidth = na.Width - 2;
                chainHeight = na.Height;
                //add the count neuron labels
                theCount = na.Height;
                for (int j = 0; j < theCount; j++)
                {
                    na.GetNeuronAt(na.Width - 1, j).Label = (j + 1).ToString();
                    na.GetNeuronAt(na.Width - 2, j).Label = "i" + (j + 1).ToString();
                    na.GetNeuronAt(na.Width-2,j).Model = Neuron.modelType.LIF;
                    na.GetNeuronAt(na.Width- 2,j).LeakRate = 1f;
                }
            }
            else
            {
                chainHeight = na.Height - 2;
                chainWidth = na.Width;
                //add the count neuron labels
                theCount = na.Width;
                for (int j = 0; j < theCount; j++)
                {
                    na.GetNeuronAt(j, na.Height - 1).Label = (j + 1).ToString();
                    na.GetNeuronAt(j, na.Height - 2).Label = "i" + (j + 1).ToString();
                    na.GetNeuronAt(j, na.Height - 2).Model = Neuron.modelType.LIF;
                    na.GetNeuronAt(j, na.Height - 2).LeakRate = 1f;
                }
            }

            //add the synapses for the chain
            for (int i = 0; i < chainHeight; i++)
            {
                for (int j = 0; j < chainWidth - 1; j++)
                {
                    na.GetNeuronAt(j, i).AddSynapse(na.GetNeuronAt(j + 1, i).id, 1);
                }
                if (i < chainHeight - 1)
                {
                    na.GetNeuronAt(chainWidth - 1, i).AddSynapse(na.GetNeuronAt(0, i + 1).id, 1);
                }
            }

            //add the synapses to the count neurons
            for (int k = 0; k < theCount; k++)
            {
                float theWeight = 1 / (float)(k + 1);
                Neuron target = GetNeuron("i" + (k + 1).ToString());
                for (int i = 0; i < chainHeight; i++)
                {
                    for (int j = 0; j < chainWidth; j++)
                    {
                        na.GetNeuronAt(j, i).AddSynapse(target.id, theWeight);
                    }
                }
            }

            for (int k = 0; k < theCount; k++)
            {
                Neuron source = GetNeuron("i" + (k + 1).ToString());
                Neuron target = GetNeuron((k + 1).ToString());
                source.AddSynapse(target.id, 1);
                for (int j = k-1; j >= 0; j--)
                {
                    target = GetNeuron((j + 1).ToString());
                    source.AddSynapse(target.id, -1);
                }

            }
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            AddSynapses();
        }
    }
}
