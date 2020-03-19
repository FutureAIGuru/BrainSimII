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
    public class Module2DTouch : ModuleBase
    {
        public override string ShortDescription { get => "Handles 2 Touch sensors"; }
        public override string LongDescription
        {
            get =>
                "This module has 2 rows of neurons representing input from two touch sensors. It receives input from the 2DSim module " +
                "and outputs touch info to the Internal Model. It necessarily handles the positions of the two touch sensors forming " +
                "the beginning of an internal sense of proprioception. " +
                "";
        }
        public Module2DTouch()
        {
            minWidth = 8;
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            Module2DModel naModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));
            if (naModel == null) return;

            for (int i = 0; i < na.Height; i++)
            {
                //neurons:  0:touch   1:antAngle  2:antDistance 3: sensedLineAngle 4: conf1 5: len1 6: conf2 7: len2 8: touch-ended 9: modelchanged
                if (na.GetNeuronAt(0, i).CurrentCharge == 0) continue;
                float antAngle = na.GetNeuronAt(1, i).CurrentCharge;
                float antDist = na.GetNeuronAt(2, i).CurrentCharge;
                float lineAngle = na.GetNeuronAt(3, i).CurrentCharge;
                float p1IsEndpt = na.GetNeuronAt(4, i).CurrentCharge;
                float l1 = na.GetNeuronAt(5, i).CurrentCharge;
                float p2IsEndpt = na.GetNeuronAt(6, i).CurrentCharge;
                float l2 = na.GetNeuronAt(7, i).CurrentCharge;

                //create the line segment (all coordinates relative to self)
                PointPlus antennaPos = new PointPlus()
                { R = antDist, Theta = antAngle };

                float lineAngleAbs = antAngle - lineAngle;

                PointPlus pv1 = new PointPlus() { R = l1, Theta = (float)Math.PI + lineAngleAbs };
                PointPlus pv2 = new PointPlus() { R = l2, Theta = lineAngleAbs };

                Point P1 = antennaPos.P + pv1.V;
                Point P2 = antennaPos.P + pv2.V;
                PointPlus P1P = new PointPlus() { P = P1, Conf = 1-p1IsEndpt };
                PointPlus P2P = new PointPlus() { P = P2, Conf = 1-p2IsEndpt };

               // bool modelChanged = naModel.AddSegmentFromTouch(P1P, P2P);

            }
        }
        public override void Initialize()
        {
            na.GetNeuronAt(0, 0).Label = "Right";
            na.GetNeuronAt(0, 1).Label = "Left";
        }
    }


}
