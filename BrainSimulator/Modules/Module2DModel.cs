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

namespace BrainSimulator
{
    public class PointPlus
    {
        private Point p;
        private float r;
        private float theta;
        private bool polarDirty = false;
        private bool xyDirty = false;

        [XmlIgnore]
        public Point P
        {
            get { if (xyDirty) UpdateXY(); return p; }
            set { polarDirty = true; p = value; }
        }
        public float X { get { if (xyDirty) UpdateXY(); return (float)p.X; } set { p.X = value; polarDirty = true; } }
        public float Y { get { if (xyDirty) UpdateXY(); return (float)p.Y; } set { p.Y = value; polarDirty = true; } }
        [XmlIgnore]
        public Vector V { get => (Vector)P; }
        [XmlIgnore]
        public float Degrees { get => (float)(Theta * 180 / Math.PI); }
        public float Conf { get; set; }
        [XmlIgnore]
        public float R { get { if (polarDirty) UpdatePolar(); return r; } set { r = value; xyDirty = true; } }
        [XmlIgnore]
        public float Theta
        {
            get { if (polarDirty) UpdatePolar(); return theta; }
            set
            {//keep theta within the range +/- PI
                theta = value;
                if (theta > Math.PI) theta -= 2 * (float)Math.PI;
                if (theta < -Math.PI) theta += 2 * (float)Math.PI;
                xyDirty = true;
            }
        }

        private void UpdateXY()
        {
            p.X = r * Math.Cos(theta);
            p.Y = r * Math.Sin(theta);
            xyDirty = false;
        }

        public void UpdatePolar()
        {
            theta = (float)Math.Atan2(p.Y, p.X);
            r = (float)Math.Sqrt(p.X * p.X + p.Y * p.Y);
            polarDirty = false;
        }
        public bool Near(PointPlus PP, float toler)
        {
            if ((Math.Abs(PP.R - R) < 1 && Math.Abs(PP.Theta - Theta) < .1) ||
                ((Math.Abs(PP.X - X) < toler && Math.Abs(PP.Y - Y) < toler)))
                return true;
            return false;
        }
    }

    public class Segment
    {
        public PointPlus P1;
        public PointPlus P2;
        public Color theColor;
        public PointPlus MidPoint()
        {
            return new PointPlus { X = (P1.X + P2.X) / 2, Y = (P1.Y + P2.Y) / 2 };
        }
    }

    public class Module2DModel : ModuleBase
    {
        private List<Thing> KBSegments;
        private List<Thing> KBPossiblePoints;
        public List<Thing> GetKBSegments() { return KBSegments; }
        public List<Thing> GetKBPossiblePoints() { return KBPossiblePoints; }


        public Module2DModel()
        {
            KBSegments = new List<Thing>();
            KBPossiblePoints = new List<Thing>();
        }

        public void GetSegmentsFromKB()
        {
            NeuronArea naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
            if (naKB == null) return;
            if (naKB.TheModule is Module2DKB kb)
            {
                KBSegments = kb.HavingParent(kb.Labeled("Segment"));
                KBPossiblePoints = kb.HavingParent(kb.Labeled("PossiblePoint"));
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
                else if (t1.V is Color c)
                    s.theColor = c;
            }
            return s;
        }

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

        private void AddSegmentToKB(PointPlus P1, PointPlus P2, Color theColor)
        {
            NeuronArea naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
            if (naKB == null) return;
            if (naKB.TheModule is Module2DKB kb)
            {
                Thing p1 = kb.AddThing("", new Thing[] { kb.Labeled("Point") }, P1);
                Thing p2 = kb.AddThing("", new Thing[] { kb.Labeled("Point") }, P2);
                Thing color = kb.Valued(theColor);
                if (color == null)
                    color = kb.AddThing("", new Thing[] { kb.Labeled("Color") }, theColor);
                kb.AddThing("", new Thing[] { kb.Labeled("Segment") }, null, new Thing[] { p1, p2, color });
            }
        }

