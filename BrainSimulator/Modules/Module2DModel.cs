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
using System.Xml.Serialization;
using System.Diagnostics;

namespace BrainSimulator.Modules
{
    public class Module2DModel : ModuleBase
    {
        private List<Thing> KBSegments;
        private List<Thing> KBPossiblePoints;

        //these are public to let the dialog box use the info
        public List<Thing> GetKBSegments() { return KBSegments; }
        public List<Thing> GetKBPossiblePoints() { return KBPossiblePoints; }


        public override string ShortDescription { get => "Maintains an internal representation of surroung things"; }
        public override string LongDescription
        {
            get =>
                "This module receives input from the Touch and Vision modules and merges the information to maintain a representation of " +
                "physical objects in the entity's environment. It also supports imaingation via the temporary addition of imagined objects " + "" +
                "and the temporary change in point of view.\r\n" +
                "\r\n" +
                "";
        }

        public Module2DModel()
        {
            //            GetSegmentsFromKB();
            KBSegments = new List<Thing>();
            KBPossiblePoints = new List<Thing>();
        }

        public void GetSegmentsFromKB()
        {
            ModuleView naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
            if (naKB == null) return;
            if (naKB.TheModule is Module2DKB kb)
            {
                KBSegments = kb.HavingParent(kb.Labeled("Segment"));
                KBPossiblePoints = kb.HavingParent(kb.Labeled("TempP"));
            }
        }
        public Segment SegmentFromKBThing(Thing t)
        {
            if (t == null) return null;
            if (t.References.Count == 0 && t.ReferencedBy.Count == 1)
            {
                t = t.ReferencedBy[0].T;
            }
            Segment s = new Segment();
            foreach (Link l in t.References)
            {
                Thing t1 = l.T;
                if (s.P1 == null && t1.V is PointPlus p1)
                    s.P1 = p1;
                else if (t1.V is PointPlus p2)
                    s.P2 = p2;
                else if (t1.V is int c)
                    s.theColor = Utils.FromArgb(c);
            }
            return s;
        }

        //this is legacy to convert a Thing (new) to a Segment (old) 
        //eventually, we should be able to get rid of segments.
        public void SegmentToKBThing(Segment s, Thing t)
        {
            bool p1Handled = false;
            foreach (Link l in t.References)
            {
                Thing t1 = l.T;
                if (!p1Handled && t1.V is PointPlus)
                {
                    t1.V = s.P1;
                    p1Handled = true;
                    continue;
                }
                if (t1.V is PointPlus)
                    t1.V = s.P2;
                if (t1.V is Color c)
                    t1.V = s.theColor;
            }
        }

        int pCount = 0;
        int sCount = 0;
        int cCount = 0;
        int ppCount = 0;
        private void AddSegmentToKB(PointPlus P1, PointPlus P2, int theColor)
        {
            ModuleView naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
            if (naKB == null) return;
            if (naKB.TheModule is Module2DKB kb)
            {
                Thing p1 = kb.AddThing("p" + pCount++, new Thing[] { kb.Labeled("Point") }, P1);
                Thing p2 = kb.AddThing("p" + pCount++, new Thing[] { kb.Labeled("Point") }, P2);
                Thing color = kb.Valued(theColor);
                if (color == null)
                    color = kb.AddThing("c" + cCount++, new Thing[] { kb.Labeled("Color") }, theColor);
                kb.AddThing("s" + sCount++, new Thing[] { kb.Labeled("Segment") }, null, new Thing[] { p1, p2, color });
            }
        }

