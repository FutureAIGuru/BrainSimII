//
// Copyright (c) Charles Simon. All rights reserved.
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
        int doPush = 0;
        bool doFaceSegment = false;
        bool doBackOff = false;

        public override void Fire()
        {
            Init();  //be sure to leave this here

            if (GetNeuronValue("Auto") == 0) return;
            goToGoal = GetNeuronValue("Goal") == 1;

            ModuleBehavior mBehavior = (ModuleBehavior)FindModleu(typeof(ModuleBehavior));
            if (mBehavior == null) return;
            Module2DModel mModel = (Module2DModel)FindModleu(typeof(Module2DModel));
            if (mModel is null) return;
            ModuleEvent mEvent = (ModuleEvent)FindModleu(typeof(ModuleEvent));
            if (mEvent == null) return;
            ModuleUKSN UKS = (ModuleUKSN)FindModleu(typeof(ModuleUKSN));
            if (UKS == null) return;

            if (GetNeuronValue("ModuleBehavior", "Done") == 0) return;


            mModel.GetSegmentsFromUKS();
            IList<Thing> segments = mModel.GetUKSSegments();
            if (segments.Count == 0) return;
            Thing t = segments[0]; //TODO: this only works with just a single item in the UKS

            //figure out if any motion occured
            Segment s = Module2DModel.SegmentFromUKSThing(t);
            Module2DModel.OrderSegment(s);

            if (GetNeuronValue("E0") == 1)
            {
                GoToLandmark(UKS.Labeled("E0").References[0].T, s);
                doPush = 2;
                doBackOff = true;
                return;
            }
            if (GetNeuronValue("E1") == 1)
            {
                GoToLandmark(UKS.Labeled("E1").References[0].T, s);
                doPush = 2;
                doBackOff = true;
                return;
            }
            if (GetNeuronValue("E2") == 1)
            {
                GoToLandmark(UKS.Labeled("E2").References[0].T, s);
                doPush = 2;
                doBackOff = true;
                return;
            }


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

            if (lastPosition == null) //create objects to keep track of the target and last position
            {
                s1 = s.Clone();
                lastPosition = mModel.AddSegmentToUKS(s1);
                lastPosition.Label = "LastPosition";
                lastPosition.RemoveParent(UKS.Labeled("Segment"));
                lastPosition.AddParent(UKS.Labeled("SSegment"));
                Module2DSim mSim = (Module2DSim)FindModleu(typeof(Module2DSim));
                if (mSim is null) return;
                Segment motionTarget = mSim.GetMotionTarget();
                if (motionTarget == null)
                {
                    motionTarget = new Segment();
                    motionTarget.P1 = new PointPlus(4, 1.5f);
                    motionTarget.P2 = new PointPlus(4, -2.5f);
                    motionTarget.theColor = (ColorInt)0xff;
                }
                endTarget = mModel.AddSegmentToUKS(motionTarget);
                endTarget.Label = "EndTarget";
                //endTarget.RemoveParent(UKS.Labeled("Segment"));
                //endTarget.AddParent(UKS.Labeled("SSegment"));
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

            if (Abs(motion.R) > .01 || Abs(motion.rotation) > .05 && !goToGoal && !doBackOff)
            {
                //check for existing Event
                Thing currentEvent = MostLikelyEvent(t);
                if (currentEvent == null)
                {                //add new Event
                    Thing lm1 = mEvent.CreateLandmark(new List<Thing>() { t });
                    Thing t1 = mEvent.CreateEvent(lm1);
                    Thing t3 = UKS.AddThing("m" + motionCount++, UKS.Labeled("Motion"), motion);
                    mEvent.AddOutcomePair(t1, UKS.GetOrAddThing("Push","Action"), t3);
                }
                return;
            }

            if (doBackOff)
            {
                DoBackOff(s);
                doBackOff = false;
                doFaceSegment = true;
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

        //val is the quality of the distance...combination of distance plus rotation...we'll see how it works
        private class Seg : Motion
        {
            public float Val
            {
                get { return X * X + Y * Y + rotation * rotation; }
            }// + .5f* Abs(Theta) + .1f*Abs(rotation); }// +  Abs(rotation / 2); }
            public override string ToString()
            {
                string s = "R: " + R.ToString("F3") + ", Theta: " + Degrees.ToString("F3") + "° (" + X.ToString("F2") + "," + Y.ToString("F2") + ") Rot: " + rotation + " Val: " + Val;
                return s;
            }
        }

        void GoToGoal(Segment sCurrent, Segment sTarget)
        {
            //instead of working with endpoints, we'll just work with midpoint and rotation because the length can't change
            Seg pCurrent = new Seg { P = sCurrent.MidPoint.P, rotation = sCurrent.Angle };
            Seg pTarget = new Seg { P = sTarget.MidPoint.P, rotation = sTarget.Angle };
            Motion neededMotion = DistanceFromTarget(pTarget, pCurrent);
            //there are two exit cases...here we test for absolute closeness, below we test for the best action makes thing further
            if (Abs(neededMotion.rotation) < .05 && Abs(neededMotion.X) < .1 && Abs(neededMotion.Y) < .1)
            {
                endTarget = null;
            }
            else
            {
                ModuleUKSN UKS = (ModuleUKSN)FindModleu(typeof(ModuleUKSN));
                Thing bestEvent = null;
                if (neededMotion.R > .2f)
                {
                    //create a temp distination which is slightly offset
                    ModuleBehavior mBehavior = (ModuleBehavior)FindModleu(typeof(ModuleBehavior));
                    Seg tempTarget = new Seg() { P = sTarget.MidPoint.P + new PointPlus { R = .15f, Theta = sTarget.Angle - PI / 2 }.V, rotation = 0 };
                    neededMotion = DistanceFromTarget(tempTarget, pCurrent);
                    bestEvent = UKS.Labeled("E0");
                    if (neededMotion.Theta < -.05)
                        bestEvent = UKS.Labeled("E2");
                    if (neededMotion.Theta > .05)
                        bestEvent = UKS.Labeled("E1");
                }
                else if (neededMotion.R > .01)
                {
                    if (neededMotion.rotation < -.02)
                        bestEvent = UKS.Labeled("E2");
                    else
                        bestEvent = UKS.Labeled("E1");
                }
                if (bestEvent != null)
                {
                    Motion m = (Motion)bestEvent.Children[0].References[1].T.V;
                    Seg nextPosition = NextPosition(pCurrent, (Motion)bestEvent.Children[0].References[1].T.V);
                    Motion next = DistanceFromTarget(pTarget, nextPosition);
                    //if (next.R < neededMotion.R || Abs(next.rotation) > Utils.Rad(5))
                    {
                        GoToLandmark(bestEvent.References[0].T, sCurrent);
                        doPush = 2;
                        doBackOff = true;
                    }
                    ////                    else
                    //                    {
                    //                        endTarget = null;
                    //                    }
                }
                //find the event with the most desireable outcome and then go the the landmark.
                //Thing bestEvent = null;
                //List<Thing> events = UKS.Labeled("Event").Children;
                //Seg[] distances = new Seg[events.Count];

                ////first time through loop initialize array and take first action
                //for (int i = 0; i < events.Count; i++)
                //{
                //    distances[i] = new Seg() { P = neededMotion.P, rotation = neededMotion.rotation };
                //    Thing tEvent = events[i];
                //    Motion motion = (Motion)tEvent.Children[0].References[1].T.V;
                //    distances[i] = NextPosition(distances[i], motion);
                //}

                ////second time through loop, add to array
                //for (int j = 0; j < 3; j++) //three steps
                //{
                //    for (int i = 0; i < events.Count; i++)
                //    {
                //        for (int k = 0; k < events.Count; k++)
                //        {
                //            Thing tEvent = events[k];
                //            Motion motion = (Motion)tEvent.Children[0].References[1].T.V;

                //            Seg newS = NextPosition(distances[i], motion);
                //            if (newS.Val < distances[i].Val)
                //            { distances[i] = newS; }
                //        }
                //    }
                //}

                //float bestDist = float.MaxValue;
                //for (int i = 0; i < events.Count; i++) //last time through loop, find best
                //{
                //    if (distances[i].Val < bestDist)
                //    {
                //        bestDist = distances[i].Val;
                //        bestEvent = events[i];
                //    }
                //}

                //Motion motion1 = (Motion)bestEvent.Children[0].References[1].T.V;
                //Seg newS1 = NextPosition(pCurrent, motion1);
                //if (newS1.Val < pTarget.Val)
                //{
                //    GoToLandmark(bestEvent.References[0].T, sCurrent);
                //    doPush = 2;
                //    doBackOff = true;
                //}
                //else //our best move makes things worse
                //{
                //    endTarget = null;
                //}
            }
        }

        void GoToLandmark(Thing landmark, Segment s)
        {
            Segment s1 = Module2DModel.SegmentFromUKSThing(landmark.References[0].T);
            Module2DModel.OrderSegment(s1);
            float ratio = s1.P1.R / (s1.Length);
            PointPlus target = new PointPlus
            {
                X = s.P1.X + (s.P2.X - s.P1.X) * ratio,
                Y = s.P1.Y + (s.P2.Y - s.P1.Y) * ratio,
            };

            MoveTo(target, .2f);
        }

        Motion DistanceFromTarget(Seg s, Seg target)
        {
            Motion retVal = new Motion
            {
                P = (s - target).P,
                rotation = s.rotation - target.rotation
            };
            while (retVal.rotation > PI)
                retVal.rotation -= PI;
            while (retVal.rotation < -PI)
                retVal.rotation += PI;
            return retVal;
        }
        Seg NextPosition(Seg s, Motion m)
        {
            Seg retVal = new Seg
            {
                P = (s - m).P,
                rotation = s.rotation - m.rotation,
            };
            while (retVal.rotation > PI)
                retVal.rotation -= PI;
            while (retVal.rotation < -PI)
                retVal.rotation += PI;

            return retVal;
        }

        //not used but may be useful elsewhere.
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

        void MoveTo(PointPlus target, float offset)
        {
            ModuleBehavior mBehavior = (ModuleBehavior)FindModleu(typeof(ModuleBehavior));
            target.X -= offset; //body radius
            mBehavior.TurnTo(-target.Theta);
            mBehavior.MoveTo(target.R);
            mBehavior.TurnTo(target.Theta); //return to previous orientation
        }
        void Push(Segment s)
        {
            ModuleBehavior mBehavior = (ModuleBehavior)FindModleu(typeof(ModuleBehavior));
            float dist = (float)Utils.FindDistanceToSegment(s, out Point closest);
            PointPlus closestPP = new PointPlus { P = closest };
            mBehavior.TurnTo(-closestPP.Theta);
            mBehavior.MoveTo(closestPP.R - .1f);
        }
        void FaceMidPoint(Segment s)
        {
            ModuleBehavior mBehavior = (ModuleBehavior)FindModleu(typeof(ModuleBehavior));
            mBehavior.TurnTo(-s.MidPoint.Theta);
        }
        void DoFaceSegment(Segment s)
        {
            ModuleBehavior mBehavior = (ModuleBehavior)FindModleu(typeof(ModuleBehavior));
            Utils.FindDistanceToSegment(s, out Point closest);
            PointPlus closestPP = new PointPlus { P = closest };
            mBehavior.TurnTo(-closestPP.Theta);
        }
        void DoBackOff(Segment s)
        {
            ModuleBehavior mBehavior = (ModuleBehavior)FindModleu(typeof(ModuleBehavior));
            PointPlus target = new PointPlus(s.MidPoint + new PointPlus { R = 2f, Theta = s.Angle - PI / 2 });
            mBehavior.TurnTo(-target.Theta);
            mBehavior.MoveTo(target.R);
            mBehavior.TurnTo(target.Theta); //return to previous orientation
        }

        Thing MostLikelyEvent(Thing currentEvent)
        {
            Thing retVal = null;
            ModuleUKSN UKS = (ModuleUKSN)FindModleu(typeof(ModuleUKSN));
            if (UKS == null) return retVal;

            Segment s1 = Module2DModel.SegmentFromUKSThing(currentEvent.References[0].T);
            IList<Thing> events = UKS.Labeled("Event").Children;

            foreach (Thing t in events)
            {
                if (t.References.Count > 0)
                {
                    Thing lm = t.References[0].T;
                    if (lm.References.Count > 0)
                    {
                        Thing lms = lm.References[0].T;
                        Segment s2 = Module2DModel.SegmentFromUKSThing(lms);

                        if (s1.MidPoint.Near(s2.MidPoint, 0.2f))
                            retVal = t;
                    }
                }
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
                        MoveTo(target, .2f);
                        doPush = 2;
                        doBackOff = true;
                        state++;
                        break;
                    }
                case 1: //goto 1st turnpoint
                    {
                        Point p1 = s.MidPoint.P;
                        Point p2 = s.P1.P;
                        PointPlus target = new PointPlus { X = (float)(p1.X + p2.X) / 2, Y = (float)(p1.Y + p2.Y) / 2 };
                        MoveTo(target, .2f);
                        doPush = 2;
                        doBackOff = true;
                        state++;
                        break;
                    }
                case 2: //goto second turnpoint
                    {
                        Point p1 = s.MidPoint.P;
                        Point p2 = s.P2.P;
                        PointPlus target2 = new PointPlus { X = (float)(p1.X + p2.X) / 2, Y = (float)(p1.Y + p2.Y) / 2 };
                        MoveTo(target2, .22f);
                        doPush = 2;
                        doBackOff = true;
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
            AddLabel("E0");
            AddLabel("E1");
            AddLabel("E2");

            state = 0;
            lastPosition = null;
            doFaceSegment = false;
            doBackOff = false;
            doPush = 0;
        }
    }
}
