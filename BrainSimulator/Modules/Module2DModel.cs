//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using static BrainSimulator.Utils;
using static System.Math;



namespace BrainSimulator.Modules
{

    public class Module2DModel : ModuleBase
    {
        private List<Thing> UKSSegments;
        private List<Thing> UKSPoints;

        //these are public to let the dialog box use the info

        public List<Thing> GetUKSSegments() { return UKSSegments; }
        public List<Thing> GetUKSPoints() { return UKSPoints; }

        //these are used to build labels for things
        //TODO make public and reset on initialize
        int pCount = 0;
        int sCount = 0;
        int cCount = 0;
        int mCount = 0;

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
            UKSSegments = new List<Thing>();
            UKSPoints = new List<Thing>();
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            if (UKSPoints == null || UKSPoints.Count == 0)
                GetSegmentsFromUKS();

            ModuleBehavior nmBehavior = (ModuleBehavior)FindModuleByType(typeof(ModuleBehavior));
            Thing obstacle = NearestThingAhead();
            if (obstacle != null)
            {
                Segment s = SegmentFromUKSThing(obstacle);
                Utils.FindDistanceToSegment(new Point(0, 0), s.P1.P, s.P2.P, out Point closest);
                double dist = ((Vector)closest).Length;

                if (dist < 0.6f)
                {
                    //we are approaching an obsctacle.
                    SetNeuronValue(null, "Obstacle", 1);
                }
            }
        }

        public void GetSegmentsFromUKS()
        {
            ModuleUKSN nmUKS = (ModuleUKSN)FindModuleByType(typeof(ModuleUKSN));
            if (nmUKS is ModuleUKS UKS)
            {
                UKSSegments = UKS.Labeled("Segment").Children;
                UKSPoints = new List<Thing>();
                if (UKS.Labeled("ModelThing") != null)
                    UKSPoints = UKS.Labeled("ModelThing").Children;
            }
        }