        //get input from vision...less accurate
        public void AddPosiblePointToKB(PointPlus P, int leftColor, int rightColor, float angularResolution, float minDepth, float maxDepth)
        {
            //TODO implement occlusion detection
            if (leftColor != 0 && rightColor != 0) return;
            GetSegmentsFromKB();
            if (KBPossiblePoints == null) return; //this is a startup issue 
            ModuleView naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
            if (naKB?.TheModule is Module2DKBN kb)
            {
                var allPoints = kb.GetChildren(kb.Labeled("Point")).Union(KBPossiblePoints);
                foreach (Thing t in allPoints)
                {
                    if (t.Parents[0].Label == "TempP")
                    {
                        if (
                            (int)t.References[0].T.V != leftColor &&
                            (int)t.References[0].T.V != rightColor &&
                            (int)t.References[1].T.V != leftColor &&
                            (int)t.References[1].T.V != rightColor
                            ) continue;
                    }
                    else if (t.Label != "TempP")
                    {
                        //segment?
                        Thing seg = t.ReferencedBy[0].T;
                        Thing colort = seg.References[2].T;
                        if ((int)colort.V != leftColor && (int)colort.V != rightColor)
                            continue;
                    }
                    else
                        continue;
                    if (t.V is PointPlus pp)
                    {
                        float allowedAngularError = 20 / P.R;
                        if (allowedAngularError < 8) allowedAngularError = 8;
                        if (Math.Abs(P.Theta - pp.Theta) < allowedAngularError * angularResolution) //tighten to this depends on distance
                        {
                            if (pp.R < maxDepth + .5 && pp.R > minDepth - .5)
                            //if (pp.R < maxDepth && pp.R > minDepth )
                            {
                                //point is already in KB
                                if (P.R < pp.Conf)
                                {
                                    //the nwe value is more acurate, update the KB with the current vision
                                    P.Conf = P.R;
                                    t.V = P;
                                }
                                return;
                            }
                            else  //there are many issues which could cause a miss (occlusion, etc. ) so we default to ignoring the point
                                return;
                        }
                    }
                }

                //The point appears to be not previously seen...take care of adding it
                
                //will there be two points with the same color which might be merged into a possible segment?
                bool segmentAdded = false;
                foreach (Thing t1 in KBPossiblePoints)
                {
                    if (t1.V is PointPlus pp && t1.References.Count > 1)
                    {
                        //this joins points which have the same color
                        int refLeftColor = (int)t1.References[0].T.V;
                        int refRightColor = (int)t1.References[1].T.V;
                        float angularWidth = P.Theta - pp.Theta;
                        if (leftColor == refRightColor && leftColor != 0 && angularWidth < 0)
                        {
                            AddSegmentToKB(P, pp, leftColor);
                            kb.DeleteThing(t1);
                            segmentAdded = true;
                            break;
                        }
                        if (rightColor == refLeftColor && rightColor != 0 && angularWidth > 0)
                        {
                            AddSegmentToKB(P, pp, refLeftColor);
                            kb.DeleteThing(t1);
                            segmentAdded = true;
                            break;
                        }

                    }
                }

                //This point is not part of a segment...add it as a temporary point
                if (!segmentAdded)
                {
                    Thing newThing = kb.AddThing("pp" + ppCount++, new Thing[] { kb.Labeled("TempP") }, P);
                    if (newThing != null)
                    {
                        //add references to colors...ref 0 left, ref 1 right
                        Thing colorL = kb.Valued(leftColor);
                        if (colorL == null)
                            colorL = kb.AddThing("c" + cCount++, new Thing[] { kb.Labeled("Color") }, leftColor);
                        newThing.AddReference(colorL);
                        Thing colorR = kb.Valued(rightColor);
                        if (colorR == null)
                            colorR = kb.AddThing("c" + cCount++, new Thing[] { kb.Labeled("Color") }, rightColor);
                        newThing.AddReference(colorR);
                    }
                }
            }
            UpdateDialog();
        }