        //get input from vision...less accurate
        public void AddPosiblePointToKB(PointPlus P, Color theColor)
        {
            NeuronArea naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
            if (naKB == null) return;
            if (naKB.TheModule is Module2DKB kb)
            {
                if (kb.Valued(P) == null)
                {
                    //will there be two points with the same color which might be merged into a possible segment?
                    bool segmentAdded = false;
                    foreach (Thing t1 in KBPossiblePoints)
                    {
                        if (t1.V is PointPlus pp)
                        {
                            //this is a bit of a hack which joins points which have the same color
                            if (t1.References.Count > 0 && t1.References[0].T.V is Color tColor && tColor == theColor)
                            {
                                AddSegmentToKB(P, pp, theColor);
                                kb.DeleteThing(t1);
                                segmentAdded = true;
                                break;
                            }
                        }
                    }
                    if (!segmentAdded)
                    {
                        Thing newThing = kb.AddThing("P", new Thing[] { kb.Labeled("PossiblePoint") }, P);
                        if (newThing != null)
                        {
                            Thing color = kb.Valued(theColor);
                            if (color == null)
                                color = kb.AddThing("", new Thing[] { kb.Labeled("Color") }, theColor);
                            newThing.AddReference(color);
                        }
                    }
                }
            }
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
        }

        //get input from touch... accurate locations
        public bool AddSegment(PointPlus P1, PointPlus P2, Color theColor)
        {
            if (KBSegments is null) return false;
            if (imagining) return false;
            bool found = false;
            bool modelChanged = false;
            if (P1.Conf == 0 && P2.Conf == 0) return false;
            NeuronArea naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
            if (naKB == null) return false;
            if (naKB.TheModule is Module2DKB kb)
            {
                Thing t1 = kb.Valued(P1, kb.Labeled("Point").Children, .5f);
                if (P1.Conf == 1 && t1 != null && ((PointPlus)t1.V).Conf == 0)
                { t1.V = P1; found = true; }
                Thing t2 = kb.Valued(P2, kb.Labeled("Point").Children, .5f);
                if (P2.Conf == 1 && t2 != null && ((PointPlus)t2.V).Conf == 0)
                { t2.V = P2; found = true; }
                if (found)
                {
                    if (theNeuronArray.FindAreaByLabel("ModuleBehavior") is NeuronArea naBehavior)
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
                AddSegmentToKB(P1, P2, theColor);
                na.GetNeuronAt("New").SetValue(1);
                modelChanged = true;
            }
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
            return modelChanged;
        }

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

        public void MarkVisibleObjects()
        {
            NeuronArea naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
            if (naKB == null) return;
            Module2DKB kb = (Module2DKB)naKB.TheModule;
            Thing tVisible = kb.Labeled("Visible");

            //clear all visiblility references
            for (int i = 0; i < KBSegments.Count; i++)
                KBSegments[i].RemoveReference(tVisible);

            NeuronArea naVision = theNeuronArray.FindAreaByLabel("Module2DVision");
            int possibleViewAngles = naVision.Width;
            float deltaTheta = Module2DVision.fieldOfView / possibleViewAngles;
            for (int i = 0; i < possibleViewAngles; i++)
            {
                float theta = (float)-Module2DVision.fieldOfView / 2 + (i * deltaTheta);
                PointPlus P = new PointPlus { R = 10, Theta = theta };
                foreach(Thing t in KBSegments)
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
            NeuronArea naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
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

        public bool imagining = false;
        PointPlus imaginationOffset;
        float imaginationDirection;
        public List<Segment> imagination = new List<Segment>();

        public void ImagineObject(Segment obj)
        {
            if (!imagining) return;
            imagination.Add(obj);
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
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
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
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
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable

        }

        public override void Initialize()
        {
            GetSegmentsFromKB();
            na.GetNeuronAt(0, 0).Label = "New";
            na.GetNeuronAt(1, 0).Label = "Change";
            na.GetNeuronAt(2, 0).Label = "Color";

            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
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
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
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
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
        }
    }
}



