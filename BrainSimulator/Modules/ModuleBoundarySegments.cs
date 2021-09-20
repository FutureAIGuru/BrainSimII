//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleBoundarySegments : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleBoundarySegments()
        {
            minHeight = 2;
            maxHeight = 500;
            minWidth = 2;
            maxWidth = 500;
        }

        int bndryPtCt = 0;
        int bndryCt = 0;


        //TODO: Add color because down-the-road, only similar-colored boundary arcs can be related
        public class Arc
        {
            public Point p1;
            public Point p2;
            //public Point p3; //we'll use this as the arc midpoint when arcs are curved

            public float Length
            {
                get => (float)((Vector)(p1 - (Vector)p2)).Length;
            }
            public Angle Angle
            {
                get
                {
                    Angle angle = Atan2(p2.Y - p1.Y, p2.X - p1.X);
                    if (angle < 0) angle += PI;
                    return angle;
                    ;
                }
            }
            public Point MidPoint { get => new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2); }
            public Point MidPointI { get => new Point(Round((p1.X + p2.X) / 2), Round((p1.Y + p2.Y) / 2)); }


            public override string ToString()
            {
                return ("(" + p1.X + "," + p1.Y + ") (" + p2.X + "," + p2.Y + ")" + Angle.ToString() + " " + Length.ToString());
            }
        }
        [XmlIgnore]
        public List<Arc> boundaries = new List<Arc>();
        [XmlIgnore]
        public List<Arc> horizBoundaries = new List<Arc>();
        [XmlIgnore]
        public List<Arc> vertBoundaries = new List<Arc>();
        [XmlIgnore]
        public List<Arc> tList1 = new List<Arc>();
        [XmlIgnore]
        public List<Arc> tList2 = new List<Arc>();
        [XmlIgnore]
        public List<Arc> tList3 = new List<Arc>();
        [XmlIgnore]
        public List<Arc> segments = new List<Arc>();

        float angleStep = 90f;
        [XmlIgnore]

        public List<Point> favoredPoints = new List<Point>();
        ModuleView naSource;

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
                     //return;
            naSource = theNeuronArray.FindModuleByLabel("Boundary");
            if (naSource == null) return;

            DeleteBoundariesFromUKS();
            boundaries.Clear();

            FindSegments();

            //JoinHorizSegmentsWhichMissBy1();

            //JoinColinearSegments(boundaries);
            //JoinEndsWhichMissBy1(-1);

            //delete zero-length segments
            for (int i = 0; i < boundaries.Count; i++)
            {
                if (boundaries[i].Length == 0)
                {
                    boundaries.RemoveAt(i);
                    i--;
                }
            }

            AddBoundarySegmentsToUKS();

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();
        }
        private void DeleteBoundariesFromUKS()
        {

        }

        void AddBoundarySegmentsToUKS()
        {
            naSource = theNeuronArray.FindModuleByLabel("UKS");
            if (naSource == null) return;
            ModuleUKS uks = (ModuleUKS)naSource.TheModule;
            Thing segmentParent = uks.Labeled("Segment");
            if (segmentParent == null)
                segmentParent = uks.AddThing("Segment", "Shape");
            Thing pointParent = uks.Labeled("ModelThing"); //TODO: this modelthing is a 2D and we're now in 3D
            if (pointParent == null) return;

            uks.DeleteAllChilden(segmentParent);
            //uks.DeleteAllChilden(pointParent);

            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].Length > 0)
                {
                    Thing pt1 = uks.Valued((PointPlus)segments[i].p1, segmentParent.Children);
                    if (pt1 == null)
                    {
                        pt1 = uks.AddThing("BndryPt" + bndryPtCt++, new Thing[] { segmentParent /*,pointParent*/});
                        pt1.V = (PointPlus)segments[i].p1;
                    }
                    Thing pt2 = uks.Valued((PointPlus)segments[i].p2, segmentParent.Children);
                    if (pt2 == null)
                    {
                        pt2 = uks.AddThing("BndryPt" + bndryPtCt++, new Thing[] { segmentParent /*,pointParent*/ });
                        pt2.V = (PointPlus)segments[i].p2;
                    }
                    Thing seg = uks.AddThing("BndrySeg" + bndryCt++, "Segment");
                    seg.AddChild(pt1);
                    seg.AddChild(pt2);
                }
            }
        }


        //HERE BEGINS AN EXPERIMENT in improved boundary-finding by 
        //finding line segments which cross nearest the most boundary pixels
        Point[] CreateRectangle(Point pt, Angle a, float length, float width)
        {
            //points layout
            //     1          3
            //  0                5
            //     2          4
            PointPlus pt0 = pt;
            PointPlus pt1 = new PointPlus(width, width);
            pt1.Theta += a;
            pt1 += pt0;
            PointPlus pt2 = new PointPlus(width, -width);
            pt2.Theta += a;
            pt2 += pt0;
            PointPlus pt3 = new PointPlus(length - 2 * width, 0f);
            pt3.Theta += a;
            pt3 += pt1;
            PointPlus pt4 = new PointPlus(length - 2 * width, 0f);
            pt4.Theta += a;
            pt4 += pt2;
            PointPlus pt5 = new PointPlus(length, 0f);
            pt5.Theta += a;
            pt5 += pt0;

            Point[] corners = new Point[6];
            corners[0] = pt0;
            corners[1] = pt1;
            corners[2] = pt3;
            corners[3] = pt5;
            corners[4] = pt4;
            corners[5] = pt2;
            return corners;
        }
        public class AngleVal
        {
            public Angle a; public float len; public int count;
            public override string ToString()
            {
                return a.ToString() + len + ":" + count;
            }
        }
        void FindSegments()
        {
            //read the neurons in the bit map and get the boundary and corner points
            segments.Clear();
            favoredPoints.Clear();
            List<Point> firingPoints = new List<Point>();
            List<Point> pointsToSearch = new List<Point>();
            float rectWidth = 0.4f;
            for (int j = 0; j < naSource.Height; j++)
            {
                for (int i = 0; i < naSource.Width; i++)
                {
                    int index = naSource.GetNeuronIndexAt(i, j);
                    float lastCharge = MainWindow.theNeuronArray.GetNeuronLastCharge(index);
                    if (lastCharge > 0.1f)
                    {
                        firingPoints.Add(new Point(i, j));
                        pointsToSearch.Add(new Point(i, j));
                        if (lastCharge == .99f)
                            favoredPoints.Add(new Point(i, j));
                    }
                }
            }
            for (int k = 0; k < favoredPoints.Count; k++)
                for (int l = k + 1; l < favoredPoints.Count; l++)
                {
                    PointPlus p1 = favoredPoints[k];
                    PointPlus p2 = favoredPoints[l];
                    float estDist = (p1 - p2).R;
                    float actDist = NeuronsInRectangle(firingPoints, p1, (p2 - p1).Theta, 1, out int hitCount);
                    float diff = Abs(estDist - actDist);
                    if (diff < 4)
                    {
                        segments.Add(new Arc { p1 = p1, p2 = p2 });
                    }
                }

            return;

#pragma warning disable 162
            //work from the corner points to find all the segments
            for (int k = 0; k < favoredPoints.Count; k++)
            {
                Point start = favoredPoints[k];
                pointsToSearch.Remove(start);
                Angle angleStep = Angle.FromDegrees(1f);
                List<AngleVal> angleVals = new List<AngleVal>();
                for (Angle a = Angle.FromDegrees(-5); a < Angle.FromDegrees(180); a += angleStep)
                {
                    //get the longest rectangle from this neuron and angle
                    float bestLength = NeuronsInRectangle(firingPoints, start, a, rectWidth, out int hitCount);
                    angleVals.Add(new AngleVal { a = a, len = bestLength - 1, count = hitCount, });
                }
                //find local maxima and add to segments
                float curLength = 0;
                int startOfRange = 0;
                for (int i = 0; i < angleVals.Count; i++)
                {
                    Angle x = Atan2(1, 5);
                    if (angleVals[i].len > curLength)
                    {
                        //increasing value
                        startOfRange = i;
                        curLength = angleVals[i].len;
                    }
                    else if (angleVals[i].len < curLength - 2) //the -2 eliminates some noise
                    {
                        //value has started decreasing...find the midpoint of the maximum
                        if (startOfRange == -1) continue;
                        int centerOfRange = (int)Round((startOfRange + i - 1) / 2f);
                        startOfRange = -1;
                        PointPlus p1 = start;
                        PointPlus p2 = new PointPlus(angleVals[centerOfRange].len, angleVals[centerOfRange].a);
                        p2 += p1;
                        //if (Abs((p2 - p1).R) > 2)
                        {
                            //p2 is the calculated point... is it near a favored pt?
                            for (int j = 0; j < favoredPoints.Count; j++)
                            {
                                if ((((PointPlus)favoredPoints[j]) - p2).R < 2)
                                {
                                    p2 = favoredPoints[j];
                                    break;
                                }
                            }

                            segments.Add(new Arc { p1 = p1, p2 = p2 });

                            //remove points from pointsToSearch
                            Point[] startingRectangle = CreateRectangle(start, angleVals[centerOfRange].a, (float)angleVals[centerOfRange].len, rectWidth + 2);
                            for (int j = 0; j < pointsToSearch.Count; j++)
                            {
                                if (Utils.IsPointInPolygon(startingRectangle, pointsToSearch[j]))
                                {
                                    pointsToSearch.RemoveAt(j);
                                    j--;
                                }
                            }
                        }
                        curLength = angleVals[i].len;
                    }
                }

            }

            return;
#pragma warning restore 162

            //THIS METHOD builds every segment at every location and then merges them into groups
            //segments.Clear();
            ////try every boundary
            //for (int i = 0; i < horizBoundaries.Count; i++)
            //{
            //    float curLength = 1;

            //    Angle a = new Angle(0);
            //    //try every point in every boundary
            //    for (int j = (int)horizBoundaries[i].p1.X; j <= (int)horizBoundaries[i].p2.X; j++)
            //    {
            //        Arc bestArc = new Arc(); 
            //        float bestError = 0;
            //        Point start = new Point(j, horizBoundaries[i].p1.Y);
            //        //try most angles at every point
            //        for (a = Angle.FromDegrees(0); a < Angle.FromDegrees(180); a += Angle.FromDegrees(15))
            //        {
            //            FindSegmentThroughPoint(start, a, out Point p1, out Point p2);

            //            float length = (float)(p1 - p2).Length;
            //            float error = FindSegmentError(p1, p2);
            //            if (length > curLength + 1 || error < bestError && length > curLength-1)
            //            {
            //                //float error = FindSegmentError(p1, p2);
            //                bestArc.p1 = p1;
            //                bestArc.p2 = p2;
            //                curLength = length;
            //                bestError = error;
            //            }
            //            if (length < curLength && bestArc.Length > 3)
            //            {
            //                AddSegmentToList(bestArc);
            //                bestArc = new Arc();
            //                bestError = 0;
            //            }
            //            if (length < curLength && length > 0)
            //                curLength = length;
            //        }
            //    }
            //}
            //GroupSegments();
        }

        private float NeuronsInRectangle(List<Point> firingPoints, Point start, Angle a, float rectWidth, out int bestFiringCount)
        {
            //find the length for this angle
            float length;
            float bestLength = 0;
            bestFiringCount = 0;
            float lengthStep = (float)Abs(1 / Sin(a));
            if (lengthStep > 1.414) lengthStep = (float)Abs(1 / Cos(a));
            lengthStep = 1f;
            bestLength = 0;
            for (length = 1; length < 100; length += lengthStep)
            {
                Point[] startingRectangle = CreateRectangle(start, a, (float)length, rectWidth);
                int firingCount = 0;
                for (int i = 0; i < firingPoints.Count; i++)
                {
                    if (Utils.IsPointInPolygon(startingRectangle, firingPoints[i]))
                    {
                        firingCount++;
                    }
                }
                for (int i = 0; i < favoredPoints.Count; i++)
                {
                    if (Utils.IsPointInPolygon(startingRectangle, favoredPoints[i]))
                    {
                        firingCount += 5;
                    }
                }
                if (firingCount <= bestFiringCount)
                {
                    if (length > bestLength + 2) break;
                }
                else //new firing count is greater
                    bestLength = length;
                bestFiringCount = firingCount;
            }

            return bestLength;
        }

        void AddSegmentToList(Arc theArc)
        {
            if (theArc == null) return;
            if (theArc.Length == 0) return;
            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].p1 == theArc.p1 && segments[i].p2 == theArc.p2) return;
            }
            segments.Add(theArc);
        }

        [XmlIgnore]
        public List<List<Arc>> clusters = new List<List<Arc>>();
        void GroupSegments()
        {
            clusters.Clear();
            List<Arc> similarArcs = new List<Arc>();
            for (Angle a = 0; a < Angle.FromDegrees(180); a += Angle.FromDegrees(15))
            {
                //find group of segments with similar angles
                similarArcs.Clear();
                for (int i = 0; i < segments.Count; i++)
                {
                    if (Abs(segments[i].Angle - a) < Angle.FromDegrees(15f))
                    {
                        similarArcs.Add(segments[i]);
                    }
                }

                //within this set, find subsets which overlap/intersect
                for (int i = 0; i < similarArcs.Count; i++)
                {
                    List<Arc> cluster = new List<Arc>();
                    clusters.Add(cluster);
                    cluster.Add(similarArcs[i]); //find mesh of all segments which intersect with this one
                    bool itemAdded = true;
                    while (itemAdded)
                    {
                        itemAdded = false;
                        for (int j = 0; j < similarArcs.Count; j++)
                        {
                            for (int k = 0; k < cluster.Count; k++)
                            {
                                //replace this with closeness measure
                                //                                if (Utils.SegmentsIntersect(similarArcs[j].p1, similarArcs[j].p2, cluster[k].p1, cluster[k].p2))
                                if (Utils.DistanceBetweenTwoSegments(similarArcs[j].p1, similarArcs[j].p2, cluster[k].p1, cluster[k].p2) < 2)
                                {
                                    if (!cluster.Contains(similarArcs[j]))
                                    {
                                        cluster.Add(similarArcs[j]);
                                        similarArcs.Remove(similarArcs[j]);
                                        j--;
                                        itemAdded = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //process the clusters in order from most complex to least
            for (int i = 0; i < clusters.Count; i++)
            {
                Angle avgSlope = clusters[i].Average(x => x.Angle);
                double maxX = clusters[i].Max(x => x.p1.X);
                maxX = Max(maxX, clusters[i].Max(x => x.p2.X));
                double maxY = clusters[i].Max(x => x.p1.Y);
                maxY = Max(maxY, clusters[i].Max(x => x.p2.Y));
                double minX = clusters[i].Min(x => x.p1.X);
                minX = Min(minX, clusters[i].Min(x => x.p2.X));
                double minY = clusters[i].Min(x => x.p1.Y);
                minY = Min(minY, clusters[i].Min(x => x.p2.Y));

                Point p1, p2;
                if (avgSlope < Angle.FromDegrees(90))
                {
                    p1 = new Point(minX, minY);
                    p2 = new Point(maxX, maxY);
                }
                else
                {
                    p1 = new Point(minX, maxY);
                    p2 = new Point(maxX, minY);
                }
                clusters[i].Clear();
                clusters[i].Add(new Arc { p1 = p1, p2 = p2 });
            }
            clusters = clusters.OrderBy(x => x[0].Length).ToList(); ;
        }



        void FindSegmentThroughPoint(Point pt, Angle a, out Point p1, out Point p2)
        {
            float deltaX = 1 / (float)Math.Tan(a);
            float deltaY = 1;
            if (Abs(deltaX) > 1)
            {
                deltaY = 1 / deltaX;
                deltaX = 1;
            }
            float curX = (float)(pt.X);
            float curY = (float)pt.Y;
            float sx = curX;
            float sy = curY;
            float ex = curX;
            float ey = curY;

            Neuron n1 = naSource.GetNeuronAt((int)Round(curX), (int)Round(curY));
            while (n1 != null && n1.lastCharge != 0)
            {
                ex = curX;
                ey = curY;
                curX += deltaX;
                curY += deltaY;
                n1 = naSource.GetNeuronAt((int)Round(curX), (int)Round(curY));
            }
            curX = (float)(pt.X);
            curY = (float)pt.Y;
            deltaX = -deltaX;
            deltaY = -deltaY;

            n1 = naSource.GetNeuronAt((int)Round(curX), (int)Round(curY));
            while (n1 != null && n1.lastCharge != 0)
            {
                sx = curX;
                sy = curY;
                curX += deltaX;
                curY += deltaY;
                n1 = naSource.GetNeuronAt((int)Round(curX), (int)Round(curY));
            }

            p1 = new Point(sx, sy);
            p2 = new Point(ex, ey);
        }

        float FindSegmentError(Point p1, Point p2)
        {
            float error = 0;
            float curX = (float)p1.X;
            float curY = (float)p1.Y;
            float deltaX = (float)(p2.X - p1.X);
            float deltaY = (float)(p2.Y - p1.Y);
            if (deltaX == 0 && deltaY == 0) return 0;

            if (Abs(deltaX) > Abs(deltaY))
            {
                deltaY /= deltaX;
                deltaX = 1;
                Neuron n1 = naSource.GetNeuronAt((int)Round(curX), (int)Round(curY));
                Neuron n2 = naSource.GetNeuronAt((int)Round(curX), (int)Round(curY + Ceiling(deltaY)));
                while (n1 != null && n2 != null && curY <= p2.Y)
                {
                    if (n1.LastCharge != 0 && n2.LastCharge == 0)
                    {
                        error += Abs(curY - (float)Round(curY));
                    }
                    else if (n1.LastCharge == 0 && n2.LastCharge != 0)
                    {
                        error += Abs(curY - (float)Round(curY + 1));
                    }
                    curX += deltaX;
                    curY += deltaY;
                    n1 = naSource.GetNeuronAt((int)Round(curX), (int)Round(curY));
                    n2 = naSource.GetNeuronAt((int)Round(curX), (int)Round(curY + Ceiling(deltaY)));
                }
            }
            else
            {
                //deltaX = 1 / deltaY;
                //deltaY = 1;
                Neuron n1 = naSource.GetNeuronAt((int)Round(curX), (int)Round(curY));
                Neuron n2 = naSource.GetNeuronAt((int)Round(curX + Ceiling(deltaX)), (int)Round(curY));
                while (n1 != null && n2 != null && curX <= p2.X)
                {
                    if (n1.LastCharge != 0 && n2.LastCharge == 0)
                    {
                        error += Abs(curX - (float)Round(curX));
                    }
                    else if (n1.LastCharge == 0 && n2.LastCharge != 0)
                    {
                        error += Abs(curX - (float)Round(curX + 1));
                    }
                    curX += deltaX;
                    curY += deltaY;
                    n1 = naSource.GetNeuronAt((int)Round(curX), (int)Round(curY));
                    n2 = naSource.GetNeuronAt((int)Round(curX + Ceiling(deltaX)), (int)Round(curY));
                }
            }
            return error;
        }

        //do these two ranges overlap or miss by 1? (x0,x1) (x2,x3)
        bool InRange(double x0, double x1, double x2, double x3)
        {
            if (x0 > x2 - 1 && x0 < x3 + 1) return true;
            if (x1 > x2 - 1 && x1 < x3 + 1) return true;
            if (x2 > x0 - 1 && x2 < x1 + 1) return true;
            if (x3 > x0 - 1 && x3 < x1 + 1) return true;
            return false;
        }

        //given two points of a segment, how closely does it match pixels of the boundary
        //how much longer 
        float TraceLine(Point p1, Angle a)
        {

            float error = 0;
            return error;
        }

        //TODO These should migrate to the base of NA
        float GetNeuronAndValue(int i, int j)
        {
            return GetNeuronAndValue(i, j, out Neuron n);
        }
        float GetNeuronAndValue(int i, int j, out Neuron n)
        {
            n = null;
            if (naSource.GetNeuronAt(i, j) is Neuron n0)
            {
                n = n0;
                return n0.LastCharge;
            }
            return -1;
        }


        void CloneList(List<Arc> tList, List<Arc> boundaryList)
        {
            tList.Clear();
            foreach (Arc a in boundaryList)
            {
                tList.Add(new Arc { p1 = PointCopy(a.p1), p2 = PointCopy(a.p2) });
            }
        }

        void JoinHorizSegmentsWhichMissBy1()
        {
            CloneList(tList3, horizBoundaries);
            for (int i = 0; i < horizBoundaries.Count; i++)
            {
                for (int j = i + 1; j < horizBoundaries.Count; j++)
                {
                    MergeArcs(i, j);
                }
            }
            CloneList(boundaries, tList3);
        }



        private Point PointCopy(Point p)
        {
            return new Point { X = p.X, Y = p.Y };
        }
        private void MergeArcs(int i, int j)
        {
            Arc a1 = horizBoundaries[i];
            Arc a2 = horizBoundaries[j];

            if (a2.p1.X > a1.p2.X + 1 || a2.p2.X < a1.p1.X - 1) return;
            if (a2.p1.Y > a1.p1.Y + 1) return;


            if (a1.p1.X == 25 && a1.p1.Y == 35)
            { }

            //special case of single point over seg which extends both directions
            if (
                a1.p2.Y + 1 == a2.p1.Y &&
                (a1.p1.X > a2.p1.X &&
                 a1.p1.X < a2.p2.X))
            {
                tList3[j].p1 = PointCopy(a1.p2);
                tList3.Add(new Arc { p1 = PointCopy(a1.p1), p2 = PointCopy(a2.p1) });
            }

            //special case where a line has been removed (at the bottom of a v)
            else if (tList3[i].p1.Y != a1.p1.Y && tList3[i].p2.Y != a1.p1.Y)
            {
                //tList3.Add(new Arc { p1 = PointCopy(a2.p1), p2 = PointCopy(tList3[i].p2) });
                tList3.Add(new Arc { p1 = PointCopy(tList3[i].p2), p2 = PointCopy(a2.p1) });
                tList3[i].p2 = a2.p1;
            }

            ////special case of single point under seg which extends both directions
            //else if (
            //    a1.p2.Y + 1 == a2.p1.Y &&
            //    (a1.p1.X > a2.p1.X &&
            //     a1.p2.X < a2.p2.X))
            //{
            //    //tList3.Add(new Arc { p1 = PointCopy(a2.p1), p2 = PointCopy(tList3[i].p2) });
            //    tList3[i].p2 = PointCopy(a2.p1);
            //}

            //segment going down/left
            else if (!favoredPoints.Contains(a2.p2) &&
                a1.p1.Y + 1 == a2.p2.Y &&
                (a1.p1.X == a2.p2.X ||
                a1.p1.X - 1 == a2.p2.X))
            {
                tList3[j].p2 = PointCopy(a1.p1);
            }

            //segment going down/right
            else if (!favoredPoints.Contains(a2.p1) &&
                a1.p2.Y + 1 == a2.p1.Y &&
                (a1.p2.X == a2.p1.X ||
                a1.p2.X + 1 == a2.p1.X))
            {
                tList3[j].p1 = PointCopy(a1.p2);
            }

        }

        void SaveJoinHorizSegmentsWhichMissBy1(int vOffset) //vOffset = 1: down/right   -1: up/right
        {
            //delete onesie-twosie
            for (int i = 0; i < horizBoundaries.Count; i++)
            {
                if (horizBoundaries[i].Length < 3)
                {
                    horizBoundaries.RemoveAt(i);
                    i--;
                }
            }
            //you have to do this twice to keep segments from being broken on occasional 1-pixel segments
            for (int i = 0; i < horizBoundaries.Count; i++)
                for (int j = i + 1; j < horizBoundaries.Count; j++)
                {
                    if (((Vector)horizBoundaries[i].p2 - (Vector)horizBoundaries[j].p1).Length < 2)
                    {
                        Join2Segments(horizBoundaries[i], horizBoundaries[j]);
                        horizBoundaries.Remove(horizBoundaries[j]);
                        j--;
                        if (i >= horizBoundaries.Count) i--;
                    }
                }
        }
        void JoinVertSegmentsWhichMissBy1(int vOffset) //vOffset = 1: down/right   -1: down/left
        {
            for (int i = 0; i < vertBoundaries.Count; i++)
            {
                for (int j = 0; j < vertBoundaries.Count; j++)
                {

                    if (j != i
                        && (vertBoundaries[i].p2.X - vertBoundaries[j].p1.X == vOffset))
                    {
                        if (vertBoundaries[i].p2.Y == vertBoundaries[j].p1.Y ||
                            vertBoundaries[i].p2.Y + 1 == vertBoundaries[j].p1.Y
                            )
                        {
                            //only consider zero-length lines if they are favored or on an angle
                            if ((IsPointFavored(vertBoundaries[i].p2) == 5 || vertBoundaries[i].p1 != vertBoundaries[i].p2) &&
                                (IsPointFavored(vertBoundaries[j].p2) == 5 || vertBoundaries[j].p1 != vertBoundaries[j].p2) ||
                                vertBoundaries[i].p2.Y != vertBoundaries[j].p1.Y)
                            {
                                //vertBoundaries[i].p2 = vertBoundaries[j].p1;
                                Join2Segments(vertBoundaries[i], vertBoundaries[j]);
                                vertBoundaries.Remove(vertBoundaries[j]);
                                j--;
                                if (i >= vertBoundaries.Count) i--;
                            }
                        }
                    }
                }
            }
            //delete onesie-twosie
            for (int i = 0; i < vertBoundaries.Count; i++)
            {
                if (vertBoundaries[i].Length < 3)
                {
                    vertBoundaries.RemoveAt(i);
                    i--;
                }
            }
            //delete segments with wrong slope
            for (int i = 0; i < vertBoundaries.Count; i++)
            {
                if (vOffset == 1 && vertBoundaries[i].Angle < 90)
                {
                    vertBoundaries.RemoveAt(i);
                    i--;
                }
            }
            //you have to do this twice to keep segments from being broken on occasional 1-pixel segments
            for (int i = 0; i < vertBoundaries.Count; i++)
                for (int j = i + 1; j < vertBoundaries.Count; j++)
                {
                    if (((Vector)vertBoundaries[i].p2 - (Vector)vertBoundaries[j].p1).Length < 2)
                    {
                        Join2Segments(vertBoundaries[i], vertBoundaries[j]);
                        vertBoundaries.Remove(vertBoundaries[j]);
                        j--;
                    }
                }
        }

        void RemoveDuplicateSegments()
        {
            for (int i = 0; i < boundaries.Count; i++)
            {
                for (int j = i + 1; j < boundaries.Count; j++)
                {
                    if ((boundaries[i].p1 == boundaries[j].p1 && boundaries[i].p2 == boundaries[j].p2) ||
                        (boundaries[i].p2 == boundaries[j].p1 && boundaries[i].p1 == boundaries[j].p2))
                    {
                        boundaries.RemoveAt(j);
                        i--;
                        break;
                    }
                }
            }
        }

        void JoinColinearSegments(List<Arc> boundaries)
        {
            for (int i = 0; i < boundaries.Count; i++)
            {
                if (i == 3)
                { }
                float l1 = boundaries[i].Length;
                if (l1 == 0)
                {
                    boundaries.RemoveAt(i);
                    i--;
                    continue;
                }
                for (int j = i + 1; j < boundaries.Count; j++)
                {
                    //do they have similar angles?
                    float l2 = boundaries[j].Length;
                    float angleLimit = 46;
                    if (l2 > 2 && l1 > 2) angleLimit = 31;
                    if (l2 > 3 && l1 > 3) angleLimit = 16;
                    float theAngleDegrees = Math.Abs(boundaries[i].Angle.ToDegrees() - boundaries[j].Angle.ToDegrees());
                    if (theAngleDegrees > 90) theAngleDegrees = 180 - theAngleDegrees;
                    if (theAngleDegrees <= angleLimit)
                    {
                        //do they have common point?
                        if (boundaries[i].p1.Equals(boundaries[j].p1) ||
                            boundaries[i].p1.Equals(boundaries[j].p2) ||
                            boundaries[i].p2.Equals(boundaries[j].p1) ||
                            boundaries[i].p2.Equals(boundaries[j].p2))
                        {
                            Arc a = boundaries[i];
                            Join2Segments(a, boundaries[j]);
                            boundaries[i] = a;
                            boundaries.RemoveAt(j);
                            i--;
                            break;
                        }
                    }
                }
            }
        }
        void Join2Segments(Arc a1, Arc a2)
        {
            //find the pair of endpoints which make the longest result
            //but never change a favored point
            if (a1.p2.Y == 58)
            { }
            Point[] pts = new Point[] { a1.p1, a1.p2, a2.p1, a2.p2 };
            List<double> lengths = new List<double>();
            for (int i = 0; i < pts.Length; i++)
                for (int j = 0; j < pts.Length; j++)
                {
                    if (i == j)
                        lengths.Add(-1);
                    else
                        lengths.Add(((Vector)pts[i] - (Vector)pts[j]).Length);
                }
            for (int i = 0; i < lengths.Count; i++)
            {
                if (lengths[i] > 0)
                {
                    if (favoredPoints.Contains(pts[i % pts.Length]))
                        lengths[i] += 100;
                    if (favoredPoints.Contains(pts[i / pts.Length]))
                        lengths[i] += 100;
                }
            }
            int indexOfLongest = -1;
            indexOfLongest = lengths.IndexOf(lengths.Max());
            a1.p2 = pts[indexOfLongest % pts.Length];
            a1.p1 = pts[indexOfLongest / pts.Length];
        }


        void JoinEndsWhichMissBy1(float angle)
        {
            //return;

            //first check the orthogonals, then the diagonals
            for (float distLimit = 1.1f; distLimit < 1.6f; distLimit += 0.4f)
            {
                //CloneList();
                for (int i = 0; i < boundaries.Count; i++)
                {
                    if (angle == -1 || boundaries[i].Angle.ToDegrees() == angle)
                    {
                        for (int j = i + 1; j < boundaries.Count; j++)
                        {
                            //do they have similar angles?
                            if (angle == -1 || boundaries[j].Angle.ToDegrees() == angle ||
                                Math.Abs(boundaries[i].Angle.ToDegrees() - boundaries[j].Angle.ToDegrees()) < 45)
                            {
                                double dist = ((Vector)boundaries[i].p1 - (Vector)boundaries[j].p1).Length;
                                if (dist > 0 && dist < distLimit)
                                {
                                    if (favoredPoints.Contains(boundaries[i].p1))
                                        boundaries[j].p1 = boundaries[i].p1;
                                    else
                                        boundaries[i].p1 = boundaries[j].p1;
                                    break;
                                }
                                dist = ((Vector)boundaries[i].p1 - (Vector)boundaries[j].p2).Length;
                                if (dist > 0 && dist < distLimit)
                                {
                                    if (favoredPoints.Contains(boundaries[i].p1))
                                        boundaries[j].p2 = boundaries[i].p1;
                                    else
                                        boundaries[i].p1 = boundaries[j].p2;
                                    break;
                                }
                                dist = ((Vector)boundaries[i].p2 - (Vector)boundaries[j].p1).Length;
                                if (dist > 0 && dist < distLimit)
                                {
                                    if (favoredPoints.Contains(boundaries[i].p2))
                                        boundaries[j].p1 = boundaries[i].p2;
                                    else
                                        boundaries[i].p2 = boundaries[j].p1;
                                    break;
                                }
                                dist = ((Vector)boundaries[i].p2 - (Vector)boundaries[j].p2).Length;
                                if (dist > 0 && dist < distLimit)
                                {
                                    if (favoredPoints.Contains(boundaries[i].p2))
                                        boundaries[j].p2 = boundaries[i].p2;
                                    else
                                        boundaries[i].p2 = boundaries[j].p2;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        void FindFavoredPoints()
        {
            favoredPoints.Clear();
            for (int i = 0; i < naSource.Width; i++)
            {
                for (int j = 0; j < naSource.Height; j++)
                {
                    if (IsPointFavored(new Point(i, j)) >= 5)
                        favoredPoints.Add(new Point(i, j));
                }
            }
            //MatchPatterns(); //find more complex corners
        }


        //0 must be 0, 1 must be 1, -1 must be 1 and adds to favoredpoints on match, 2 don'tcare
        float[,,] matchPatterns = new float[,,]
        {
         {{ 0, 0, 0, 0 },
          { 0, 0,-1, 1 },
          { 0, 1, 2, 0 },
          { 0, 2, 2, 0 } },
         {{2, 1, 2, 0 },
          { 0, 0,-1, 0 },
          { 0, 0, 1, 0 },
          { 2, 1, 2, 0 } },
        };

        void MatchPatterns()
        {
            for (int i = 0; i < naSource.Width - 1; i++)
            {
                for (int j = 0; j < naSource.Height - 1; j++)
                {
                    for (int k = 0; k < matchPatterns.GetLength(0); k += 2)
                    {
                        for (int orientation = 0; orientation < 4; orientation++)
                        {
                            for (int i1 = 0; i1 < 4; i1++)
                            {
                                for (int j1 = 0; j1 < 4; j1++)
                                {
                                    float arrayVal = 0;
                                    arrayVal = GetArrayValue(k, orientation, i1, j1, arrayVal);
                                    if (arrayVal != 2)
                                    {
                                        if (GetNeuronAndValue(i1 + i, j1 + j) != Math.Abs(arrayVal))
                                            goto noMatch;
                                    }
                                }
                            }
                            //match
                            for (int i1 = 0; i1 < 4; i1++)
                                for (int j1 = 0; j1 < 4; j1++)
                                {
                                    float arrayVal = 0;
                                    arrayVal = GetArrayValue(k, orientation, i1, j1, arrayVal);
                                    if (arrayVal == -1)
                                        favoredPoints.Add(new Point(i + i1, j + j1));
                                }
                            continue;
                        //pattern matched
                        noMatch: continue;
                        }
                    }
                }
            }
        }

        private float GetArrayValue(int k, int orientation, int i1, int j1, float arrayVal)
        {
            switch (orientation)
            {
                case 0:
                    arrayVal = matchPatterns[k, j1, i1];
                    break;
                case 1:
                    arrayVal = matchPatterns[k, 3 - j1, i1];
                    break;
                case 2:
                    arrayVal = matchPatterns[k, j1, 3 - i1];
                    break;
                case 3:
                    arrayVal = matchPatterns[k, 3 - j1, 3 - i1];
                    break;
            }

            return arrayVal;
        }

        int IsPointFavored(Point pt)
        {
            int x = (int)pt.X;
            int y = (int)pt.Y;
            //a point is favored if it has 5 consecutive adjoining nonboundary points
            //that means it's a corner or peninsula
            ModuleView naSource = theNeuronArray.FindModuleByLabel("Boundary");
            if (naSource == null) return 0;

            if (naSource.GetNeuronAt(x, y) is Neuron n0)
                if (n0.LastCharge != 1) return 0;

            int bestCount = 0;
            int consecutiveCount = 0;
            for (int i = 0; i < 15; i++)
            {
                ModuleBoundary.GetDeltasFromDirection(i, out int dx, out int dy);
                if (naSource.GetNeuronAt(x + dx, y + dy) is Neuron n)
                {
                    if (n.LastCharge != 1)
                        consecutiveCount++;
                    else
                        consecutiveCount = 0;
                }
                if (consecutiveCount > bestCount)
                    bestCount = consecutiveCount;
                if (consecutiveCount == 5)
                    return consecutiveCount;
            }
            return bestCount;
        }


        float GetNeuronValue(Point p)
        {
            float retVal = 0;
            ModuleView naSource = theNeuronArray.FindModuleByLabel("Boundary");
            if (naSource == null) return retVal;
            if (naSource.GetNeuronAt((int)p.X, (int)p.Y) is Neuron n)
                retVal = n.LastCharge;
            return retVal;
        }
        Arc GetLongestLinearBoundary(int x, int y)
        {
            float bestLength = 0;
            Arc bestArc = null;
            //replace with search
            for (float angleDegrees = 0; angleDegrees < 180; angleDegrees += angleStep)
            {
                if (GetLinearBoundary(x, y, angleDegrees) is Arc a && a.Length > 0)
                {
                    if (a.Length > bestLength)
                    {
                        bestLength = a.Length;
                        bestArc = a;
                    }
                    if (angleDegrees == 0)
                        horizBoundaries.Add(bestArc);
                    else
                        vertBoundaries.Add(bestArc);
                }
            }

            if (bestArc != null && bestArc.Length > 0)
            {
                //ClearArcNeurons(bestArc);
                if (bestArc.p2.X < bestArc.p1.X ||
                    (bestArc.p2.X == bestArc.p1.X && bestArc.p2.Y < bestArc.p1.Y))
                {
                    Point temp = bestArc.p1;
                    bestArc.p1 = bestArc.p2;
                    bestArc.p2 = temp;
                }
            }
            return bestArc;
        }
        void ClearArcNeurons(Arc a) //except endpoints
        {
            ModuleView naSource = theNeuronArray.FindModuleByLabel("Boundary");
            if (naSource == null) return;
            float dx = (float)(a.p2.X - a.p1.X);
            float dy = (float)(a.p2.Y - a.p1.Y);
            if (Math.Abs(dx) > Math.Abs(dy))
            {
                dx /= Math.Abs(dx);
                dy /= Math.Abs(dx);
            }
            else
            {
                dx = dx / Math.Abs(dy);
                dy = dy / Math.Abs(dy);
            }
            float curx = (float)(a.p1.X + dx);
            float cury = (float)(a.p1.Y + dy);
            float ex = (float)(a.p2.X - dx);
            float ey = (float)(a.p2.Y - dy);

            while (InRange(curx, a.p1.X, ex) && InRange(cury, a.p1.Y, ey))
            {
                if (naSource.GetNeuronAt((int)curx, (int)cury) is Neuron n)
                {
                    if (n.LastCharge == 1)
                    {
                        n.LastCharge = 0.5f;
                        n.Update();
                    }
                }
                curx += dx;
                cury += dy;
            }
        }
        bool InRange(float value, double e1, double e2)
        {
            if (e1 > e2)
                return (value >= e2 && value <= e1);
            return (value >= e1 && value <= e2);
        }

        float ToRadians(float angleDegrees)
        {
            return (float)(Math.PI / 180) * angleDegrees;
        }
        Arc GetLinearBoundary(int x, int y, float angleDegrees)
        {
            Arc retVal = null;
            if (na.GetNeuronAt(x, y) is Neuron n2 && n2.LastCharge != 1 && n2.LastCharge != 0.5f) return retVal;

            ModuleView naSource = theNeuronArray.FindModuleByLabel("Boundary");
            if (naSource == null) return retVal;

            float curx = x;
            float cury = y;
            float dx = 1;
            float dy = 1;

            int sx = x;
            int sy = y;
            int ex = x;
            int ey = y;

            //follow along in positive direction
            if (angleDegrees > 45 && angleDegrees < 135)
            {
                dx = (float)(1 / Math.Tan(ToRadians(angleDegrees)));
            }
            else if (angleDegrees <= 45 || angleDegrees >= 135)
            {
                dy = (float)Math.Tan(ToRadians(angleDegrees));
            }

            while (curx >= 0 && curx < naSource.Width && cury >= 0 && cury < naSource.Height)
            {
                if (naSource.GetNeuronAt((int)Math.Round(curx), (int)Math.Round(cury)) is Neuron n)
                {
                    if (n.LastCharge != 1)//  && n.LastCharge != 0.5f)
                    {
                        break;
                    }
                }
                ex = (int)curx;
                ey = (int)cury;
                curx += dx;
                cury += dy;
            }
            curx = x;
            cury = y;
            dx = -dx;
            dy = -dy;

            while (curx >= 0 && curx < naSource.Width && cury >= 0 && cury < naSource.Height)
            {
                if (naSource.GetNeuronAt((int)Math.Round(curx), (int)Math.Round(cury)) is Neuron n)
                {
                    if (n.LastCharge != 1)// && n.LastCharge != 0.5f)
                    {
                        break;
                    }
                }
                sx = (int)curx;
                sy = (int)cury;
                curx += dx;
                cury += dy;
            }

            retVal = new Arc { p1 = new Point(sx, sy), p2 = new Point(ex, ey) };
            if (Math.Abs(retVal.Angle.ToDegrees() - angleDegrees) % 180 > angleStep / 2)
                retVal = null;
            return retVal;
        }
        Arc GetArcBoundary(int x, int y, float startAngle, float radius)
        {
            Arc retVal = null;
            return retVal;
        }

        void RemoveDanglingSegments()
        {
            for (int i = 0; i < boundaries.Count; i++)
            {
                for (int j = 0; j < boundaries.Count; j++)
                {
                    if (j != i)
                    {                    //is p1 dangling?
                        if (boundaries[i].p1 == boundaries[j].p1 ||
                                boundaries[i].p1 == boundaries[j].p2)
                        {
                            goto found;
                        }
                    }
                }
                boundaries.RemoveAt(i);
                i--;
                continue;
            found:
                //p1 is not connected
                for (int j = 0; j < boundaries.Count; j++)
                {
                    if (j != i)
                    {                    //is p2 dangling?
                        if (boundaries[i].p2 == boundaries[j].p1 ||
                                boundaries[i].p2 == boundaries[j].p2)
                        {
                            goto found1;
                        }
                    }
                }
                //p2 is not connected
                boundaries.RemoveAt(i);
                i--;
            found1:
                continue;
            }
        }

        void FindHorizBoundaries()
        {
            favoredPoints.Clear();
            horizBoundaries.Clear();
            for (int j = 0; j < naSource.Height; j++)
            {
                for (int i = 0; i < naSource.Width; i++)
                {
                    if (i == 4 && j == 36)
                    { }
                    if (naSource.GetNeuronAt(i, j) is Neuron n)
                    {
                        if (n.LastCharge > 0.9f && n.LastCharge < 1)
                        {
                            favoredPoints.Add(new Point(i, j));
                        }
                        if (n.LastCharge > 0.9f)
                        {
                            if (GetNeuronAndValue(i - 1, j) == 0)
                            {
                                int ex = i;
                                //start is i,j
                                while (GetNeuronAndValue(ex + 1, j) > 0.9f)
                                {
                                    ex++;
                                    if (GetNeuronAndValue(ex, j) < 1)
                                        favoredPoints.Add(new Point(ex, j));
                                }
                                horizBoundaries.Add(new Arc { p1 = new Point(i, j), p2 = new Point(ex, j) });
                                i = ex;
                            }
                        }
                    }
                }
            }
        }


        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            bndryPtCt = 0;
            bndryCt = 0;
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (na == null) return; //this is called the first time before the module actually exists
        }
    }
}
