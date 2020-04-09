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
using static System.Math;
using static BrainSimulator.Utils;



namespace BrainSimulator.Modules
{

    public class Module2DModel : ModuleBase
    {
        private List<Thing> KBSegments;
        private List<Thing> KBPoints;

        //these are public to let the dialog box use the info
        public List<Thing> GetKBSegments() { return KBSegments; }
        public List<Thing> GetKBPoints() { return KBPoints; }

        //these are used to build labels for things
        //TODO make public and reset on initialize
        int pCount = 0;
        int sCount = 0;
        int cCount = 0;
        int ppCount = 0;


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
            KBSegments = new List<Thing>();
            KBPoints = new List<Thing>();
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            if (KBPoints == null || KBPoints.Count == 0)
                GetSegmentsFromKB();

            ModuleBehavior nmBehavior = (ModuleBehavior)FindModuleByType(typeof(ModuleBehavior));
            Thing obstacle = NearestThingAhead();
            if (obstacle != null)
            {
                Segment s = SegmentFromKBThing(obstacle);
                Utils.FindDistanceToSegment(new Point(0, 0), s.P1.P, s.P2.P, out Point closest);
                double dist = ((Vector)closest).Length;

                if (dist < 0.6f)
                {
                    //we are approaching an obsctacle.
                    //TODO: Fire a neuron
                    SetNeuronValue(null, "Obstacle", 1);
                }
            }
        }

        public void GetSegmentsFromKB()
        {
            ModuleView naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
            if (naKB == null) return;
            if (naKB.TheModule is Module2DKB kb)
            {
                KBSegments = kb.HavingParent(kb.Labeled("Segment"));
                KBPoints = kb.HavingParent(kb.Labeled("Point"));
            }
        }

