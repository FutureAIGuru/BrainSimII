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

        //these are public so the dialog can get at them easily
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

            boundaries.Clear();

            FindSegments();

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

        void FindSegments()
        {
            //read the neurons in the bit map and get the boundary and corner points
            segments.Clear();
            favoredPoints.Clear();
            List<Point> firingPoints = new List<Point>();
            List<Point> pointsToSearch = new List<Point>();
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



        private Point PointCopy(Point p)
        {
            return new Point { X = p.X, Y = p.Y };
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
