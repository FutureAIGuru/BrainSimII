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
    public class ModulePatternGenerator : ModuleBase
    {
        Random rand = new Random();
        uint[] patterns = {0x0,0x1,0x2,0x4,0x8 };
        uint counter = 0;

        public override string ShortDescription { get => "TO DO: Short description of Module PatternGenerator."; }
        public override string LongDescription
        {
            get =>
                "TO DO: Long description of Module PatternGenerator.";
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here
            if (GetNeuronValue("Enable") == 1)
            {
                Neuron n = GetNeuron("Output");
                if (n == null || n.synapses.Count == 0) return;
                int startID = n.synapses[0].targetNeuron;
                //for up to 32 bits
                byte[] buf = new byte[4];
                uint randNumber;
                if (GetNeuronValue("Patterns") == 0)
                {
                    rand.NextBytes(buf);
                    randNumber = BitConverter.ToUInt32(buf, 0);
                    if (randNumber > uint.MaxValue / 2)
                    {
                        randNumber = patterns[rand.Next(patterns.Length)];
                    }
                    counter++;
                    if (counter > 15) counter = 0;
                    randNumber = counter;
                }
                else
                {
                    randNumber = patterns[rand.Next(patterns.Length)];
                }

                for (int i = 0; i < 4; i++)
                {
                    uint bit = (uint)(randNumber & (1 << i));
                    if (bit == 0)
                        MainWindow.theNeuronArray.GetNeuron(startID + i).SetValue(0);
                    else
                        MainWindow.theNeuronArray.GetNeuron(startID + i).SetValue(1);
                }
            }
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            AddLabel("Enable");
            Neuron n = AddLabel("Patterns");
            n.AddSynapse(n.id, 1);
            AddLabel("Output");

        }
    }
}
