//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using Microsoft.VisualBasic.CompilerServices;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleStrokeFinder : ModuleBase
    {
        //fill this method in with code which will execute
        //once for each cycle of the engine

        public class Stroke
        {
            public Point p1 = new Point(0, 0);
            public Point p2 = new Point(0, 0);
            public float length { get { return (float)Sqrt((p1.Y - p2.Y) * (p1.Y - p2.Y) + (p1.X - p2.X) * (p1.X - p2.X)); } }
            public Angle angle { get { return Math.Atan2(p1.Y - p2.Y, p1.X - p2.X); } }
            public float width;
            public bool p1Touching = false; //perhaps should be a pointer to other stroke
            public bool p2Touching = false;

            //Motion
            //Color
            //Curvature
            //Taper
        }
        public class Segment0
        {
            public Point p1;
            public Point p2;
            public Angle angle { get { return (Angle)Math.Atan2(p1.Y - p2.Y, p1.X - p2.X); } }
            public float length { get { return (float)((Vector)p1 - (Vector)p2).Length; } }
            public bool handled = false;
        }
        [XmlIgnore]
        public List<Stroke> strokes = new List<Stroke>();
        [XmlIgnore]
        public List<Segment0> lines = new List<Segment0>();

        public override void Fire()
        {
            Init();  //be sure to leave this here

            foreach (Neuron n in na.Neurons())
            {
                n.SetValueInt(0);
                n.Model = Neuron.modelType.FloatValue;
            }

            ModuleView naSource = theNeuronArray.FindModuleByLabel("ModuleBoundary1");
            if (naSource == null) return;

            strokes.Clear();
            lines.Clear();

            ModuleBoundary1 mlf = (ModuleBoundary1)naSource.TheModule;
            if (mlf.boundaries != null)
            {
                DeletePreviousStrokes();
                List<Boundary> theLines = mlf.xx;//.boundaries;
                for (int i = 0; i < theLines.Count; i++)
                {
                    List<Segment0> temp = FindPrimaryLines(theLines[i]);
                    for (int j = 0; j < temp.Count; j++)
                        lines.Add(temp[j]);
                }
                //sort the line endpoints (bounding rectangle)
                for (int i = 0; i < lines.Count; i++)
                {
                    Segment0 theLine = lines[i];
                    if (theLine.p1.X > theLine.p2.X || (theLine.p1.X == theLine.p2.X & theLine.p1.Y > theLine.p2.Y))
                    {
                        Point tempPoint = theLine.p2;
                        theLine.p2 = theLine.p1;
                        theLine.p1 = tempPoint;
                    }
                }

                //process the lines into strokes
                for (int i = 0; i < lines.Count; i++)
                {
                    List<Segment0> linesOfSameSlope = new List<Segment0>();
                    if (!lines[i].handled)
                    {
                        //create a list of lines of same slope
                        Segment0 l1 = lines[i];
                        l1.handled = true;
                        for (int j = i + 1; j < lines.Count; j++)
                        {
                            Segment0 l2 = lines[j];
                            {
                                Angle diff = Math.Min(2 * PI - Abs(l1.angle - l2.angle), Abs(l1.angle - l2.angle));
                                if (diff < Utils.Rad(15) && !l2.handled)
                                {
                                    if (linesOfSameSlope.Count == 0)
                                    {
                                        linesOfSameSlope.Add(l1);
                                    }
                                    linesOfSameSlope.Add(l2);
                                    l2.handled = true;
                                }
                            }
                        }
                        //join colinear lines
                        for (int j = 0; j < linesOfSameSlope.Count; j++)
                        {
                            for (int k = j + 1; k < linesOfSameSlope.Count; k++)
                            {
                                Segment0 l11 = linesOfSameSlope[j];
                                Segment0 l21 = linesOfSameSlope[k];
                                double d1 = Utils.DistancePointToLine(l11.p1, l21.p1, l21.p2);
                                double d2 = Utils.DistancePointToLine(l11.p2, l21.p1, l21.p2);
                                double d3 = Utils.DistancePointToLine(l21.p1, l11.p1, l11.p2);
                                double d4 = Utils.DistancePointToLine(l21.p2, l11.p1, l11.p2);
                                double min = Min(new double[] { d1, d2, d3, d4 });
                                if (min <= .5)
                                {
                                    //find the two furthest points to be new endpoints
                                    Point[] pts = new Point[] { l11.p1, l11.p2, l21.p1, l21.p2 };
                                    GetFurthestPoints(pts);
                                    if (Distance(pts[3], pts[2]) < 11)
                                    {
                                        l11.p1 = pts[0];
                                        l11.p2 = pts[1];
                                        linesOfSameSlope.RemoveAt(k);
                                        break;
                                    }
                                }
                            }
                        }

                        //now look for strokes
                        for (int j = 0; j < linesOfSameSlope.Count; j++)
                        {
                            for (int k = j + 1; k < linesOfSameSlope.Count; k++)
                            {
                                Segment0 l11 = linesOfSameSlope[j];
                                Segment0 l21 = linesOfSameSlope[k];
                                double[] d = new double[4];
                                Point[] near = new Point[4];
                                d[0] = Utils.FindDistanceToSegment(l11.p1, l21.p1, l21.p2, out near[0]);
                                d[1] = Utils.FindDistanceToSegment(l11.p2, l21.p1, l21.p2, out near[1]);
                                d[2] = Utils.FindDistanceToSegment(l21.p1, l11.p1, l11.p2, out near[2]);
                                d[3] = Utils.FindDistanceToSegment(l21.p2, l11.p1, l11.p2, out near[3]);
                                double min = Min(d);
                                if (min < 8) //TODO make relative to length
                                {
                                    CreateCenteredStroke(l11, l21);
                                    linesOfSameSlope.RemoveAt(k);
                                    linesOfSameSlope.RemoveAt(j);
                                    j--;
                                    break;
                                }
                            }
                        }
                    }
                }
                //find stokes which likely connect and connect them.
                for (int i = 0; i < strokes.Count; i++)
                {
                    for (int j = i + 1; j < strokes.Count; j++)
                    {
                        ConnectStrokes(strokes[i], strokes[j]);
                    }
                }
            }

            for (int i = 0; i < strokes.Count; i++)
                SaveThing(strokes[i]);
            FindClustersOfAbsoluteStrodkes();
            ConvertClusterToRelativeStrokes();
            MatchClustersAgainstStoredShapes();
            if (dlg != null)
                UpdateDialog();
        }
        private void CreateCenteredStroke(Segment0 s1, Segment0 s2)
        {
            PointPlus p1 = new PointPlus(s1.p1);
            PointPlus p2 = new PointPlus(s1.p2);
            PointPlus p3 = new PointPlus(s2.p1);
            PointPlus p4 = new PointPlus(s2.p2);

            //transfer first point to origin
            PointPlus p10 = p1 - p1;
            PointPlus p20 = p2 - p1;
            PointPlus p30 = p3 - p1;
            PointPlus p40 = p4 - p1;

            //rotate so both segments are horizontal.
            Angle a = p20.Theta;
            PointPlus p10r = p10; p10r.Theta -= a;
            PointPlus p20r = p20; p20r.Theta -= a;
            PointPlus p30r = p30; p30r.Theta -= a;
            PointPlus p40r = p40; p40r.Theta -= a;
            double minX = Min(new double[] { p10r.X, p20r.X, p30r.X, p40r.X });
            double maxX = Max(new double[] { p10r.X, p20r.X, p30r.X, p40r.X });
            double y = (p30r.Y + p40r.Y) / 2;
            PointPlus pa = new PointPlus((float)minX, (float)y / 2);
            PointPlus pb = new PointPlus((float)maxX, (float)y / 2);
            pa.Theta += a;
            pb.Theta += a;
            pa = pa + p1;
            pb = pb + p1;

            Stroke theStroke = new Stroke
            {
                width = (float)y,
                p1 = pa.P,
                p2 = pb.P,
            };
            strokes.Add(theStroke);
        }
        private void ConnectStrokes(Stroke s1, Stroke s2)
        {
            Utils.FindIntersection(s1.p1, s1.p2, s2.p1, s2.p2, out Point interSection);
            if (Distance(s1.p1, interSection) < 5 && Distance(s2.p1, interSection) < 5)
            {
                s1.p1 = interSection;
                s2.p1 = interSection;
            }
            if (Distance(s1.p2, interSection) < 5 && Distance(s2.p1, interSection) < 5)
            {
                s1.p2 = interSection;
                s2.p1 = interSection;
            }
            if (Distance(s1.p1, interSection) < 5 && Distance(s2.p2, interSection) < 5)
            {
                s1.p1 = interSection;
                s2.p2 = interSection;
            }
            if (Distance(s1.p2, interSection) < 5 && Distance(s2.p2, interSection) < 5)
            {
                s1.p2 = interSection;
                s2.p2 = interSection;
            }
        }
        double Distance(Point p1, Point p2)
        {
            double dist = ((Vector)p1 - (Vector)p2).Length;
            return dist;
        }

        int strokeCount = 0;
        private void SaveThing(Stroke s)
        {
            AddStrokeToUKS(new PointPlus(s.p1), new PointPlus(s.p2));
        }
        int pCount = 0;
        //TODO add more properties to Stroke
        public Thing AddStrokeToUKS(PointPlus P1, PointPlus P2)
        {
            ModuleUKS uks = (ModuleUKS)FindModuleByType(typeof(ModuleUKS));
            if (uks is ModuleUKS UKS)
            {
                if (uks.Labeled("AbsStroke") == null) uks.AddThing("AbsStroke", "Visual");
                Thing t1 = uks.Valued(P1);
                if (t1 == null)
                    t1 = UKS.AddThing("p" + pCount++, UKS.Labeled("Point"), P1);
                Thing t2 = uks.Valued(P2);
                if (t2 == null)
                    t2 = UKS.AddThing("p" + pCount++, UKS.Labeled("Point"), P2);
                Thing newThing = UKS.AddThing("s" + strokeCount++, UKS.Labeled("AbsStroke"), null, new Thing[] { t1, t2 });
                return newThing;
            }
            return null;
        }
        Stroke GetStrokeFromThing(Thing t)
        {
            Stroke s = new Stroke();
            s.p1 = ((PointPlus)t.References[0].T.V).P;
            s.p2 = ((PointPlus)t.References[1].T.V).P;
            return s;
        }

        //find clusters of absolute strokes
        void FindClustersOfAbsoluteStrodkes()
        {

        }

        //convert cluster to relative strokes
        void ConvertClusterToRelativeStrokes()
        {
            ModuleUKS uks = (ModuleUKS)FindModuleByType(typeof(ModuleUKS));
            if (uks.Labeled("AbsStroke") == null) uks.AddThing("AbsStroke", "Visual");
            List<Thing> strokes = uks.Labeled("AbsStroke").Children;
            List<Thing> relations = uks.Labeled("Relation").Children;
            //create a tempShape
            Thing tempShape = uks.AddThing("TempShape", "Shape");
            //create relative strokes
            for (int i = 0; i < strokes.Count; i++)
            {
                uks.AddThing("ts" + strokeCount++, tempShape);
            }
            List<Thing> relStrokes = uks.Labeled("TempShape").Children;
            //foreach stroke-pair  in cluster
            for (int i = 0; i < strokes.Count; i++)
            {
                for (int j = 0; j < strokes.Count; j++)
                {
                    if (j != i)
                    {
                        //foreach relation, does pair meet relation
                        foreach (Thing tRelation in relations)
                        {
                            if (StrokesHaveRelation(strokes[i], strokes[j], tRelation))
                            {
                                //add relation in tempShape
                                relStrokes[i].AddReference(relStrokes[j]);
                                relStrokes[i].AddReference(tRelation);
                            }
                        }
                    }
                }
            }
        }

        int shapeCount = 0;
        //match against stored relative clusters (shapes)
        void MatchClustersAgainstStoredShapes()
        {
            ModuleUKS uks = (ModuleUKS)FindModuleByType(typeof(ModuleUKS));
            if (uks.Labeled("Cluster") == null) uks.AddThing("Cluster", "Shape");
            Thing tempShape = uks.Labeled("TempShape");
            List<Thing> clusters = uks.Labeled("Cluster").Children;
            //for each stored shape, calculate error from cluster
            Thing bestThing = null;
            int bestMatch = int.MinValue;
            int bestCount = -10;
            for (int i = 0; i < clusters.Count; i++)
            {
                Thing t = clusters[i];
                int matchCount = ClusterMatch(t, tempShape);
                if (matchCount > bestMatch)
                {
                    bestMatch = matchCount;
                    bestThing = t;
                    bestCount = i;
                }
            }
            //add shape if not found
            if (bestThing == null || bestMatch < 98)
            {
                tempShape.Label = "Shape" + shapeCount++;
                uks.Labeled("Cluster").AddChild(tempShape);
            }
            else
            {
                //TODO add linkage to original strokes 
                //delete the tempShape
                SetNeuronValue(null, 0, bestCount, 1);
                while (tempShape.Children.Count != 0)
                    uks.DeleteThing(tempShape.Children[0]);
                uks.DeleteThing(tempShape);
            }
        }
        int ClusterMatch(Thing storedShape, Thing tempShape)
        {
            int matchCount = 100;
            List<int> storedStrokes = GetStrokeCounts(storedShape);
            List<int> tempStrokes = GetStrokeCounts(tempShape);

            for (int i = 0; i < storedStrokes.Count && i < tempStrokes.Count; i++)
            {
                int storedCount = 0;
                int tempCount = 0;
                if (i < storedStrokes.Count) storedCount = storedStrokes[i];
                if (i < tempStrokes.Count) tempCount = tempStrokes[i];
                matchCount -= Abs(storedCount - tempCount);
            }
            return matchCount;
        }
        List<int> GetStrokeCounts(Thing t)
        {
            List<Thing> strokes = t.Children;

            int[,] relations = new int[strokes.Count, strokes.Count];
            for (int i = 0; i < strokes.Count; i++)
            {
                Thing source = strokes[i];
                int target = 0;
                for (int j = 0; j < source.References.Count; j++)
                {
                    Thing t1 = source.References[j].T;
                    if (j % 2 == 0)
                    {//dest
                        target = strokes.IndexOf(t1);
                    }
                    else
                    {//relation
                        int curVal = relations[i, target];
                        if (t1.Label == "SameAngle") curVal |= 1;
                        if (t1.Label == "SameLength") curVal |= 2;
                        relations[i, target] = curVal;
                    }
                }
            }
            List<int> counts = new List<int>();
            for (int i = 0; i < 16; i++)
                counts.Add(0);
            foreach (int i in relations)
                counts[i]++;
            return counts;
        }

        bool StrokesHaveRelation(Thing t1, Thing t2, Thing tRelation)
        {
            //Initial Relations: equal angle, equal length, 
            if (tRelation.Label == "SameAngle")
            {
                Stroke s1 = GetStrokeFromThing(t1);
                Stroke s2 = GetStrokeFromThing(t2);
                Angle diff = Math.Min(2 * PI - Abs(s1.angle - s2.angle), Abs(s1.angle - s2.angle));
                if (diff < Utils.Rad(15))
                    return true;
            }
            if (tRelation.Label == "RightAngle")
            {
                Stroke s1 = GetStrokeFromThing(t1);
                Stroke s2 = GetStrokeFromThing(t2);
                if (Abs(s1.angle - (s2.angle + PI / 4)) < Utils.Rad(5)) return true;
                if (Abs(s1.angle - (s2.angle - PI / 4)) < Utils.Rad(5)) return true;
            }
            if (tRelation.Label == "SameLength")
            {
                Stroke s1 = GetStrokeFromThing(t1);
                Stroke s2 = GetStrokeFromThing(t2);
                if (Abs(s1.length - s2.length) <= 10)
                    return true;
            }
            return false;
        }

        void DeletePreviousStrokes()
        {
            ModuleUKS uks = (ModuleUKS)FindModuleByType(typeof(ModuleUKS));
            if (uks.Labeled("AbsStroke") == null) uks.AddThing("AbsStroke", "Visual");
            List<Thing> strokes = uks.Labeled("AbsStroke").Children;
            while (strokes.Count != 0)
            {
                uks.DeleteThing(strokes[0]);
            }
        }
        Thing AddRelationship(Thing t1, Thing t2, Thing relation)
        {
            return t1;
        }

        List<Segment0> FindPrimaryLines(Boundary b)
        {
            string s = b.theString;
            Point curPt = b.p1;
            List<Segment0> theLines = new List<Segment0>();
            for (int i = 0; i < s.Length; i++)
            {
                ModuleBoundary1.GetPositionOfLinePoint((int)b.p1.X, (int)b.p1.Y, s, i, out int newX, out int newY);
                curPt = new Point(newX, newY);
                for (int j = i + 2; j <= s.Length; j++)
                {
                    string ss = s.Substring(i, j - i);
                    if (!ModuleBoundary1.IsStraightLine(ss) || j == s.Length)
                    {
                        string lineString = ss;
                        if (j < s.Length)
                            lineString = ss.Substring(0, ss.Length - 1);
                        ModuleBoundary1.GetPositionOfLinePoint((int)curPt.X, (int)curPt.Y, lineString, 100, out newX, out newY);
                        Point newPt = new Point(newX, newY);
                        if (lineString.Length > 2)
                        {
                            theLines.Add(new Segment0 { p1 = curPt, p2 = newPt });
                            curPt = newPt;
                            i = j - 2;
                        }
                        break;
                    }
                }
            }
            return theLines;
        }


        void GetFurthestPoints(Point[] pts)
        {
            Point p1 = pts[0];
            Point p2 = pts[1];
            Point p3 = pts[2];
            Point p4 = pts[3];
            Point pInvalid = new Point(double.NaN, double.NaN);
            //find the furthest points
            float bestDist = 0;
            for (int i = 0; i < pts.Length; i++)
                for (int j = i + 1; j < pts.Length; j++)
                {
                    double dist = ((Vector)pts[i] - (Vector)pts[j]).Length;
                    if (dist > bestDist)
                    {
                        bestDist = (float)dist;
                        p1 = pts[i];
                        p2 = pts[j];
                        p3 = pInvalid;
                        for (int m = 0; m < pts.Length; m++)
                        {
                            if (m != i && m != j && p3.Equals(pInvalid)) { p3 = pts[m]; continue; }
                            if (m != i && m != j && !p3.Equals(pInvalid)) p4 = pts[m];
                        }
                    }
                }
            //now, order the array: furthest, furthest, nearest, nearest
            pts[0] = p1;
            pts[1] = p2;
            pts[2] = p3;
            pts[3] = p4;
        }


        Point GetMin(Point[] points)
        {
            int count = points.Length;
            double[] x = new double[count];
            for (int i = 0; i < count; i++)
                x[i] = points[i].X;
            double[] y = new double[count];
            for (int i = 0; i < count; i++)
                y[i] = points[i].Y;
            return new Point(Min(x), Min(y));
        }
        Point GetMax(Point[] points)
        {
            int count = points.Length;
            double[] x = new double[count];
            for (int i = 0; i < count; i++)
                x[i] = points[i].X;
            double[] y = new double[count];
            for (int i = 0; i < count; i++)
                y[i] = points[i].Y;
            return new Point(Max(x), Max(y));
        }
        double Min(double[] numbers)
        {
            double min = double.MaxValue;
            for (int i = 0; i < numbers.Length; i++)
                if (numbers[i] < min) min = numbers[i];
            return min;
        }
        double Max(double[] numbers)
        {
            double max = -double.MaxValue;
            for (int i = 0; i < numbers.Length; i++)
                if (numbers[i] > max) max = numbers[i];
            return max;
        }


        //or when the engine restart button is pressed
        public override void Initialize()
        {
            foreach (Neuron n in na.Neurons1)
            {
                n.SetValue(0);
            }
        }
    }
}
