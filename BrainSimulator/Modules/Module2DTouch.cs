//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Windows;

namespace BrainSimulator.Modules
{
    public class Module2DTouch : ModuleBase
    {
        public Module2DTouch()
        {
            minWidth = 12;
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            Module2DModel naModel = (Module2DModel)FindModleu(typeof(Module2DModel));
            if (naModel == null) return;

            for (int i = 0; i < mv.Height; i++)
            {
                //neurons:  0:touch   1:antAngle  2:antDistance 3: sensedLineAngle 4: conf1 5: len1 6: conf2 7: len2 8: touch-ended 9: modelchanged
                if (mv.GetNeuronAt(0, i).CurrentCharge == 0) continue;
                float antDist = mv.GetNeuronAt(1, i).CurrentCharge;
                float antAngle = mv.GetNeuronAt(2, i).CurrentCharge;
                float lineAngle = mv.GetNeuronAt(3, i).CurrentCharge;
                float p1IsEndpt = mv.GetNeuronAt(4, i).CurrentCharge;
                float l1 = mv.GetNeuronAt(5, i).CurrentCharge;
                float p2IsEndpt = mv.GetNeuronAt(6, i).CurrentCharge;
                float l2 = mv.GetNeuronAt(7, i).CurrentCharge;
                float mR = mv.GetNeuronAt(9, i).CurrentCharge;
                float mTheta = mv.GetNeuronAt(10, i).CurrentCharge;
                float mPhi = mv.GetNeuronAt(11, i).CurrentCharge;

                PointPlus motion = new PointPlus() { R = mR, Theta = mTheta, Conf = mPhi };
                //create the line segment (all coordinates relative to self)
                PointPlus antennaPos = new PointPlus()
                { R = antDist, Theta = antAngle };

                float lineAngleAbs = antAngle - lineAngle;

                PointPlus pv1 = new PointPlus() { R = l1, Theta = (float)Math.PI + lineAngleAbs };
                PointPlus pv2 = new PointPlus() { R = l2, Theta = lineAngleAbs };

                Point P1 = antennaPos.P + pv1.V;
                Point P2 = antennaPos.P + pv2.V;
                PointPlus P1P = new PointPlus() { P = P1, Conf = 1 - p1IsEndpt };
                PointPlus P2P = new PointPlus() { P = P2, Conf = 1 - p2IsEndpt };

                bool modelChanged = naModel.AddSegmentFromTouch(P1P, P2P, motion, i);

            }
        }
        public override void Initialize()
        {
            ClearNeurons();
            mv.GetNeuronAt(0, 0).Label = "Right";
            mv.GetNeuronAt(0, 1).Label = "Left";
            mv.GetNeuronAt(1, 0).Label = "R";
            mv.GetNeuronAt(2, 0).Label = "θ";
            mv.GetNeuronAt(3, 0).Label = "Collθ";
            mv.GetNeuronAt(4, 0).Label = "P1";
            mv.GetNeuronAt(5, 0).Label = "L1";
            mv.GetNeuronAt(6, 0).Label = "P2";
            mv.GetNeuronAt(7, 0).Label = "L2";
            mv.GetNeuronAt(8, 0).Label = "Done►";
            mv.GetNeuronAt(9, 0).Label = "mR";
            mv.GetNeuronAt(10, 0).Label = "mθ";
            mv.GetNeuronAt(11, 0).Label = "mф";
            foreach (Neuron n in mv.Neurons)
                n.Model = Neuron.modelType.FloatValue;
        }
    }


}