        public static Segment SegmentFromUKSThing(Thing t)
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
                else if (s.P2 == null && t1.V is PointPlus p2)
                    s.P2 = p2;
                else if (t1.V is PointPlus p3)
                    s.Motion = p3;
                else if (t1.V is int c)
                    s.theColor = c;
            }
            if (s.P1 == null || s.P2 == null) return null;
            return s;
        }

        //Segments consist of two points and a color. Optionally, a segment may have some motion. 
        //AddToModel determines whether or not the endpoints are to be modified as Sallie moves so static objects can be stored
        public Thing AddSegmentToUKS(Segment s)
        {
            return AddSegmentToUKS(s.P1, s.P2, s.theColor);
        }
        public Thing AddSegmentToUKS(PointPlus P1, PointPlus P2, int theColor, PointPlus motion = null, bool addToModel = true)
        {
            ModuleUKS nmUKS = (ModuleUKSN)FindModuleByType(typeof(ModuleUKSN));
            if (nmUKS is ModuleUKS UKS)
            {
                Thing t1, t2;
                Thing t3 = null;
                t1 = UKS.AddThing("p" + pCount++, new Thing[] { UKS.Labeled("Point") }, P1);
                t2 = UKS.AddThing("p" + pCount++, new Thing[] { UKS.Labeled("Point") }, P2);
                if (addToModel)
                {
                    t1.AddParent(UKS.Labeled("ModelThing"));
                    t2.AddParent(UKS.Labeled("ModelThing"));
                }
                if (motion != null)
                    t3 = UKS.AddThing("m" + mCount++, new Thing[] { UKS.Labeled("Motion") }, motion);
                Thing color = UKS.Valued(theColor);
                if (color == null)
                    color = UKS.AddThing("c" + cCount++, new Thing[] { UKS.Labeled("Color") }, theColor);
                Thing newThing = null;
                if (motion != null)
                    newThing = UKS.AddThing("s" + sCount++, new Thing[] { UKS.Labeled("Segment") }, null, new Thing[] { t1, t2, color, t3 });
                else
                    newThing = UKS.AddThing("s" + sCount++, new Thing[] { UKS.Labeled("Segment") }, null, new Thing[] { t1, t2, color });
                return newThing;
            }
            return null;
        }


        public Thing MostLikelySegment(Segment newSegment)
        {
            Thing retVal = null;
            if (UKSSegments == null) return null;
            foreach (Thing t in UKSSegments)
            {
                Segment s = SegmentFromUKSThing(t);
                if (s == null) continue;
                if (s.theColor == newSegment.theColor)
                {
                    //TODO is the segment visible?
                    retVal = t;
                    break;
                }
            }
            return retVal;
        }

        public static void OrderSegment(object x)
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
            if (UKSPoints == null) return null;
            Angle closestTheta = Rad(180);
            foreach (Thing t in UKSPoints)
            {
                if (t.ReferencedBy.Count > 0)
                {
                    Segment s = SegmentFromUKSThing(t.ReferencedBy[0].T);
                    if (s == null) continue;
                    if (s.theColor == theColor)
                    {
                        if (t.V is PointPlus p)
                        {
                            Angle deltaAngle = Abs(p.Theta - p1.Theta);
                            if (deltaAngle < closestTheta)
                            {
                                closestTheta = deltaAngle;
                                retVal = t;
                            }
                        }
                    }
                }
            }
            return retVal;
        }

        public void UpdateEndpointFromVision(PointPlus P1, ColorInt theColor, bool moved)
        {
            //Debug.WriteLine("UpdatePoint: " + P1 + theColor);
            Thing match = MostLikelyPoint(P1, theColor);
            if (match != null)
            {
                if (match.V is PointPlus p)
                    if (P1.Conf < p.Conf || moved)
                        match.V = P1;
                UpdateDialog();
            }
        }


        //TODO: rewrite again
        public Thing AddSegmentFromVision(PointPlus P1, PointPlus P2, ColorInt theColor, bool moved)
        {
            Thing retVal = null;
            Debug.WriteLine("AddSegment: " + P1 + P2 + theColor);
            //determine if the segment is already in the UKS.  
            //Correct it if it is there, add it if it is not.
            //FUTURE: detect motion
            if (theColor == 0) return null;
            Segment newSegment = new Segment() { P1 = P1, P2 = P2, theColor = theColor };
            ModuleUKSN UKS = (ModuleUKSN)FindModuleByType(typeof(ModuleUKSN));
            GetSegmentsFromUKS();
            if (UKS != null)
            {
                //it's easier if we sort by theta
                OrderSegment(newSegment);
                retVal = MostLikelySegment(newSegment);
                if (retVal != null)
                {
                    //UKS.Fire(match);
                    //OrderSegment(match);
                    //Segment s = SegmentFromUKSThing(match);
                    //float newVisualWidth = newSegment.VisualWidth();
                    //float matchVisualWidth = s.VisualWidth();
                    ////if the newVisualWidth is bigger, an adjustment is needed
                    ////this happens if the initial view was occluded but now it is less
                    //Thing match1 = MostLikelyPoint(newSegment.P1, newSegment.theColor);
                    //Thing match2 = MostLikelyPoint(newSegment.P2, newSegment.theColor);
                    //if (match1 != null && match2 != null)
                    //{
                    //    if (newSegment.P1.Conf < s.P1.Conf)
                    //    {
                    //        s.P1.Conf = newSegment.P1.Conf;
                    //        s.P1.R = newSegment.P1.R;
                    //        s.P1.Theta = newSegment.P1.Theta;
                    //    }
                    //    if (newSegment.P2.Conf < s
                    //        .P2.Conf)
                    //    {
                    //        s.P2.Conf = newSegment.P2.Conf;
                    //        s.P2.R = newSegment.P2.R;
                    //        s.P2.Theta = newSegment.P2.Theta;
                    //    }
                    //}

                    ////there is a significant point mismatch...
                    //else
                    //{
                    //    if (match1 == null && newSegment.P1.R > s.P1.R)
                    //    {
                    //        s.P1.Conf = newSegment.P1.Conf;
                    //        s.P1.R = newSegment.P1.R;
                    //        s.P1.Theta = newSegment.P1.Theta;
                    //    }
                    //    if (match2 == null && newSegment.P2.R > s.P2.R)
                    //    {
                    //        s.P2.Conf = newSegment.P2.Conf;
                    //        s.P2.R = newSegment.P2.R;
                    //        s.P2.Theta = newSegment.P2.Theta;
                    //    }
                    //}
                }
                else
                {
                    retVal = AddSegmentToUKS(P1, P2, theColor);
                    UKS.Fire(retVal);
                }
                UpdateDialog();
            }
            return retVal;
        }


        //TODO this is not currently used but is needed
        //if the new segment extends an existing segment, merge/delete the intermediate point(s)
        //if the new segment lies within an existing segment, ignore it
        private Thing MergeNewSegment(Segment s)
        {
            OrderSegment(s);
            foreach (Thing t in UKSSegments)
            {
                Segment segment = SegmentFromUKSThing(t);

                //if one of the points is not on the line...this is not the stored object we're looking for
                float d1 = Utils.DistancePointToLine(s.P1.P, segment.P1.P, segment.P2.P);
                float d2 = Utils.DistancePointToLine(s.P2.P, segment.P1.P, segment.P2.P);
                if (d1 > 0.1 || d2 > 0.1) continue; //we may need to loosen this to handle seen segments

                //if both points are not on the segment, there is no overlap (but the new segment may completely include the other which we'll ignore)
                Point closest;
                d1 = (float)Utils.FindDistanceToSegment(s.P1.P, segment.P1.P, segment.P2.P, out closest);
                d2 = (float)Utils.FindDistanceToSegment(s.P2.P, segment.P1.P, segment.P2.P, out closest);
                if (d1 > 0.05 && d2 > 0.05) continue; //we may need to loosen this to handle seen segments

                //if we got here, the new segment needs to be merged onto this existing segment

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
                        t.References[0].T.V = points[0];
                        t.References[1].T.V = points[3];
                    }
                }
                else
                {
                    if (!points[3].Near((PointPlus)t.References[0].T.V, .01f))
                    {
                        t.References[0].T.V = points[3];
                    }
                    if (!points[0].Near((PointPlus)t.References[1].T.V, .01f))
                    {
                        t.References[1].T.V = points[0];
                    }
                }

                return t;
            }
            return null;
        }

        //get input from touch... accurate locations, no color
        public bool AddSegmentFromTouch(PointPlus P1, PointPlus P2, PointPlus motion, int arm)
        {
            //if conf=0, it's a known endpoint. conf=1, not an endpoint
            ModuleUKSN UKS = (ModuleUKSN)FindModuleByType(typeof(ModuleUKSN));
            if (UKS is null) return false;
            if (UKSSegments is null) return false;
            if (imagining) return false;

            ColorInt theColor = Utils.ColorToInt(Colors.Wheat);
            Segment newSegment = new Segment() { P1 = P1, P2 = P2, theColor = theColor };
            //OrderSegment(newSegment);

            Thing t1 = GetNearestThing(newSegment.MidPoint.Theta, out float dist1);
            //TODO: for motion testing
            t1 = UKS.Labeled("s0");
            if (t1 == null)
            {
                //TODO: merge mutliple segments
                //                AddSegmentToUKS(P1, P2, theColor, motion); //don't store motion with the segment (yet)
                AddSegmentToUKS(P1, P2, theColor);
            }

            else if (dist1 < 1)
            {
                Segment prevSegment = SegmentFromUKSThing(t1);
                PointPlus prevMidpoint = prevSegment.MidPoint;
                Angle oldM = prevSegment.Angle;
                Angle newM = newSegment.Angle;
                PointPlus offset = new PointPlus() { R = prevSegment.Length, Theta = newM };
                if (P1.Conf == 0 && P2.Conf == 0) //we're given both endpoints
                {
                    prevSegment.P1.P = P1.P; prevSegment.P1.Conf = 0;
                    prevSegment.P2.P = P2.P; prevSegment.P2.Conf = 0;
                }
                else if (P1.Conf == 0)
                {
                    prevSegment.P1.P = P1.P; prevSegment.P1.Conf = 0;
                    prevSegment.P2.P = P1.P - offset.V;
                }
                else if (P2.Conf == 0)
                {
                    prevSegment.P1.P = P2.P; prevSegment.P2.Conf = 0;
                    prevSegment.P2.P = P2.P + offset.V;
                }
                else
                {
                    //we're not near an endpoint--match the modpoint as close as possible & preserve length
                    //make the segment match the two points
                    PointPlus newMidpoint1 = new PointPlus() { P = (Point)GetClosestPointOnLine(P1.V, P2.V, prevMidpoint.V), };
                    //offset is the dietance from the midpoint to each end
                    offset.R = offset.R / 2;
                    PointPlus newP1 = new PointPlus() { P = newMidpoint1.P + offset.V };
                    PointPlus newP2 = new PointPlus() { P = newMidpoint1.P - offset.V };
                    prevSegment.P1.R = newP1.R; prevSegment.P1.Theta = newP1.Theta;
                    prevSegment.P2.R = newP2.R; prevSegment.P2.Theta = newP2.Theta;
                }
                PointPlus newMidpoint = prevSegment.MidPoint;
                newMidpoint.P = newMidpoint.P - prevMidpoint.V;
                if (newMidpoint.R > 0.01 && motion.R != 0)
                {
                    if (prevSegment.Motion == null)
                    {
                        prevSegment.Motion = new PointPlus();
                        Thing tMotion = UKS.AddThing("m" + mCount++, UKS.Labeled("Point"));
                        tMotion.V = prevSegment.Motion;
                        t1.AddReference(tMotion);
                    }
                    prevSegment.Motion.R = motion.R;
                    prevSegment.Motion.Theta = motion.Theta;
                    prevSegment.Motion.Conf = newM - oldM;
                }
            }

            UpdateDialog();
            return false;
        }

        public float GetDistanceAtDirection(Angle theta)
        {
            GetNearestThing(theta, out float dist);
            return dist;
        }

        public Thing GetNearestThing(Angle theta = default(Angle))
        {
            if (theta is null) theta = 0;
            return GetNearestThing(theta, out float dist);
        }

        public Thing GetNearestThing(Angle theta, out float dist)
        {
            Thing nearest = null;
            dist = float.MaxValue;
            Segment s = null;
            foreach (Thing t in UKSSegments)
            {
                s = SegmentFromUKSThing(t);
                //OrderSegment(s);

                //does this object cross the given visual angle?
                PointPlus pv = new PointPlus { R = 10, Theta = theta };
                Utils.FindIntersection(new Point(-.2, 0), pv.P, s.P1.P, s.P2.P,
                    out bool lines_intersect, out bool segments_intersect, out Point intersection, out Point clos_p1, out Point close_p2, out double collisionAngle);
                if (!segments_intersect)
                {
                    continue;
                }
                //and is it the nearest?
                Vector v = (Vector)intersection;
                if (v.Length < dist)
                {
                    nearest = t;
                    dist = (float)v.Length;
                }
            }
            if (nearest != null)
            {
                return nearest;
            }
            return null;
        }

        //not presently used
        public bool SetColor(Angle theta, Color theColor)
        {
            //int nearest = -1;
            //float dist = float.MaxValue;
            //Segment s = null;
            //for (int i = 0; i < UKSSegments.Count; i++)
            //{
            //    s = SegmentFromUKSThing(UKSSegments[i]);
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
            //    SegmentToUKSThing(s, UKSSegments[nearest]);
            //    na.GetNeuronAt("Color").SetValue(1);
            //    return true;
            //}
            return false;
        }

        //TODO: see if this is useful
        public Segment FindGreen()
        {
            foreach (Thing t in UKSSegments)
            {
                Segment s = SegmentFromUKSThing(t);
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
            for (int i = 0; i < UKSSegments.Count; i++)
            {
                s = SegmentFromUKSThing(UKSSegments[i]);
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
            GetSegmentsFromUKS();
            Thing retVal = null;
            float dist = 1000;
            foreach (Thing t in UKSPoints)
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
            if (UKSSegments == null) return null;
            float requiredPathWidth = 0.2f;
            Thing retVal = null;
            float dist = 100;
            foreach (Thing t in UKSSegments)
            {
                Segment s = SegmentFromUKSThing(t);
                if (s == null) return null;
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
            ModuleView naUKS = theNeuronArray.FindModuleByLabel("Module2DUKS");
            if (naUKS == null) return;
            ModuleUKSN UKS = (ModuleUKSN)naUKS.TheModule;
            if (UKSSegments == null) return;
            //Thing tVisible = UKS.Labeled("Visible");

            //clear all visiblility references
            //for (int i = 0; i < UKSSegments.Count; i++)
            //    UKSSegments[i].RemoveReference(tVisible);

            ModuleView naVision = theNeuronArray.FindModuleByLabel("Module2DVision");
            if (naVision == null) return;
            int possibleViewAngles = naVision.Width;
            float deltaTheta = Module2DVision.fieldOfView / possibleViewAngles;
            for (int i = 0; i < possibleViewAngles; i++)
            {
                float theta = (float)-Module2DVision.fieldOfView / 2 + (i * deltaTheta);
                PointPlus P = new PointPlus { R = 10, Theta = theta };
                foreach (Thing t in UKSSegments)
                {
                    Segment s = SegmentFromUKSThing(t);
                    if (s == null) continue;
                    Utils.FindIntersection(new Point(0, 0), P.P, s.P1.P, s.P2.P,
                        out bool lines_intersect, out bool segments_intersect,
                        out Point intersection, out Point close_p1, out Point closep2, out double collisionAngle);
                    if (segments_intersect)
                    {
                        //TODO...only fire the closest at each point
                        UKS.Fire(t);
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
            ModuleUKSN UKS = (ModuleUKSN)FindModuleByType(typeof(ModuleUKSN));
            if (UKS == null) return null;
            bool ok = true;
            foreach (Thing t in UKSSegments)
            {
                Segment s = SegmentFromUKSThing(t);
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
            Segment s1 = SegmentFromUKSThing(t1);
            Segment s2 = SegmentFromUKSThing(t2);
            if (s1 == null || s2 == null)
                return 0;
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
            if (UKSSegments == null) return retVal;
            if (UKSSegments.Count == 0) return retVal;
            foreach (Thing t in UKSSegments)
            {
                retVal.Add(t);
            }
            retVal.Sort(CompareSegmentsByDistance);
            int matches = 0;
            Segment s = SegmentFromUKSThing(retVal[0]);
            float d = (float)Utils.FindDistanceToSegment(s);
            for (int i = 1; i < retVal.Count; i++)
            {
                s = SegmentFromUKSThing(retVal[i]);
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
        //this will all need to be converted to run with UKS Things instead of segments
        [XmlIgnore]
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
            if (UKSPoints == null) return;
            //move all the objects in the model
            foreach (Thing t in UKSPoints)
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
            if (UKSPoints == null) return;
            //move all the objects in the model
            foreach (Thing t in UKSPoints)
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

            GetSegmentsFromUKS();

            ModuleUKSN UKSn = (ModuleUKSN)FindModuleByType(typeof(ModuleUKSN));
            if (UKSn == null) return;
            while (UKSSegments.Count > 0)
            {
                UKSn.DeleteThing(UKSSegments[0]);
            }

            while (UKSPoints.Count > 0)
            {
                UKSn.DeleteThing(UKSPoints[0]);
            }

            na.GetNeuronAt(0, 0).Label = "New";
            na.GetNeuronAt(1, 0).Label = "Change";
            AddLabel("Obstacle");
            UpdateDialog();
        }
    }
}



