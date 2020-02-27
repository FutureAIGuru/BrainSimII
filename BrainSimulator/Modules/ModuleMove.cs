//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BrainSimulator.Modules
{
    public class ModuleMove : ModuleBase
    {
        public override string ShortDescription { get => "Moves the entity within the simulator"; }
        public override string LongDescription
        {
            get => "The outer neurons can move the entity by pre-programmed amounts. It always moves forward or back relative to the direciton it is headed.\n\r" +
                "The center neuron can be applied with any float value to move the entity by a specified amount.\n\r" +
                "Other modules such as Simulator and Model are informed directly of the motiion. When the Simulator is informed, a collision may cancel the requested motion.";
        }

        public ModuleMove()
        {
            minHeight = 5;
            minWidth = 1;
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            float[] dist = { .1f,.025f,0,-.025f,-.1f};
            float distance = 0;


            for (int i = 0; i < na.Height; i++)
            {
                if (na.GetNeuronAt(0,i).LastCharge > 0.9)
                {
                    distance = dist[i];
                }
            }

            if (na.GetNeuronAt(0,2).CurrentCharge!= 0)
            {
                distance = na.GetNeuronAt(0,2).CurrentCharge;
                na.GetNeuronAt(0,2).SetValue(0);
            }

            ModuleRealityModel mrm = (ModuleRealityModel)FindModuleByType(typeof(ModuleRealityModel));
            if (mrm != null)
                mrm.Move(distance);

            Module3DSim m3D = (Module3DSim)FindModuleByType(typeof(Module3DSim));
            if (m3D != null && distance != 0)
                m3D.Move(distance);

            bool moved = false;
            Module2DSim m2D = (Module2DSim)FindModuleByType(typeof(Module2DSim));
            if (m2D != null && distance != 0)
                moved = m2D.Move(distance);

            Module2DModel m2DModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));
            if (m2DModel != null && moved && distance != 0)
                m2DModel.Move(distance);
        }

        public override void Initialize()
        {
            na.GetNeuronAt(0, 2).Model = Neuron.modelType.FloatValue;
            na.GetNeuronAt(0, 0).Label = "^^";
            na.GetNeuronAt(0, 1).Label = "^";
            na.GetNeuronAt(0, 3).Label = "v";
            na.GetNeuronAt(0, 4).Label = "vv";
        }
    }

}
