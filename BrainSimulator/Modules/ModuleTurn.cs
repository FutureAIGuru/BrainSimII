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
    public class ModuleTurn : ModuleBase
    {
        public override string ShortDescription { get => "Rotates the entity within the simulator"; }
        public override string LongDescription
        {
            get => "The outer neurons can rotate the entity by pre-programmed amounts.\n\r" +
                "The center neuron can be applied with any float value to rotate the entity by a specified amount." +
                "Other modules such as Simulator and Model are informed directly of the rotation.";
        }

        public ModuleTurn()
        {
            minHeight = 1;
            minWidth = 5;
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable

            double[] rotation = { -Math.PI / 6, -Math.PI / 12, 0, Math.PI / 12, Math.PI / 6 };

            float direction = 0;

            for (int i = 0; i < na.Width; i++)
            {
                if (na.GetNeuronAt(i, 0).LastCharge > 0.9 || na.GetNeuronAt(i, 0).LastCharge > 0.9 && i < rotation.Length)
                {
                    direction = (float)rotation[i];
                }
            }
            if (na.GetNeuronAt(2, 0).LastCharge != 0)
            {
                direction = na.GetNeuronAt(2, 0).LastCharge;
                na.GetNeuronAt(2, 0).SetValue(0);
            }

            ModuleRealityModel mrm = (ModuleRealityModel)FindModuleByType(typeof(ModuleRealityModel));
            if (mrm != null && direction != 0)
                mrm.Rotate(20);

            Module2DSim m2D = (Module2DSim)FindModuleByType(typeof(Module2DSim));
            if (m2D != null && direction != 0)
                m2D.Rotate(direction);

            Module3DSim m3D = (Module3DSim)FindModuleByType(typeof(Module3DSim));
            if (m3D != null && direction != 0)
                m3D.Rotate(direction);

            Module2DModel m2DModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));
            if (m2DModel != null && direction != 0)
                m2DModel.Rotate(direction);
        }

        public override void Initialize()
        {
            na.GetNeuronAt(2, 0).Model = Neuron.modelType.FloatValue;
            na.GetNeuronAt(0, 0).Label = "<<";
            na.GetNeuronAt(1, 0).Label = "<";
            na.GetNeuronAt(3, 0).Label = ">";
            na.GetNeuronAt(4, 0).Label = ">>";
        }
    }
}
