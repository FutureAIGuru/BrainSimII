//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;

namespace BrainSimulator.Modules
{
    public class ModuleTurn : ModuleBase
    {

        public ModuleTurn()
        {
            minHeight = 1;
            minWidth = 5;
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable

            double[] rotation = { -Math.PI / 2, -Math.PI / 24, 0, Math.PI / 24, Math.PI / 2 };

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

            Module2DVision m2DVision = (Module2DVision)FindModuleByType(typeof(Module2DVision));
            if (m2DVision != null && direction != 0) m2DVision.ViewChanged();

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
