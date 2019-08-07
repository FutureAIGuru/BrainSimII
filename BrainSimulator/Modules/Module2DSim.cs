using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace BrainSimulator
{
    public class Module2DSim : ModuleBase
    {
        public class physObject
        {
            public Point P1;
            public Point P2;
            public Color theColor;
            public float Aroma = 0;
            public float Temperature = 0;
        }
        public List<physObject> objects = new List<physObject>();
        public List<Point> CameraTrack = new List<Point>();
        public List<physObject> currentView = new List<physObject>();

        //private Vector[] antennaeRelative = { new Vector(.5, .5), new Vector(-.75,.75) };
        private Vector[] antennaeRelative = { new Vector(.5, .5)};
        public Point[] antennaeActual;

        public Point CameraPosition = new Point(0, 0);
        public float CameraDirection1 = (float)Math.PI/2;
        private double boundarySize = 5.5;

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
        }
        public override void Initialize()
        {
            Random rand = new Random();
            CameraTrack.Clear();
            CameraPosition = new Point(0, 0);
            CameraTrack.Add(CameraPosition);
            CameraDirection1 = (float)Math.PI/2;

            antennaeRelative = new Vector[] { new Vector(0.4, .5), new Vector(-0.4, .5) };
            antennaeActual = new Point[antennaeRelative.Length];
            for (int i = 0; i < antennaeRelative.Length; i++)
                antennaeActual[i] = CameraPosition + antennaeRelative[i];

            objects.Clear();
            
            //build a pen to keep the entity inside
            objects.Add(new physObject() { P1 = new Point(boundarySize, boundarySize), P2 = new Point(boundarySize, -boundarySize), theColor = Colors.Black });
            objects.Add(new physObject() { P1 = new Point(boundarySize, -boundarySize), P2 = new Point(-boundarySize, -boundarySize), theColor = Colors.Black });
            objects.Add(new physObject() { P1 = new Point(-boundarySize, -boundarySize), P2 = new Point(-boundarySize, boundarySize), theColor = Colors.Black });
            objects.Add(new physObject() { P1 = new Point(-boundarySize, boundarySize), P2 = new Point(boundarySize, boundarySize), theColor = Colors.Black });

            //create a few  obstacles for testing
            physObject newObject = new physObject
            {
                P1 = new Point(-2, 1.5),
                P2 = new Point(2, 1.5),
                theColor = Colors.Red,
                Aroma = -1,
                Temperature = 10,
            };
            objects.Add(newObject);
            newObject = new physObject
            {
                P1 = new Point(2,1.5),
                P2 = new Point(2,2),
                theColor = Colors.Blue,
                Aroma = -1,
                Temperature = -10,
            };
            objects.Add(newObject);
            newObject = new physObject
            {
                P1 = new Point(.75, 4),
                P2 = new Point(.85, 3),
                theColor = Colors.Blue,
                Aroma = -1,
                Temperature = -10,
            };
            objects.Add(newObject);
            newObject = new physObject
            {
                P1 = new Point(.75, 5),
                P2 = new Point(-.85, 5),
                theColor = Colors.Green,
                Aroma = 1,
                Temperature = 5,
            };
            objects.Add(newObject);

            ////add some randome obstacles?
            CreateView();
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });

        }

        public void Rotate(double theta) //(in radians * 1000)
        {
            CameraDirection1 -= (float)theta / 1000f;
            if (CameraDirection1 > Math.PI) CameraDirection1 -= (float)Math.PI*2;
            if (CameraDirection1 < -Math.PI) CameraDirection1 += (float)Math.PI*2;
            CreateView();
            HandleTouch(); 
            //CreateAroma();  //rotation doesn't affect aroma field (at this time)
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
        }


        private Point Bound(Point p)
        {
            if (p.X > boundarySize) p.X = boundarySize;
            if (p.Y > boundarySize) p.Y = boundarySize;
            if (p.X < -boundarySize) p.X = -boundarySize;
            if (p.Y < -boundarySize) p.Y = -boundarySize;
            return p;
        }

        double lastGreen = 0;

        public bool Move(int motion)
        {
            Point newPosition = new Point()
            {
                X = CameraPosition.X + motion / 1000f * Math.Cos(CameraDirection1),
                Y = CameraPosition.Y + motion / 1000f * Math.Sin(CameraDirection1)
            };

            //code for eating green objects
            //for (int i = 0; i < objects.Count; i++)
            //{
            //    if (objects[i].theColor == Colors.Green)
            //    {
            //        Point P1 = objects[i].P1;
            //        Point P2 = objects[i].P2;
            //        physObject ph = objects[i];
            //        FindIntersection(P1, P2, CameraPosition, newPosition, out bool linesintersect, out bool segments_intersect, out Point Intersection, out Point close_1, out Point close_p2);
            //        if (segments_intersect)
            //        {//we found some food
            //            Vector v = P2 - P1;
            //            P2 = P1 + v * .95;
            //            ph.P2 = P2;
            //            objects.RemoveAt(i);
            //            if (v.Length > .2)
            //                objects.Add(ph);
            //            return false;
            //        }
            //    }
            //}

            //check for collisions
            //collision can impede motion
            //collision is actual intersection of desired motion path and obstacle
            bool collision = CheckForCollisions(newPosition);


            //update position and add track...only if moving is OK
            Vector v1 = newPosition - CameraPosition;
            if (!collision)
            {
                CameraPosition = newPosition;
                CameraTrack.Add(CameraPosition);
            }

            HandleAroma();
            HandleTouch();
            CreateView();
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });

            if (collision)
            {
                NeuronArea naEntity= theNeuronArray.FindAreaByLabel("ModuleEntity");
                if (naEntity != null)
                {
                    try
                    {
                        naEntity.GetNeuronAt("Collision").CurrentCharge = 1;
                    }
                    catch { }
                }

            }
            return (!collision);
        }

        //a collision is the intersection of the desired newPosition and an obstacle
        private bool CheckForCollisions(Point newPosition)
        {
            //generally, you can only be up against one obstacle 
            for (int i = 0; i < objects.Count; i++)
            {
                Point P1 = objects[i].P1;
                Point P2 = objects[i].P2;
                physObject ph = objects[i];
                FindIntersection(P1, P2, CameraPosition, newPosition, out bool linesintersect, out bool segments_intersect, 
                    out Point Intersection, out Point close_1, out Point close_p2,out double collisionAngle);
                if (segments_intersect)
                {//we're against an obstacle
                    return true;
                }
            }
            return false;
        }

        //aroma is a field strength at a given position
        private void HandleAroma()
        {
            NeuronArea naSmell = theNeuronArray.FindAreaByLabel("Module2DSmell");
            //find the aroma value
            if (naSmell != null)
            {
                double sumGreen = GetColorWeightAtPoint(CameraPosition, Colors.Green);

                double diffGreen = sumGreen - lastGreen;
                lastGreen = sumGreen;
                if (diffGreen > .00001)
                    naSmell.GetNeuronAt(0, 0).CurrentCharge = 1;
                else if (diffGreen > -.01)
                    naSmell.GetNeuronAt(1, 0).CurrentCharge = 1;
                else
                    naSmell.GetNeuronAt(2, 0).CurrentCharge = 1;
            }
        }

        //touch is the intersection of an antenna with an obstacle
        public void HandleTouch()
        {
            //is there an object within .1 in front of us
            for (int i = 0; i < antennaeRelative.Length; i++)
            {
                PolarVector pv = Utils.ToPolar((Point)antennaeRelative[i]);
                //pv.theta = Math.PI / 2 - pv.theta;
                pv.theta = CameraDirection1 -  pv.theta;
                Point antennaPositionAbs = CameraPosition + (Vector) Utils.ToCartesian(pv);

                //Point antennaPositionAbs = CameraPosition + antennaeRelative[i];
                CheckAntenna(antennaPositionAbs,i);

            }
        }

        private void CheckAntenna(Point antennaPositionAbs, int index)
        {
            //this all works in absolute coordinates
            NeuronArea naTouch = theNeuronArray.FindAreaByLabel("Module2DTouch");
            if (naTouch == null) return;

            antennaeActual[index] = antennaPositionAbs;

            for (int i = 0; i < objects.Count; i++)
            {
                Point P1 = objects[i].P1;
                Point P2 = objects[i].P2;
                physObject ph = objects[i];
                FindIntersection(P1, P2, CameraPosition, antennaPositionAbs, out bool linesintersect, out bool segments_intersect,
                    out Point Intersection, out Point close_p1, out Point close_p2, out double collisionAngle);

                if (segments_intersect)
                {//we're touching
                 //can we feel the end of the segment?
                    float conf1 = 0;
                    float conf2 = 0;
                    float l1 = 0.5f; //initial (guess) length of segment
                    float l2 = 0.5f;
                    Vector dist1 = P1 - CameraPosition;
                    if (dist1.Length < ((Vector)antennaPositionAbs - (Vector)CameraPosition).Length)
                    {
                        conf1 = 1;
                        l1 = (float)(Intersection - P1).Length;
                    }
                    Vector dist2 = P2 - CameraPosition;
                    if (dist2.Length < ((Vector)antennaPositionAbs-(Vector)CameraPosition).Length)
                    {
                        conf2 = 1;
                        l2 = (float)(Intersection - P2).Length;
                    }
                    //collisionAngle -= Math.PI / 2;

                    Vector antennaPositionRel = Intersection - CameraPosition;
                    antennaPositionRel = Utils.RotateVector(antennaPositionRel, Math.PI / 2 - CameraDirection1);
                    PolarVector pv = Utils.ToPolar((Point)antennaPositionRel);

                    //everything from here out is relative coordinates
                    //neurons:  0:touch   1:antAngle  2:antDistance 3: sensedLineAngle 4: conf1 5: len1 6: conf2 7: len2
                    naTouch.GetNeuronAt(0, index).CurrentCharge = 1;
                    naTouch.GetNeuronAt(1, index).CurrentCharge = (float)pv.theta;
                    naTouch.GetNeuronAt(2, index).CurrentCharge = (float)pv.r;
                    naTouch.GetNeuronAt(3, index).CurrentCharge = (float)collisionAngle;
                    naTouch.GetNeuronAt(4, index).CurrentCharge = (float)conf1;
                    naTouch.GetNeuronAt(5, index).CurrentCharge = (float)l1;
                    naTouch.GetNeuronAt(6, index).CurrentCharge = (float)conf2;
                    naTouch.GetNeuronAt(7, index).CurrentCharge = (float)l2;

                    antennaeActual[index] = Intersection;
                }
            }
        }

        //do ray tracing to create the view the Entitiy would see
        private void CreateView()
        {
            NeuronArea naVision = theNeuronArray.FindAreaByLabel("Module2DVision");
            if (naVision == null) return;

            currentView.Clear();
            double deltaTheta = Utils.fieldOfView; //60-degrees
            deltaTheta /= (double)(naVision.Width-1); //-1 so we get both endpoints
            double direction = CameraDirection1 + Utils.fieldOfView/2;
            double theta = direction;

            for (int i = 0; i < naVision.Width; i++)
            {
                //create a segment from the view direction for this pixel
                Point p2 = CameraPosition + new Vector(Math.Cos(theta) * 100, Math.Sin(theta) * 100);
                Color theColor = Colors.Pink;
                double closestDistance = 20;
                for (int j = 0; j < objects.Count; j++)
                {
                    FindIntersection(CameraPosition, p2, objects[j].P1, objects[j].P2,
                        out bool lines_intersect, out bool segments_intersect, 
                        out Point intersection, out Point close_p1, out Point closep2,out double collisionAngle);
                    if (segments_intersect)
                    {
                        double distance = Point.Subtract(intersection, CameraPosition).Length;
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            theColor = objects[j].theColor;
                        }
                    }
                }
                naVision.GetNeuronAt(i, 0).SetValueInt(Utils.ToArgb(theColor));
                Point p3 = new Point (CameraPosition.X + p2.X/100,CameraPosition.Y + p2.Y/100);
                currentView.Add(new physObject() { P1 = p3, P2 = new Point(0,0), theColor = theColor });
                theta -= deltaTheta;
            }
        }

        public double GetColorWeightAtPoint(Point point, Color theColor)
        {
            double sum = 0;
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].theColor == theColor)
                {
                    Point closest;
                    Point P1 = objects[i].P1;
                    Point P2 = objects[i].P2;
                    double distance = FindDistanceToSegment(point, P1, P2, out closest);
                    sum += 1 / (distance * distance);
                }
            }
            return sum;
        }

        // Calculate the distance between
        // point pt and the segment p1 --> p2.
        private double FindDistanceToSegment(
            Point pt, Point p1, Point p2, out Point closest)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            if ((dx == 0) && (dy == 0))
            {
                // It's a point not a line segment.
                closest = p1;
                dx = pt.X - p1.X;
                dy = pt.Y - p1.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            // Calculate the t that minimizes the distance.
            double t = ((pt.X - p1.X) * dx + (pt.Y - p1.Y) * dy) /
                (dx * dx + dy * dy);

            // See if this represents one of the segment's
            // end points or a point in the middle.
            if (t < 0)
            {
                closest = new Point(p1.X, p1.Y);
                dx = pt.X - p1.X;
                dy = pt.Y - p1.Y;
            }
            else if (t > 1)
            {
                closest = new Point(p2.X, p2.Y);
                dx = pt.X - p2.X;
                dy = pt.Y - p2.Y;
            }
            else
            {
                closest = new Point(p1.X + t * dx, p1.Y + t * dy);
                dx = pt.X - closest.X;
                dy = pt.Y - closest.Y;
            }

            return Math.Sqrt(dx * dx + dy * dy);
        }

        // Find the point of intersection between
        // the lines p1 --> p2 and p3 --> p4.
        private void FindIntersection(
            Point p1, Point p2, Point p3, Point p4,
            out bool lines_intersect, out bool segments_intersect,
            out Point intersection,
            out Point close_p1, out Point close_p2,
            out double collisionAngle)
        {
            // Get the segments' parameters.
            double dx12 = p2.X - p1.X;
            double dy12 = p2.Y - p1.Y;
            double dx34 = p4.X - p3.X;
            double dy34 = p4.Y - p3.Y;

            double theta1 = Math.Atan2(dy12, dx12); //obstacle
            double theta2 = Math.Atan2(dy34,dx34); //motion attempt
            collisionAngle = theta2 - theta1;

            // Solve for t1 and t2
            double denominator = (dy12 * dx34 - dx12 * dy34);

            double t1 =
                ((p1.X - p3.X) * dy34 + (p3.Y - p1.Y) * dx34)
                    / denominator;
            if (double.IsInfinity(t1))
            {
                // The lines are parallel (or close enough to it).
                lines_intersect = false;
                segments_intersect = false;
                intersection = new Point(float.NaN, float.NaN);
                close_p1 = new Point(float.NaN, float.NaN);
                close_p2 = new Point(float.NaN, float.NaN);
                return;
            }
            lines_intersect = true;

            double t2 =
                ((p3.X - p1.X) * dy12 + (p1.Y - p3.Y) * dx12)
                    / -denominator;

            // Find the point of intersection.
            intersection = new Point(p1.X + dx12 * t1, p1.Y + dy12 * t1);

            // The segments intersect if t1 and t2 are between 0 and 1.
            segments_intersect =
                ((t1 >= 0) && (t1 <= 1) &&
                 (t2 >= 0) && (t2 <= 1));

            // Find the closest points on the segments.
            if (t1 < 0)
            {
                t1 = 0;
            }
            else if (t1 > 1)
            {
                t1 = 1;
            }

            if (t2 < 0)
            {
                t2 = 0;
            }
            else if (t2 > 1)
            {
                t2 = 1;
            }

            close_p1 = new Point(p1.X + dx12 * t1, p1.Y + dy12 * t1);
            close_p2 = new Point(p3.X + dx34 * t2, p3.Y + dy34 * t2);
        }
    }
}


