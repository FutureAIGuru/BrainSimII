//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Collections.Generic;
using System.Windows;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleMoveObject : ModuleBase
    {
        int state = 0;
        Thing lastPosition = null;
        Thing endTarget = null;
        int motionCount = 0;
        bool goToGoal = false;
        Thing home = null;
        int  doPush = 0;
        bool doFaceSegment = false;
        bool doHome = false;

        public override void Fire()
        {
            Init();  //be sure to leave this here

            if (GetNeuronValue("Auto") == 0) return;
            goToGoal = GetNeuronValue("Goal") == 1;

            ModuleBehavior mBehavior = (ModuleBehavior)FindModuleByType(typeof(ModuleBehavior));
            if (mBehavior == null) return;
            Module2DModel mModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));
            if (mModel is null) return;
            ModuleEvent mEvent = (ModuleEvent)FindModuleByType(typeof(ModuleEvent));
            if (mEvent == null) return;
            ModuleUKSN UKS = (ModuleUKSN)FindModuleByType(typeof(ModuleUKSN));
            if (UKS == null) return;

            if (GetNeuronValue("ModuleBehavior", "Done") == 0) return;

            if (home == null)
            {
                home = UKS.AddThing("Home", "Point");
                home.V = new PointPlus();
                home.AddParent(UKS.Labeled("ModelThing"));
            }

            mModel.GetSegmentsFromUKS();
            List<Thing> segments = mModel.GetUKSSegments();
            if (segments.Count == 0) return;
            Thing t = segments[0]; //TODO: this only works with just a single item in the UKS

            //figure out if any motion occured
            Segment s = Module2DModel.SegmentFromUKSThing(t);
            Module2DModel.OrderSegment(s);
            if (doPush != 0)
            {
                if (doPush == 2)
                    Push(s);
                doPush--;
                return;
            }
            if (doFaceSegment)
            {
                DoFaceSegment(s);
                doFaceSegment = false;
                return;
            }

            Segment s1;
            if (lastPosition == null) //creat objects to keep track of the target and last position
            {
                s1 = s.Clone();
                lastPosition = mModel.AddSegmentToUKS(s1);
                lastPosition.Label = "LastPosition";
                lastPosition.RemoveParent(UKS.Labeled("Segment"));
                lastPosition.AddParent(UKS.Labeled("SSegment"));
                Segment motionTarget = s.Clone();
                motionTarget.P1.X += 0.5f;
                motionTarget.P2.X += 0.5f;
                motionTarget.theColor = (ColorInt)0xff;
                endTarget = mModel.AddSegmentToUKS(motionTarget);
                endTarget.Label = "EndTarget";
                //                endTarget.RemoveParent(UKS.Labeled("Segment"));
                //                endTarget.AddParent(UKS.Labeled("SSegment"));
            }
            else
            {
                s1 = Module2DModel.SegmentFromUKSThing(lastPosition);
            }

            //get motion from subtracting and then updating last position
            Angle rotation = s.Angle - s1.Angle;
            if (rotation > PI / 2) rotation = PI - rotation;
            if (rotation < -PI / 2) rotation = PI + rotation;
            Motion motion = new Motion()
            {
                P = (Point)s.MidPoint.V - s1.MidPoint.V,
                rotation = rotation,
            };
            lastPosition.References[0].T.V = s.P1.Clone();
            lastPosition.References[1].T.V = s.P2.Clone();

            if (Abs(motion.R) > .05 || Abs(motion.rotation) > .05)
            {
                //check for existing Event
                Thing currentEvent = MostLikelyEvent(t);
                if (currentEvent == null)
                {                //add new Event
                    Thing lm1 = mEvent.CreateLandmark(new List<Thing>() { t });
                    Thing t1 = mEvent.CreateEvent(lm1);
                    Thing t3 = UKS.AddThing("m" + motionCount++, new Thing[] { UKS.Labeled("Motion") }, motion);
                    mEvent.AddOutcomePair(t1, UKS.Labeled("Push"), t3);
                }
                return;
            }

            if (doHome)
            {
                DoHome();
                doHome = false;
                return;
            }

            if (goToGoal)
            {
                if (endTarget != null)
                {
                    Segment s2 = Module2DModel.SegmentFromUKSThing(endTarget);
                    GoToGoal(s, s2);
                    return;
                }
            }

            Explore(s);
        }

        private class Seg : Motion
        { }

        void GoToGoal(Segment sCurrent, Segment sTarget)
        {
            Motion m1 = DistanceFromTarget(sCurrent, sTarget);
            if (Abs(m1.rotation) < .05 && Abs(m1.X) < .1 && Abs(m1.Y) < .1)
            {
                endTarget = null;
            }
            else
            {
                Seg pCurrent = new Seg { P = sCurrent.MidPoint.P, rotation = sCurrent.Angle };
                ModuleUKSN UKS = (ModuleUKSN)FindModuleByType(typeof(ModuleUKSN));
                //find the event with the most desireable outcome and then go the the landmark.
                Thing bestEvent = null;
                foreach (Thing tEvent in UKS.Labeled("Event").Children)
                {
                    Motion motion = (Motion)tEvent.Children[0].References[1].T.V;
                    Seg newS = NextPosition(pCurrent, motion);
                    bestEvent = tEvent;
                }
                GoToLandmark(bestEvent.References[0].T,sCurrent);
                doPush = 2;
            }
        }

        void GoToLandmark (Thing landmark,Segment s)
        {
            Segment s1 = Module2DModel.SegmentFromUKSThing(landmark.References[0].T);
            Module2DModel.OrderSegment(s1);
            PointPlus pt1 = s.P1 + s1.P1;
            PointPlus pt2 = s.P2 + s1.P2;
            Utils.FindIntersection(s.P1.P, pt1.P, s.P2.P, pt2.P, out bool lines_intersect, out bool segments_intersect,out Point intersection, out Point close_p1, out Point close_p2, out double collisionAngle);
            PointPlus newPoint = new PointPlus { P = intersection };
            MoveTo(newPoint);
        }

        Motion DistanceFromTarget(Segment s, Segment target)
        {
            Motion retVal = new Motion
            {
                P = (s.MidPoint - target.MidPoint).P,
                rotation = s.Angle - target.Angle
            };
            return retVal;
        }
        Seg NextPosition(Seg s, Motion m)
        {
            Seg retVal = new Seg
            {
                P = (s + m).P,
                rotation = s.rotation + m.rotation,
            };
            return retVal;
        }
        Segment NextPosition(Segment s, Motion m)
        {
            PointPlus oldMidPoint = s.MidPoint;
            PointPlus newMidPoint = oldMidPoint + m;
            PointPlus dP1 = s.P1 - oldMidPoint;
            PointPlus dP2 = s.P2 - oldMidPoint;
            dP1.Theta += m.rotation;
            dP2.Theta += m.rotation;
            Segment retVal = new Segment()
            {
                P1 = newMidPoint + dP1,
                P2 = newMidPoint + dP2,
                theColor = s.theColor,
            };
            return retVal;
        }

        void MoveTo(PointPlus target)
        {
            ModuleBehavior mBehavior = (ModuleBehavior)FindModuleByType(typeof(ModuleBehavior));
            target.X -= 0.21f; //body radius
            mBehavior.TurnTo(-target.Theta);
            mBehavior.MoveTo(target.R);
            mBehavior.TurnTo(target.Theta); //return to previous orientation
        }
        void Push(Segment s)
        {
            ModuleBehavior mBehavior = (ModuleBehavior)FindModuleByType(typeof(ModuleBehavior));
            float dist = (float)Utils.FindDistanceToSegment(s, out Point closest);
            PointPlus closestPP = new PointPlus { P = closest };
            mBehavior.TurnTo(-closestPP.Theta);
            mBehavior.MoveTo(closestPP.R - .1f);
        }
        void FaceMidPoint(Segment s)
        {
            ModuleBehavior mBehavior = (ModuleBehavior)FindModuleByType(typeof(ModuleBehavior));
            mBehavior.TurnTo(-s.MidPoint.Theta);
        }
        void DoFaceSegment(Segment s)
        {
            ModuleBehavior mBehavior = (ModuleBehavior)FindModuleByType(typeof(ModuleBehavior));
            Utils.FindDistanceToSegment(s, out Point closest);
            PointPlus closestPP = new PointPlus { P = closest };
            mBehavior.TurnTo(-closestPP.Theta);
        }
        void DoHome()
        {
            ModuleUKSN UKS = (ModuleUKSN)FindModuleByType(typeof(ModuleUKSN));
            MoveTo((PointPlus)UKS.Labeled("Home").V);
        }

        Thing MostLikelyEvent(Thing currentEvent)
        {
            Thing retVal = null;
            ModuleUKSN UKS = (ModuleUKSN)FindModuleByType(typeof(ModuleUKSN));
            if (UKS == null) return retVal;

            Segment s1 = Module2DModel.SegmentFromUKSThing(currentEvent.References[0].T);

            foreach (Thing t in UKS.Labeled("Event").Children)
            {
                Thing lm = t.References[0].T;
                Thing lms = lm.References[0].T;
                Segment s2 = Module2DModel.SegmentFromUKSThing(lms);

                if (s1.MidPoint.Near(s2.MidPoint, 0.2f))
                    retVal = t;
            }
            return retVal;
        }

        private void Explore(Segment s)
        {
            switch (state)
            {
                case 0: //go to midpoint 
                    {
                        PointPlus target = s.MidPoint;
                        MoveTo(target);
                        doPush = 2;
                        doHome = true;
                        state++;
                        break;
                    }
                case 1: //goto 1st turnpoint
                    {
                        Point p1 = s.MidPoint.P;
                        Point p2 = s.P1.P;
                        PointPlus target = new PointPlus { X = (float)(p1.X + p2.X) / 2, Y = (float)(p1.Y + p2.Y) / 2 };
                        MoveTo(target);
                        doPush = 2;
                        doHome = true;
                        state++;
                        break;
                    }
                case 2: //goto second turnpoint
                    {
                        Point p1 = s.MidPoint.P;
                        Point p2 = s.P2.P;
                        PointPlus target2 = new PointPlus { X = (float)(p1.X + p2.X) / 2, Y = (float)(p1.Y + p2.Y) / 2 };
                        MoveTo(target2);
                        doPush = 2;
                        doHome = true;
                        state++;
                        break;
                    }
                case 3://face midpoint
                    {
                        FaceMidPoint(s);
                        state++;
                        break;
                    }

            }
        }


        public override void Initialize()
        {
            ClearNeurons();

            Neuron n = AddLabel("Auto");
            
            n = AddLabel("Goal");
            
            state = 0;
            lastPosition = null;
            home = null;
        }
    }
}