        //get input from touch... accurate locations
        //this contains a lot of commented-out code which used to merge information
        //it will need to be re-implemented to handle upgrades 
        public bool AddSegment(PointPlus P1, PointPlus P2, Color theColor)
        {
            if (KBSegments is null) return false;
            if (imagining) return false;
            bool found = false;
            bool modelChanged = false;
            if (P1.Conf == 0 && P2.Conf == 0) return false;
            ModuleView naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
            if (naKB == null) return false;
            if (naKB.TheModule is Module2DKB kb)
            {
                Thing t1 = kb.Valued(P1, kb.Labeled("Point").Children, .5f);
                if (P1.Conf == 0 && t1 != null && ((PointPlus)t1.V).Conf != 0)
                { t1.V = P1; found = true; }
                Thing t2 = kb.Valued(P2, kb.Labeled("Point").Children, .5f);
                if (P2.Conf == 0 && t2 != null && ((PointPlus)t2.V).Conf != 0)
                { t2.V = P2; found = true; }
                if (found)
                {
                    if (theNeuronArray.FindAreaByLabel("ModuleBehavior") is ModuleView naBehavior)
                    { naBehavior.GetNeuronAt("EndPt").SetValue(1); }
                }
                Segment s = SegmentFromKBThing(t1);
                if (s == null)
                    s = SegmentFromKBThing(t2);
                //if (s.P1.Conf == 1 && s.P2.Conf == 0)
                //{
                //    //use the slope of the input points and correct the slope of the stored segment
                //    PointPlus PPInput = new PointPlus { P = (Point)(P2.P - P1.P) };
                //    PointPlus PPStored = new PointPlus { P = (Point)(s.P2.P - s.P1.P) };
                //    PPStored.Theta = PPInput.Theta;
                //    s.P2.P = (Point)(s.P1.P + (Vector)PPStored.P);
                //}
                //if (s.P1.Conf == 0 && s.P2.Conf == 1)
                //{
                //    //use the slope of the input points and correct the slope of the stored segment
                //    PointPlus PPInput = new PointPlus { P = (Point)(P1.P - P2.P) };
                //    PointPlus PPStored = new PointPlus { P = (Point)(P1.P - P2.P) };
                //    PPStored.R = PPInput.R;
                //    s.P1.P = (Point)(s.P2.P + (Vector)PPStored.P);
                //}
            }
            return false;
            //is object already here? (but not seen?)...adjust it
            foreach (Thing t in KBSegments)
            {
                Segment segment = SegmentFromKBThing(t);

                //if one of the points is not on the line...this is not the stored object we're looking for
                float d1 = Utils.DistancePointToLine(P1.P, segment.P1.P, segment.P2.P);
                float d2 = Utils.DistancePointToLine(P2.P, segment.P1.P, segment.P2.P);
                if (d1 > 0.2 || d2 > 0.2) continue;
                Point closest;

                //if both point are within the segment, this is already in the model
                d1 = (float)Utils.FindDistanceToSegment(P1.P, segment.P1.P, segment.P2.P, out closest);
                d2 = (float)Utils.FindDistanceToSegment(P2.P, segment.P1.P, segment.P2.P, out closest);

                if (P1.Conf != 1 && P2.Conf != 1)
                    if (d1 < 0.1 && d2 < 0.1) return modelChanged; //segment already in

                //does the proposed touched overlap with the model segment?
                //if the slopes are different, they must be different

                found = true;

                //merge the segments  
                //an existing known  endpoint cannot be changed
                //double m1 = Math.Abs(Math.Atan2((P1.P - P2.P).Y, (P1.P - P2.P).X));
                //    Vector delta = Utils.ToCartesian(P1.P) - Utils.ToCartesian(P2.P);
                //    double m1 = Math.Abs(Math.Atan2(delta.Y, delta.X));
                ////    if (Math.Abs(m1) > Math.PI / 4)
                //    { //primarily vertical line...sort by y}

                //order endpoints by theta
                if (P1.Theta > P2.Theta)
                {
                    PointPlus pTemp = P1;
                    P1 = P2;
                    P2 = pTemp;
                }
                if (segment.P1.Theta > segment.P2.Theta)
                {
                    PointPlus pTemp = segment.P1;
                    segment.P1 = segment.P2;
                    segment.P2 = pTemp;
                }
                //extend lower?
                if (segment.P1.Conf == 0 && (P1.Conf == 1 || segment.P1.Theta > P1.Theta))
                {
                    segment.P1 = P1;
                    na.GetNeuronAt("Change").SetValue(1);
                    modelChanged = true;
                }
                //extend upper?
                if (segment.P2.Conf == 0 && (P2.Conf == 1 || segment.P2.Theta < P2.Theta))
                {
                    segment.P2 = P2;
                    na.GetNeuronAt("Change").SetValue(1);
                    modelChanged = true;
                }
                //This is needed because (above) we just changed the pointers, not the value
                SegmentToKBThing(segment, t);
            }
            //    }
            //    else
            //    {//primarily horizontal line...sort by x
            //        if (P1.P.X > P2.P.X)
            //        {
            //            PointPlus pTemp = P1;
            //            P1 = P2;
            //            P2 = pTemp;
            //        }
            //        if (objects[i].P1.P.X > objects[i].P2.P.X)
            //        {
            //            PointPlus pTemp = objects[i].P1;
            //            objects[i].P1 = objects[i].P2;
            //            objects[i].P2 = pTemp;
            //        }
            //        if (objects[i].P1.conf == 0 && P1.P.X < objects[i].P1.P.X)
            //        {
            //            objects[i].P1 = P1;
            //            na.GetNeuronAt("Change").SetValue(1);
            //            modelChanged = true;
            //        }
            //        if (objects[i].P2.conf == 0 && P2.P.X > objects[i].P2.P.X)
            //        {
            //            objects[i].P2 = P2;
            //            na.GetNeuronAt("Change").SetValue(1);
            //            modelChanged = true;
            //        }
            //    }
            //}
            if (!found)
            {
                //not already here?  Add it
                Segment newObject = new Segment()
                {
                    P1 = P1,
                    P2 = P2,
                    theColor = theColor
                };

                //objects.Add(newObject);
                // AddSegmentToKB(P1, P2, theColor);
                na.GetNeuronAt("New").SetValue(1);
                modelChanged = true;
            }
            UpdateDialog();
            return modelChanged;
        }

