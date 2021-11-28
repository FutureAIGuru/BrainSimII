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
    public class ModuleRobotManual : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleRobotManual()
        {
            minHeight = 2;
            maxHeight = 500;
            minWidth = 6;
            maxWidth = 6;
        }


        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            ModuleView mv = MainWindow.theNeuronArray.FindModuleByLabel("Robot");
            if (mv == null) return;

            for (int i = 0; i < na.Height; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    if (na.GetNeuronAt(j,i).Fired())
                    {
                        Neuron nTarget = mv.GetNeuronAt(0, i);
                        switch (j)
                        {
                            case 0:
                                if (nTarget.LeakRate < 0) nTarget.LeakRate = 0;
                                else nTarget.LeakRate = -1;
                                break;
                            case 1: nTarget.SetValue(nTarget.LastCharge - 0.1f); break;
                            case 2: nTarget.SetValue(nTarget.LastCharge - 0.01f); break;
                            case 3: nTarget.SetValue(0.5f); break;
                            case 4: nTarget.SetValue(nTarget.LastCharge + 0.01f); break;
                            case 5: nTarget.SetValue(nTarget.LastCharge + 0.1f); break;
                        }
                    }
                }
            }


        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            ModuleView mv = MainWindow.theNeuronArray.FindModuleByLabel("Robot");
            if (mv == null) return;
            na.Height = mv.Height;
            if (mv != null)
            {
                for (int i = 0; i < mv.Height; i++)
                {
                    na.GetNeuronAt(0, i).Label = mv.GetNeuronAt(0, i).Label;
                    if (na.GetNeuronAt(0, i).Label != ".")
                    {
                        na.GetNeuronAt(1, i).Label = "  <<";
                        na.GetNeuronAt(2, i).Label = "   <";
                        na.GetNeuronAt(3, i).Label = "   --";
                        na.GetNeuronAt(4, i).Label = "   >";
                        na.GetNeuronAt(5, i).Label = "  >>";
                    }
                }
            }
            MainWindow.Update();
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
            if (na == null) return; //this is called the first time before the module actually exists
        }
    }
}
