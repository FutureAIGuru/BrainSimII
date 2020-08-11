//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

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
            minWidth = 3;
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            float[] dist = { .5f, .1f, 0, -.025f, -.1f };
            float motionX = 0;
            float motionY = 0;

            if (na.Width < 3) Initialize();

            for (int i = 0; i < na.Height; i++)
            {
                if (na.GetNeuronAt(1, i).LastCharge > 0.9)
                {
                    motionX = dist[i];
                }
            }

            if (na.GetNeuronAt(1, 2).LastCharge != 0)
            {
                motionX = na.GetNeuronAt(1, 2).LastCharge;
                na.GetNeuronAt(1, 2).SetValue(0);
            }

            if (na.GetNeuronAt(0, 2).LastCharge > 0.9) motionY = 0.5f;
            if (na.GetNeuronAt(2, 2).LastCharge > 0.9) motionY = -0.5f;


            //obsolete
            ModuleRealityModel mrm = (ModuleRealityModel)FindModuleByType(typeof(ModuleRealityModel));
            if (mrm != null)
                mrm.Move(motionX);

            Module3DSim m3D = (Module3DSim)FindModuleByType(typeof(Module3DSim));
            if (m3D != null && motionX != 0)
                m3D.Move(motionX);

            bool moved = false;
            Module2DSim m2D = (Module2DSim)FindModuleByType(typeof(Module2DSim));
            if (m2D != null && (motionX != 0 || motionY != 0))
                moved = m2D.Move(motionX, motionY);

            Module2DModel m2DModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));
            if (m2DModel != null && moved && motionX != 0 || motionY != 0)
                m2DModel.Move(motionX, motionY);

            Module2DVision m2DVision = (Module2DVision)FindModuleByType(typeof(Module2DVision));
            if (m2DVision != null && motionX != 0) m2DVision.ViewChanged();

        }

        public override void Initialize()
        {
            ClearNeurons();
            if (na.Width == 1)
            {
                na.Width = MinWidth;
                na.FirstNeuron -= MainWindow.theNeuronArray.rows;
            }
            na.GetNeuronAt(1, 2).Model = Neuron.modelType.FloatValue;
            na.GetNeuronAt(1, 0).Label = "^^";
            na.GetNeuronAt(1, 1).Label = "^";
            na.GetNeuronAt(1, 3).Label = "v";
            na.GetNeuronAt(1, 4).Label = "vv";
            na.GetNeuronAt(0, 2).Label = "<";
            na.GetNeuronAt(2, 2).Label = ">";

        }
    }

}