        //returns a point with low confidence which an entity might use to explore and improve the confidence in the point
        public PointPlus FindLowConfidence()
        {
            PointPlus pv = null;
            float nearest = float.MaxValue;
            foreach (Thing t in KBSegments)
            {
                Segment s = SegmentFromKBThing(t);
                if (s.P1.Conf == 0)
                {
                    if (s.P1.R < nearest)
                    {
                        if (pv == null) pv = new PointPlus();
                        pv.P = s.P1.P;
                        nearest = s.P1.R;
                    }
                }
                if (s.P2.Conf == 0)
                {
                    if (s.P2.R < nearest)
                    {
                        if (pv == null) pv = new PointPlus();
                        pv.P = s.P2.P;
                        nearest = s.P2.R;
                    }
                }
            }
            return pv;
        }

        //not presently used
        public bool IsAlreadyInModel(float theta, Color theColor)
        {
            int nearest = -1;
            float dist = float.MaxValue;
            Segment s = null;
            for (int i = 0; i < KBSegments.Count; i++)
            {
                s = SegmentFromKBThing(KBSegments[i]);

                //does this object cross the given visual angle?
                PointPlus pv = new PointPlus { R = 10, Theta = theta };
                Utils.FindIntersection(new Point(0, 0), pv.P, s.P1.P, s.P2.P,
                    out bool lines_intersect, out bool segments_intersect, out Point intersection, out Point clos_p1, out Point close_p2, out double collisionAngle);
                if (!segments_intersect) continue;

                //and is it the nearest?
                Vector v = (Vector)intersection;
                if (v.Length < dist)
                {
                    nearest = i;
                    dist = (float)v.Length;
                }
            }
            if (nearest != -1 && s.theColor == theColor)
            {
                return true;
            }
            return false;
        }