        public static Segment SegmentFromKBThing(Thing t)
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
                    s.theColor = c;
            }
            return s;
        }

        private Thing MostLikelySegment(Segment newSegment)
        {
            Thing retVal = null;
            if (KBSegments == null) return null;
            foreach (Thing t in KBSegments)
            {
                Segment s = SegmentFromKBThing(t);
                if (s.theColor == newSegment.theColor)
                {
                    //TODO is the segment visible?

                    //if the segments visually overlap
                    float thx1 = newSegment.P1.Theta;
                    float thx2 = newSegment.P2.Theta;
                    float thy1 = s.P1.Theta;
                    float thy2 = s.P2.Theta;
                    if (thx1 > thx2)
                    { float temp = thx1; thx1 = thx2; thx2 = temp; }
                    if (thy1 > thy2)
                    { float temp = thy1; thy1 = thy2; thy2 = temp; }

                    if (thx1 < thy2 && thx2 > thy1)
                    {
                        //yes, the segments overlap
                        retVal = t;
                        break;
                    }
                }
            }
            return retVal;
        }

        public void OrderSegment(object x)
        {
            if (x is Thing t)
            {
                if (((PointPlus)t.References[0].T.V).Theta > ((PointPlus)t.References[1].T.V).Theta)
                {
                    Link temp = t.References[0]; t.References[0] = t.References[1]; t.References[1] = temp;
                }
            }
            if (x is Segment s)
            {
                if (s.P1.Theta > s.P2.Theta)
                {
                    PointPlus temp = s.P1; s.P1 = s.P2; s.P2 = temp;
                }
            }
        }

        private Thing MostLikelyPoint(PointPlus p1, ColorInt theColor)
        {
            Thing retVal = null;
            if (KBPoints == null) return null;
            foreach (Thing t in KBPoints)
            {
                Segment s = SegmentFromKBThing(t.ReferencedBy[0].T);
                if (s.theColor == theColor)
                {
                    if (t.V is PointPlus p)
                    {
                        if (Abs(p.Theta - p1.Theta) < Rad(5))  //are angles within 5 degrees
                        {
                            if (p1.Conf < p.Conf)
                            {
                                return t;
                            }
                        }
                    }
                }
            }
            return retVal;
        }

        public void UpdateEndpointFromVision(PointPlus P1, ColorInt theColor) //we might add color
        {
            Thing match = MostLikelyPoint(P1, theColor);
            if (match != null)
            {
                match.V = P1;
            }
        }

        public void AddSegmentFromVision(PointPlus P1, PointPlus P2, int theColor)
        {
            //determine if the segment is already in the UKS.  
            //Correct it if it is there, add it if it is not.
            //FUTURE: detect motion
            if (theColor == 0) return;
            Segment newSegment = new Segment() { P1 = P1, P2 = P2, theColor = theColor };
            Module2DKBN kb = (Module2DKBN)FindModuleByType(typeof(Module2DKBN));
            GetSegmentsFromKB();
            if (kb != null)
            {
                //it's easier if we sort by theta
                OrderSegment(newSegment);
                Thing match = MostLikelySegment(newSegment);
                if (match != null)
                {
                    kb.Fire(match);
                    OrderSegment(match);
                    Segment s = SegmentFromKBThing(match);
                    float newVisualWidth = newSegment.VisualWidth();
                    float matchVisualWidth = s.VisualWidth();
                    //if the newVisualWidth is bigger, an adjustment is needed
                    //this happens if the initial view was occluded but now it is less
                    //if (newVisualWidth > matchVisualWidth)
                    {
                        if (newSegment.P1.Conf < s.P1.Conf)
                        {
                            s.P1.Conf = newSegment.P1.Conf;
                            s.P1.R = newSegment.P1.R;
                            s.P1.Theta = newSegment.P1.Theta;
                        }
                        if (newSegment.P2.Conf < s.P2.Conf)
                        {
                            s.P2.Conf = newSegment.P2.Conf;
                            s.P2.R = newSegment.P2.R;
                            s.P2.Theta = newSegment.P2.Theta;
                        }
                        //if (s.P1.R < newSegment.P1.R && s.P1.Conf > newSegment.P1.Conf)
                        //    s.P1.R = newSegment.P1.R;
                        //if (s.P2.R < newSegment.P2.R && s.P2.Conf > newSegment.P2.Conf)
                        //    s.P2.R = newSegment.P2.R;
                    }

                    ////if the input point is significantly further it may be occlusion related
                    //if (newSegment.P1.R > s.P1.R + s.P1.Conf && s.P1.Conf > newSegment.P1.Conf)
                    //{
                    //    s.P1.R = newSegment.P1.R;
                    //    s.P1.Theta = newSegment.P1.Theta;
                    //}
                    //if (newSegment.P2.R > s.P2.R + s.P2.Conf && s.P2.Conf > newSegment.P2.Conf)
                    //{
                    //    s.P2.R = newSegment.P2.R;
                    //    s.P2.Theta = newSegment.P2.Theta;
                    //}
                    ////if the newVisualWidth is smaller, an occlusion 

                }
                else
                {
                    //P1.Conf = P1.R;
                    //P2.Conf = P2.R;
                    Thing newThing = AddSegmentToKB(P1, P2, theColor);
                    kb.Fire(newThing);
                    UpdateDialog();
                }
            }
        }


        private float SegmentLength(Thing t)
        {
            if (t == null || t.Parents[0].Label != "Segment") return -1;
            Segment s = SegmentFromKBThing(t);
            return s.Length();
        }

        //get input from vision...less accurate
        public Thing AddPointFromVision(PointPlus P)
        {
            float angularResolution = (float)PI / 90; //2-degrees
            float depthResolution = 0.5f;
            GetSegmentsFromKB();
            if (KBPoints == null) return null; //this is a startup issue 
            P.Conf = P.R;
            Thing newThing = null;
            ModuleView naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
            if (naKB?.TheModule is Module2DKBN kb)
            {
                //find the nearest point at this angle...if it's near in depth, use it, otherwise create a new point
                Thing t1 = NearestPoint(P.Theta, 5 * angularResolution);
                if (t1 != null && t1.V is PointPlus pp)
                {
                    //if the new point is less than .5 closer, it is likely the same point
                    if (pp.R - P.R < depthResolution)
                    {
                        //point already in model...update it if this is a more accurate entry
                        if (pp.Conf < P.Conf)
                            pp = P;
                        return t1;
                    }
                }
                //point not in model...add it
                P.Conf = P.R;
                newThing = kb.AddThing("p" + ppCount++, new Thing[] { kb.Labeled("Point") }, P);
            }
            UpdateDialog();
            return newThing;
        }

        private Thing newPoint(PointPlus p)
        {
            ModuleView naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
            if (naKB == null) return null;
            if (naKB.TheModule is Module2DKB kb)
            {
                Thing t1 = kb.AddThing("p" + pCount++, new Thing[] { kb.Labeled("Point") }, p);
                return t1;
            }
            return null;
        }

        public Thing AddSegmentToKB(object P1, object P2, int theColor)
        {
            ModuleView naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
            if (naKB == null) return null;
            if (naKB.TheModule is Module2DKB kb)
            {
                Thing t1, t2;
                if (P1 is PointPlus p1)
                    t1 = kb.AddThing("p" + pCount++, new Thing[] { kb.Labeled("Point") }, p1);
                else
                    t1 = (Thing)P1;
                if (P2 is PointPlus p2)
                    t2 = kb.AddThing("p" + pCount++, new Thing[] { kb.Labeled("Point") }, p2);
                else
                    t2 = (Thing)P2;
                Thing color = kb.Valued(theColor);
                if (color == null)
                    color = kb.AddThing("c" + cCount++, new Thing[] { kb.Labeled("Color") }, theColor);
                Thing newThing = kb.AddThing("s" + sCount++, new Thing[] { kb.Labeled("Segment") }, null, new Thing[] { t1, t2, color });
                return newThing;
            }
            return null;
        }

        //if the new segment extends an existing segment, merge/delete the intermediate point(s)
        //if the new segment lies within an existing segment, delete it
        private bool MergeNewSegment(Segment s)
        {
            //Segment s = SegmentFromKBThing(t1);
            foreach (Thing t in KBSegments)
            {
                //                if (t != t1)
                {
                    Segment segment = SegmentFromKBThing(t);
                    if (s.theColor == segment.theColor)
                    {

                        //if one of the points is not on the line...this is not the stored object we're looking for
                        float d1 = Utils.DistancePointToLine(s.P1.P, segment.P1.P, segment.P2.P);
                        float d2 = Utils.DistancePointToLine(s.P2.P, segment.P1.P, segment.P2.P);
                        if (d1 > 0.05 || d2 > 0.05) continue; //we may need to loosen this to handle seen segments

                        //if both points are not on the segment, there is no overlap (but the new segment may completely include the other which we'll ignore)
                        Point closest;
                        d1 = (float)Utils.FindDistanceToSegment(s.P1.P, segment.P1.P, segment.P2.P, out closest);
                        d2 = (float)Utils.FindDistanceToSegment(s.P2.P, segment.P1.P, segment.P2.P, out closest);
                        if (d1 > 0.05 && d2 > 0.05) continue; //we may need to loosen this to handle seen segments

                        //if we got here, the new segment needs to be merged onto this existing segment

                        //if the existing endpoints are used by other segments, we can't change them
                        //TODO this should be obsolete because points are no longer shared
                        bool p1CanChange = t.References[0].T.ReferencedBy.Count < 2;
                        bool p2CanChange = t.References[1].T.ReferencedBy.Count < 2;

                        //sort by theta so we get the true max/min points
                        List<PointPlus> points = new List<PointPlus> {
                            s.P1,
                            s.P2,
                            (PointPlus)t.References[0].T.V,
                            (PointPlus)t.References[1].T.V,
                            };
                        points.Sort((x, y) => x.Theta.CompareTo(y.Theta));
                        if (((PointPlus)t.References[0].T.V).Theta < ((PointPlus)t.References[1].T.V).Theta)
                        {
                            //the points on the existing segment are alreaady in the right order;
                            if (!points[0].Near((PointPlus)t.References[0].T.V, .01f))
                            {
                                if (p1CanChange)
                                    t.References[0].T.V = points[0];
                                else
                                    ChangeReference(t, 0, newPoint(points[0]));
                                if (p2CanChange)
                                    t.References[1].T.V = points[3];
                                else
                                    ChangeReference(t, 1, newPoint(points[3]));
                            }
                        }
                        else
                        {
                            if (!points[3].Near((PointPlus)t.References[0].T.V, .01f))
                            {
                                if (p1CanChange)
                                    t.References[0].T.V = points[3];
                                else
                                    ChangeReference(t, 0, newPoint(points[3]));
                            }
                            if (!points[0].Near((PointPlus)t.References[1].T.V, .01f))
                            {
                                if (p2CanChange)
                                    t.References[1].T.V = points[0];
                                else
                                    ChangeReference(t, 1, newPoint(points[0]));
                            }
                        }

                        return true;
                    }
                }
            }
            return false;
        }
        //move to Thing as a metnod
        private void ChangeReference(Thing t, int num, Thing newThing)
        {
            Thing oldRef = t.References[num].T;
            t.RemoveReference(oldRef);
            t.References.Insert(0, new Link() { T = newThing });
        }

        //get input from touch... accurate locations, no color
        public bool AddSegmentFromTouch(PointPlus P1, PointPlus P2)
        {
            //if (P1 != P2) return false;
            if (KBSegments is null) return false;
            if (imagining) return false;

            ModuleView naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
            if (naKB == null) return false;
            if (naKB.TheModule is Module2DKB kb)
            {
                int theColor = Utils.ColorToInt(Colors.Wheat);
                Segment s = new Segment() { P1 = P1, P2 = P2, theColor = theColor };
                bool newSegmentMerged = MergeNewSegment(s);

                if (!newSegmentMerged)
                {
                    Thing t1 = NearestPoint(P1.Theta);
                    Thing t2 = NearestPoint(P2.Theta);
                    Thing newThing;
                    if (t1 != null && t2 != null)
                    {
                        t1.V = P1;
                        t2.V = P2;
                        newThing = AddSegmentToKB(t1, t2, theColor);
                    }
                    else if (t1 != null)
                    {
                        t1.V = P1;
                        newThing = AddSegmentToKB(t1, P2, theColor);
                    }
                    else if (t2 != null)
                    {
                        t2.V = P2;
                        newThing = AddSegmentToKB(P1, t2, theColor);
                    }
                    else
                    {
                        newThing = AddSegmentToKB(P1, P2, theColor);
                    }
                }
                UpdateDialog();
            }
            return false;
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
        public float GetDistanceAtDirection(float theta)
        {
            if (KBSegments == null) return 0;
            float retVal = float.MaxValue;
            Thing nearest = null;
            Segment s = null;
            foreach (Thing t in KBSegments)
            {
                s = SegmentFromKBThing(t);

                //does this object cross the given visual angle?
                PointPlus pv = new PointPlus { R = 10, Theta = theta };
                Utils.FindIntersection(new Point(0, 0), pv.P, s.P1.P, s.P2.P,
                    out bool lines_intersect, out bool segments_intersect, out Point intersection, out Point clos_p1, out Point close_p2, out double collisionAngle);
                if (!segments_intersect) continue;

                //and is it the nearest?
                Vector v = (Vector)intersection;
                if (v.Length < retVal)
                {
                    nearest = t;
                    retVal = (float)v.Length;
                }
            }
            if (retVal > 10000) retVal = 0;
            return retVal;
        }


        public Thing GetColorAtDirection(float theta)
        {
            Thing nearest = null;
            float dist = float.MaxValue;
            Segment s = null;
            foreach (Thing t in KBSegments)
            {
                s = SegmentFromKBThing(t);

                //does this object cross the given visual angle?
                PointPlus pv = new PointPlus { R = 10, Theta = theta };
                Utils.FindIntersection(new Point(0, 0), pv.P, s.P1.P, s.P2.P,
                    out bool lines_intersect, out bool segments_intersect, out Point intersection, out Point clos_p1, out Point close_p2, out double collisionAngle);
                if (!segments_intersect) continue;

                //and is it the nearest?
                Vector v = (Vector)intersection;
                if (v.Length < dist)
                {
                    nearest = t;
                    dist = (float)v.Length;
                }
            }
            if (nearest != null && nearest.References.Count > 2)
            {
                return nearest.References[2].T;
            }
            return null;
        }

        //not presently used
        public bool SetColor(float theta, Color theColor)
        {
            //int nearest = -1;
            //float dist = float.MaxValue;
            //Segment s = null;
            //for (int i = 0; i < KBSegments.Count; i++)
            //{
            //    s = SegmentFromKBThing(KBSegments[i]);
            //    //has color already been assigned?
            //    if (s.theColor != Colors.Wheat) continue;

            //    //does this object cross the given visual angle?
            //    PointPlus pv = new PointPlus() { R = 10, Theta = theta };
            //    Utils.FindIntersection(new Point(0, 0), pv.P, s.P1.P, s.P2.P,
            //        out bool lines_intersect, out bool segments_intersect, out Point intersection, out Point clos_p1, out Point close_p2, out double collisionAngle);
            //    if (!segments_intersect) continue;

            //    //and is it the nearest?
            //    Vector v = (Vector)intersection;
            //    if (v.Length < dist)
            //    {
            //        nearest = i;
            //        dist = (float)v.Length;
            //    }
            //}
            //if (nearest != -1)
            //{
            //    s.theColor = theColor;
            //    SegmentToKBThing(s, KBSegments[nearest]);
            //    na.GetNeuronAt("Color").SetValue(1);
            //    return true;
            //}
            return false;
        }

        //TODO: see if this is useful
        public Segment FindGreen()
        {
            foreach (Thing t in KBSegments)
            {
                Segment s = SegmentFromKBThing(t);
                if (s.theColor == Utils.ColorToInt(Colors.Green))
                    return s;
            }
            return null;
        }

        //find the nearest Segment to the entity
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

        private bool InRange(PointPlus p, float rangeAhead)
        {
            bool retVal = false;
            if (p.X <= 0) return retVal;
            if (p.Y > -rangeAhead && p.Y < rangeAhead) retVal = true;
            return retVal;
        }
        public Thing NearestPoint(Angle theta, float toler = .01f)
        {
            GetSegmentsFromKB();
            Thing retVal = null;
            float dist = 1000;
            foreach (Thing t in KBPoints)
            {
                if (t.V is PointPlus p)
                {
                    if (Abs(p.Theta - theta) < toler)
                    {
                        if (p.R < dist)
                        {
                            dist = p.R;
                            retVal = t;
                        }
                    }
                }
            }
            return retVal;
        }


        //this takes into acount the width of the entity to see if a collision might be imminent
        public Thing NearestThingAhead()
        {
            if (KBSegments == null) return null;
            float requiredPathWidth = 0.2f;
            Thing retVal = null;
            float dist = 100;
            foreach (Thing t in KBSegments)
            {
                Segment s = SegmentFromKBThing(t);
                if (s.P1.X < 0 && s.P2.X < 0) continue;
                if (InRange(s.P1, requiredPathWidth) ||
                    InRange(s.P2, requiredPathWidth) ||
                    (s.P2.Y < -requiredPathWidth && s.P1.Y > requiredPathWidth) ||
                    (s.P1.Y < -requiredPathWidth && s.P2.Y > requiredPathWidth)
                    )
                {
                    Utils.FindDistanceToSegment(new Point(0, 0), s.P1.P, s.P2.P, out Point closest);
                    if (closest.X > 0)
                    {
                        float newDist = (float)((Vector)closest).Length;
                        if (newDist < dist)
                        {
                            dist = newDist;
                            retVal = t;
                        }
                    }
                }
            }
            return retVal;
        }

        //maintain a list of objects in the current visual field
        public void FireVisibleObjects()
        {
            ModuleView naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
            if (naKB == null) return;
            Module2DKBN kb = (Module2DKBN)naKB.TheModule;
            if (KBSegments == null) return;
            //Thing tVisible = kb.Labeled("Visible");

            //clear all visiblility references
            //for (int i = 0; i < KBSegments.Count; i++)
            //    KBSegments[i].RemoveReference(tVisible);

            ModuleView naVision = theNeuronArray.FindAreaByLabel("Module2DVision");
            if (naVision == null) return;
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
                        //TODO...only fire the closest at each point
                        kb.Fire(t);
                        //                        t.AddReference(tVisible);
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

        private static int CompareSegmentsByDistance(Thing t1, Thing t2)
        {
            Segment s1 = SegmentFromKBThing(t1);
            Segment s2 = SegmentFromKBThing(t2);
            float d1 = (float)Utils.FindDistanceToSegment(s1);
            float d2 = (float)Utils.FindDistanceToSegment(s2);
            if (d1 > d2) return 1;
            if (d1 == d2)
            {
                //this is a bit of a hack to ensure that the list always comes back in the same order
                if (s1.theColor > s2.theColor) return 1;
            }
            return -1;
        }

        public List<Thing> NearbySegments(int max = 1)
        {
            List<Thing> retVal = new List<Thing>();
            if (KBSegments == null) return retVal;
            if (KBSegments.Count == 0) return retVal;
            foreach (Thing t in KBSegments)
            {
                retVal.Add(t);
            }
            retVal.Sort(CompareSegmentsByDistance);
            int matches = 0;
            Segment s = SegmentFromKBThing(retVal[0]);
            float d = (float)Utils.FindDistanceToSegment(s);
            for (int i = 1; i < retVal.Count; i++)
            {
                s = SegmentFromKBThing(retVal[i]);
                float d1 = (float)Utils.FindDistanceToSegment(s);
                matches++;
                if (i >= max && Abs(d - d1) > .1)
                    break;
                d = d1;
            }
            if (matches > max)
                max = matches;
            if (retVal.Count > max)
                retVal.RemoveRange(max, retVal.Count - max);
            return retVal;
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

        //This adjust all the objects in the model for an entity rotation
        public void Rotate(float theta)
        {
            if (KBPoints == null) return;
            //move all the objects in the model
            foreach (Thing t in KBPoints)
            {
                if (t.V != null && t.V is PointPlus P)
                {
                    P.Theta += theta;
                }
            }
            UpdateDialog();
        }

        public void Move(float x, float y)
        {
            if (KBPoints == null) return;
            //move all the objects in the model
            foreach (Thing t in KBPoints)
            {
                if (t.V != null && t.V is PointPlus P)
                {
                    P.X -= x;
                    P.Y -= y;
                }
            }
            UpdateDialog();
        }

        //This adjust all the objects in the model for an entity motion
        public void Move(float motion)
        {
            Move(motion, 0);
        }


        public override void Initialize()
        {
            pCount = 0;
            sCount = 0;
            cCount = 0;
            ppCount = 0;

            GetSegmentsFromKB();

            Module2DKBN kbn = (Module2DKBN)FindModuleByType(typeof(Module2DKBN));
            if (kbn == null) return;
            while (KBSegments.Count > 0)
            {
                kbn.DeleteThing(KBSegments[0]);
            }

            while (KBPoints.Count > 0)
            {
                kbn.DeleteThing(KBPoints[0]);
            }

            na.GetNeuronAt(0, 0).Label = "New";
            na.GetNeuronAt(1, 0).Label = "Change";
            AddLabel("Obstacle");
            UpdateDialog();
        }
    }
}



