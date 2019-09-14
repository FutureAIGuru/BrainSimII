//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public class ModuleGoToDest : ModuleBase
    {
        List<PointPlus> pointsToTry = new List<PointPlus>();
        PointPlus pvTry = null;
        bool tryAgain = false;
        PointPlus pvTarget = null;
        int countDown = 0;

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable

            if (countDown > 0)
            {
                countDown--;
                return;
            }

            //Input is an object in the model
            //generate some alternate points of view (current position is the first)
            //for each points of view
            ////can dest be seen? 
            ////record distance
            //if shortest distance > 0
            ///1) go to pov 
            ///2) go to dest
            ///else recursive?
            ///
            NeuronArea naBehavior = MainWindow.theNeuronArray.FindAreaByLabel("ModuleBehavior");
            ModuleBehavior nmBehavior = (ModuleBehavior)naBehavior.TheModule;
            NeuronArea naModel = theNeuronArray.FindAreaByLabel("Module2DModel");
            Module2DModel nmModel = (Module2DModel)naModel.TheModule;
        
            if (!nmBehavior.IsIdle()) return;

            if (pvTry != null)
            {
                PointPlus pvTargetSave = new PointPlus { P = pvTarget.P };
                pvTarget.X -= pvTry.X;
                pvTarget.Y -= pvTry.Y;
                pvTarget.Theta -= pvTry.Theta;
                PointPlus pv1 = nmModel.CanISGoStraightTo(pvTarget, out Segment obstacle);
                nmModel.ImagineEnd();
                if (pv1 != null)
                {
                    pvTry.Theta = -pvTry.Theta;
                    naBehavior.GetNeuronAt("TurnTo").SetValue(1);
                    naBehavior.GetNeuronAt("Theta").SetValue((float)pvTry.Theta);
                    naBehavior.GetNeuronAt("MoveTo").SetValue(1);
                    naBehavior.GetNeuronAt("R").SetValue((float)pvTry.R);
                    
                    //we made a partial move...update the target
                    
                    pointsToTry.Clear();
                    tryAgain = true;
                }
                else
                {
                    pvTarget = pvTargetSave;
                }
                pvTry = null;
                return;
            }

            if (pointsToTry.Count > 0 && !nmModel.imagining)
            {
                pvTry = pointsToTry[0];
                pointsToTry.RemoveAt(0);
                nmModel.ImagineStart(pvTry, 0);
                countDown = 5;
                return;
            }


            if (na.GetNeuronAt("Go").CurrentCharge == 0 && !tryAgain) return;
            if (naBehavior.GetNeuronAt("Done").CurrentCharge != 0) return;

            if (!tryAgain)
            {
                pvTarget = new PointPlus
                {
                    R = na.GetNeuronAt("R").CurrentCharge,
                    Theta = na.GetNeuronAt("Theta").CurrentCharge
                };
                na.GetNeuronAt("Go").SetValue(0);
                na.GetNeuronAt("R").SetValue(0);
                na.GetNeuronAt("Theta").SetValue(0);
            }
            if (pvTarget.R == 0)
                pvTarget = nmModel.FindGreen().MidPoint();
            if (pvTarget != null)
            {
                PointPlus pv1 = nmModel.CanISGoStraightTo(pvTarget, out Segment obstacle); 
            
                if (pv1 != null)
                {
                    naBehavior.GetNeuronAt("TurnTo").SetValue(1);
                    naBehavior.GetNeuronAt("Theta").SetValue((float)-pv1.Theta);
                    naBehavior.GetNeuronAt("MoveTo").SetValue(1);
                    naBehavior.GetNeuronAt("R").SetValue((float)pv1.R);
                    tryAgain = false;
                    pvTry = null;
                }
                else
                {
                    PointPlus pvTry1 = Utils.ExtendSegment(obstacle.P1.P, obstacle.P2.P, 0.2f, true);
                    PointPlus pvTry2 = Utils.ExtendSegment(obstacle.P1.P, obstacle.P2.P, 0.2f, false);
                    pointsToTry.Add(pvTry1);
                    pointsToTry.Add(pvTry2);
                    pointsToTry.Add(new PointPlus { X = 1, Y = 1 });
                    pointsToTry.Add(new PointPlus { X = 1, Y = -1 });
                }
            }
        }

        public override void Initialize()
        {
            na.GetNeuronAt(0, 0).Label = "Go";
            na.GetNeuronAt(1, 0).Model = Neuron.modelType.FloatValue;
            na.GetNeuronAt(1, 0).Label = "Theta";
            na.GetNeuronAt(2, 0).Label = "R";
            na.GetNeuronAt(2, 0).Model = Neuron.modelType.FloatValue;
        }

        public override void ShowDialog() //delete this function if it isn't needed
        {
            base.ShowDialog();
        }
    }


}
