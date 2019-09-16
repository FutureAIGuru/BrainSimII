//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace BrainSimulator
{
    public class ModuleEntity : ModuleBase
    {
        bool handlingTouch = false;
        bool handlingMemory = false;
        bool handlingCollision = false;
        bool modelChanged = false;
        int delay = 0;
        bool direction = false;

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            Module naBehavior = theNeuronArray.FindAreaByLabel("ModuleBehavior");
            Module naModel = theNeuronArray.FindAreaByLabel("Module2DModel");
            Module2DModel nmModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));
            ModuleBehavior naBehavior1 = (ModuleBehavior)FindModuleByType(typeof(ModuleBehavior));
            if (naBehavior == null) return;

            if (na.GetNeuronAt("Imagine").Fired())
            {
                PointPlus pv = new PointPlus { R = 0, Theta = 0};
                nmModel.ImagineStart(pv, 0);
                Segment phy1 = new Segment
                {
                    P1 = new PointPlus() { P = new Point(-.5, .5), Conf = 0 },
                    P2 = new PointPlus() { P = new Point(.5, .5), Conf = 0 },
                    theColor = Colors.Blue
                };
                nmModel.ImagineObject(phy1);
            }
            if (na.GetNeuronAt("!Imagine").Fired())
                nmModel.ImagineEnd();


            //possible behaviors:
            //back off from collision
            //new object discovered, follow along it  (handled as a reflex by the "Touch" module
            //go to unresolved endpoint 
            //scan for object in field of view but not in model, then go to object 
            //do something random
            //go to home/recharge

            Random rand = new Random();

            modelChanged |= naModel.GetNeuronAt("New").Fired();
            modelChanged |= naModel.GetNeuronAt("Change").Fired();

            return;
            if (na.GetNeuronAt("Collision").Fired() && !handlingCollision) //collision
            {
                na.GetNeuronAt("Collision").SetValue(0);
                naBehavior1.Stop();
                float angle = na.GetNeuronAt("CollisionAngle").CurrentCharge;
                angle += (float)(Math.PI * rand.NextDouble());
                angle = (float)Math.PI;
                if (direction)
                    angle = (float)Math.PI / 2 * ((rand.NextDouble() > .5)?1:-1);
                direction = !direction;
                angle += (float)(rand.NextDouble() * 1.5 - 1.5);
                naBehavior1.TurnTo(angle);
                handlingMemory = false;
                handlingTouch = false;
                handlingCollision = true;
                delay = 20;
            }
            if (delay > 0)
            {
                delay--;
                if (delay < 15 && handlingCollision)
                    naBehavior1.MoveTo(.1f);
                return;
            }


            if (handlingCollision)
            { handlingCollision = false; }

            //touch?
            Module naTouch = theNeuronArray.FindAreaByLabel("Module2DTouch");
            if (naBehavior1.IsIdle() && !handlingTouch && !handlingCollision)
            {
                if (naTouch.GetNeuronAt(0, 0).Fired())
                {
                    naBehavior1.Stop();
                    float angle = naTouch.GetNeuronAt(3, 0).CurrentCharge;
                    float antAngle = naTouch.GetNeuronAt(1, 0).CurrentCharge;
                    float antDist = naTouch.GetNeuronAt(2, 0).CurrentCharge;
                    if (antDist < 0.6 && modelChanged)
                    {
                        angle = (float)Math.PI / 2 - (angle + antAngle);
                        angle = (float)Math.PI / 2 - angle;
                      //  if (direction)
                      //      angle += (float)Math.PI;
                        direction = !direction;
                        naBehavior1.TurnTo(angle);
                        handlingTouch = true;
                        handlingMemory = false;
                        modelChanged = false;
                    }
                }
                else if (naTouch.GetNeuronAt(0, 1).Fired())
                {
                    naBehavior1.Stop();
                    float angle = naTouch.GetNeuronAt(3, 1).CurrentCharge;
                    float antAngle = naTouch.GetNeuronAt(1, 1).CurrentCharge;
                    float antDist = naTouch.GetNeuronAt(2, 1).CurrentCharge;
                    if (antDist < 0.6 && modelChanged)// && modelChanged > 0)
                    {
                        angle = (float)Math.PI / 2 - (angle + antAngle);
                        angle = (float)Math.PI / 2 - angle;
                        if (direction)
                            angle += (float)Math.PI;
                        direction = !direction;
                        naBehavior1.TurnTo(angle);
                        handlingTouch = true;
                        handlingMemory = false;
                        modelChanged = false;
                    }
                }
            }
            if (naBehavior1.IsIdle() && naTouch.GetNeuronAt(8, 0).Fired() && naTouch.GetNeuronAt(8, 1).Fired() && handlingTouch && !handlingCollision)
                handlingTouch = false;

            //////unresolved endpoint
            if (naBehavior1.IsIdle() && !handlingMemory && !handlingTouch && !handlingCollision)
            {
                PointPlus pv = nmModel.FindLowConfidence();
                if (pv != null)
                {
                    naBehavior1.TurnTo((float)pv.Theta);
                    handlingMemory = true;
                    delay = 5;
                }
            }

            ////see something not in model ?
            //if (naBehavior1.IsIdle() && !handlingVision && !handlingTouch && !handlingMemory && !handlingCollision)
            //{
            //    NeuronArea naVision = theNeuronArray.FindAreaByLabel("Module2DVision");
            //    for (int i = 1; i < naVision.Width - 1; i++)
            //    {
            //        int colorVal = naVision.GetNeuronAt(i, 0).CurrentChargeInt;
            //        if (colorVal != 0)
            //        {
            //            double theta = (float)Utils.fieldOfView / (naVision.Width - 1) * i - Utils.fieldOfView / 2;
            //            bool found = nmModel.IsAlreadyInModel((float)theta, Utils.FromArgb(colorVal));
            //            if (!found)
            //            {
            //                naBehavior1.TurnTo((float)theta);
            //                handlingVision = true;
            //                break;
            //            }
            //        }
            //    }
            //}

            ////ramdom
            //if (naBehavior1.IsIdle() && !handlingVision && !handlingTouch && !handlingMemory && !handlingCollision && na.GetNeuronAt("Run").LastCharge == 1)
            //{
            //    double val = rand.NextDouble();
            //    if (val > 0.9)
            //    {
            //        //naBehavior.GetNeuronAt("Random").SetValue(1); //random
            //        double theta = rand.NextDouble();
            //        theta = theta * Math.PI / 2;
            //        theta -= Math.PI / 4;
            //        naBehavior1.TurnTo((float)theta);
            //    }
            //}

            if (naBehavior1.IsIdle() && na.GetNeuronAt("Run").LastCharge == 1)
            {
                naBehavior1.MoveTo(.1f);
                delay = 1;
            }
        }




        public override void Initialize()
        {
            na.GetNeuronAt(0, 0).Label = "Collision";
            na.GetNeuronAt(1, 0).Label = "CollisionAngle";
            na.GetNeuronAt(2, 0).Label = "Idle";
            na.GetNeuronAt(3, 0).Label = "Feed";
            na.GetNeuronAt(4, 0).Label = "Run";
            na.GetNeuronAt(5, 0).Label = "Imagine";
            na.GetNeuronAt(6, 0).Label = "!Imagine";
            handlingTouch = false;
            handlingMemory = false;
            handlingCollision = false;
            delay = 0;
        }
    }
}