        //not presently used
        public bool SetColor(float theta, Color theColor)
        {
            int nearest = -1;
            float dist = float.MaxValue;
            Segment s = null;
            for (int i = 0; i < KBSegments.Count; i++)
            {
                s = SegmentFromKBThing(KBSegments[i]);
                //has color already been assigned?
                if (s.theColor != Colors.Wheat) continue;

                //does this object cross the given visual angle?
                PointPlus pv = new PointPlus() { R = 10, Theta = theta };
                Utils.FindIntersection(new Point(0, 0), pv.P, s.P1.P, s.P2.P,
                    out bool lines_intersect, out bool segments_intersect, out Point intersection, out Point clos_p1, out Point close_p2, out double collisionAngle);
                if (!segments_intersect) continue;

                //and is it the nearest?
                Vector v = (Vector)intersection;
                if (v.Length < dist)
                {
                    nearest = i;
                    dist = (float)v.Length;
                }
            }
            if (nearest != -1)
            {
                s.theColor = theColor;
                SegmentToKBThing(s, KBSegments[nearest]);
                na.GetNeuronAt("Color").SetValue(1);
                return true;
            }
            return false;
        }

        //TODO: see if this is useful
        public Segment FindGreen()
        {
            foreach (Thing t in KBSegments)
            {
                Segment s = SegmentFromKBThing(t);
                if (s.theColor == Colors.Green)
                    return s;
            }
            return null;
        }

        //find the nearest Setment to the entity
        public Segment GetNearestObject()
        {
            double closestDistance = 100;
            Segment foundObject = null;
            Segment s = null;
            for (int i = 0; i < KBSegments.Count; i++)
            {
                s = SegmentFromKBThing(KBSegments[i]);
                Utils.FindDistanceToSegment(new Point(0, 0), s.P1.P, s.P1.P, out Point closest);
                if (((Vector)closest).Length < closestDistance)
                {
                    foundObject = s;
                    closestDistance = ((Vector)closest).Length;
                }
            }
            return foundObject;
        }

        //maintain a list of objects in the current visual field
        public void MarkVisibleObjects()
        {
            ModuleView naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
            if (naKB == null) return;
            Module2DKB kb = (Module2DKB)naKB.TheModule;
            Thing tVisible = kb.Labeled("Visible");

            //clear all visiblility references
            for (int i = 0; i < KBSegments.Count; i++)
                KBSegments[i].RemoveReference(tVisible);

            ModuleView naVision = theNeuronArray.FindAreaByLabel("Module2DVision");
            int possibleViewAngles = naVision.Width;
            float deltaTheta = Module2DVision.fieldOfView / possibleViewAngles;
            for (int i = 0; i < possibleViewAngles; i++)
            {
                float theta = (float)-Module2DVision.fieldOfView / 2 + (i * deltaTheta);
                PointPlus P = new PointPlus { R = 10, Theta = theta };
                foreach (Thing t in KBSegments)
                {
                    Segment s = SegmentFromKBThing(t);
                    Utils.FindIntersection(new Point(0, 0), P.P, s.P1.P, s.P2.P,
                        out bool lines_intersect, out bool segments_intersect,
                        out Point intersection, out Point close_p1, out Point closep2, out double collisionAngle);
                    if (segments_intersect)
                    {
                        t.AddReference(tVisible);
                    }
                }
            }
        }

