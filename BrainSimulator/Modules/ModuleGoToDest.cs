//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Collections.Generic;

namespace BrainSimulator.Modules
{
    public class ModuleGoToDest : ModuleBase
    {

        List<PointPlus> pointsToTry = new List<PointPlus>();
        PointPlus pvTry = null;
        bool tryAgain = false;
        PointPlus pvTarget = null;
        int countDown = 0;

        public ModuleGoToDest()
        {
            minWidth = 3;
        }

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
            ModuleView naBehavior = MainWindow.theNeuronArray.FindModuleByLabel("ModuleBehavior");
            if (naBehavior == null) return;
            ModuleBehavior nmBehavior = (ModuleBehavior)naBehavior.TheModule;
            ModuleView naModel = theNeuronArray.FindModuleByLabel("Module2DModel");
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
                    nmBehavior.TurnTo(pvTry.Theta);
                    nmBehavior.MoveTo(pvTry.R);
                    //SetNeuronValue("ModuleBehavior", "TurnTo", 1);
                    //SetNeuronValue("ModuleBehavior", "Theta", (float)pvTry.Theta);
                    //SetNeuronValue("ModuleBehavior", "MoveTo", 1);
                    //SetNeuronValue("ModuleBehavior", "R", (float)pvTry.R);
                    //SetNeuronValue("ModuleBehavior", "Done", 0);

                    //we made a partial move...update the target
                    pointsToTry.Clear();
                    tryAgain = true;
                    countDown = 5;
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
                countDown = 15;
                return;
            }


            if (GetNeuronValue(null, "Go") == 0 && !tryAgain) return;
            if (GetNeuronValue("ModuleBehavior", "Done") == 0) return;

            if (!tryAgain)
            {
                pvTarget = new PointPlus
                {
                    R = GetNeuronValue(null, "R"),
                    Theta = GetNeuronValue(null, "Theta")
                };
                SetNeuronValue(null, "Go", 0);
                SetNeuronValue(null, "R", 0);
                SetNeuronValue(null, "Theta", 0);
            }
            // if (pvTarget.R == 0)
            //     pvTarget = nmModel.FindGreen().MidPoint();
            if (pvTarget != null)
            {
                PointPlus pv1 = nmModel.CanISGoStraightTo(pvTarget, out Segment obstacle);

                if (pv1 != null)
                {
                    SetNeuronValue("ModuleBehavior", "TurnTo", 1);
                    SetNeuronValue("ModuleBehavior", "Theta", (float)-pv1.Theta);
                    SetNeuronValue("ModuleBehavior", "MoveTo", 1);
                    SetNeuronValue("ModuleBehavior", "R", (float)pv1.R);
                    tryAgain = false;
                    pvTry = null;
                }
                else
                {
                    PointPlus pvTry1 = Utils.ExtendSegment(obstacle.P1.P, obstacle.P2.P, 0.5f, true);
                    PointPlus pvTry2 = Utils.ExtendSegment(obstacle.P1.P, obstacle.P2.P, 0.5f, false);
                    pointsToTry.Add(pvTry1);
                    pointsToTry.Add(pvTry2);
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
    }
}