        //this is asking can the entity directly see a target object or are there obstacles
        public PointPlus CanISGoStraightTo(PointPlus midPoint, out Segment obstacle)
        {
            obstacle = null;
            float closestDistance = 100;
            ModuleView naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
            if (naKB == null) return null;
            Module2DKB kb = (Module2DKB)naKB.TheModule;
            bool ok = true;
            foreach (Thing t in KBSegments)
            {
                Segment s = SegmentFromKBThing(t);
                Utils.FindIntersection(new Point(0, 0), midPoint.P, s.P1.P, s.P2.P,
                    out bool lines_intersect, out bool segments_intersect,
                    out Point intersection, out Point close_p1, out Point closep2, out double collisionAngle);
                if (segments_intersect)
                {
                    ok = false;
                    Utils.FindDistanceToSegment(new Point(0, 0), s.P1.P, s.P1.P, out Point closest);
                    if (((Vector)closest).Length < closestDistance)
                    {
                        closestDistance = (float)((Vector)closest).Length;
                        obstacle = s;
                    }
                }
            }
            if (ok) return midPoint;
            return null;
        }

        /*********************************
         * Imagination functionality
        ******************************** */
        public bool imagining = false;
        PointPlus imaginationOffset;
        float imaginationDirection;
        //this will all need to be converted to run with KB Things instead of segments
        public List<Segment> imagination = new List<Segment>();

        public void ImagineObject(Segment obj)
        {
            if (!imagining) return;
            imagination.Add(obj);
            UpdateDialog();
        }
        public void ClearImagination()
        {
            imagination.Clear();
        }
        public void ImagineStart(PointPlus offset, float direction)
        {
            imagining = true;
            if (imaginationOffset != null) ImagineEnd();
            imagining = true;
            imaginationOffset = offset;
            imaginationDirection = direction;
            Rotate((float)-offset.Theta);
            Move((float)offset.R);
            Rotate((float)(direction - offset.Theta));
            UpdateDialog();
        }
        public void ImagineEnd()
        {
            if (!imagining) return;
            ClearImagination();
            Rotate((float)(-imaginationDirection + imaginationOffset.Theta));
            Move((float)-imaginationOffset.R);
            Rotate((float)imaginationOffset.Theta);
            imaginationDirection = 0;
            imaginationOffset = null;
            imagining = false;
            UpdateDialog();
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            if (KBPossiblePoints == null)
                GetSegmentsFromKB();
        }

        public override void Initialize()
        {
            pCount = 0;
            sCount = 0;
            cCount = 0;
            ppCount = 0;

            GetSegmentsFromKB();

            Module2DKBN kbn = (Module2DKBN)FindModuleByType(typeof(Module2DKBN));
            while (KBSegments.Count > 0)
            {
                kbn.DeleteThing(KBSegments[0]);
            }

            while (KBPossiblePoints.Count > 0)
            {
                kbn.DeleteThing(KBPossiblePoints[0]);
            }
            List<Thing> points = kbn.GetChildren(kbn.Labeled("Point"));
            while (points.Count > 1)
            {
                if (points[0].Label != "TempP")
                    kbn.DeleteThing(points[0]);
                else
                    kbn.DeleteThing(points[1]);
            }

            na.GetNeuronAt(0, 0).Label = "New";
            na.GetNeuronAt(1, 0).Label = "Change";

            UpdateDialog();
        }

        public void Rotate(float theta)
        {
            //move all the objects in the model
            foreach (Thing t in KBPossiblePoints)
            {
                if (t.V != null && t.V is PointPlus P)
                {
                    P.Theta += theta;
                }
            }
            //rotate all the objects in the model
            foreach (Thing t in KBSegments)
            {
                Segment s = SegmentFromKBThing(t);
                s.P1.Theta += theta;
                s.P2.Theta += theta;
            }
            UpdateDialog();
        }

        public void Move(float motion)
        {
            //move all the objects in the model
            foreach (Thing t in KBPossiblePoints)
            {
                if (t.V != null && t.V is PointPlus P)
                {
                    P.X -= motion;
                }
            }
            //move all the objects in the model
            foreach (Thing t in KBSegments)
            {
                Segment s = SegmentFromKBThing(t);
                s.P1.X -= motion;
                s.P2.X -= motion;
            }
            UpdateDialog();
        }
    }
}



