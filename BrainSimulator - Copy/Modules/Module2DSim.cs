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
        private Vector[] antennaeRelative = { new Vector(.5, .5) };
        public Point[] antennaeActual;

        public Point CameraPosition = new Point(0, 0);
        public float CameraDirection1 = (float)Math.PI / 2;
        public double boundarySize = 5.5;

        Random rand = new Random();

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
        }
        public override void Initialize()
        {
            CameraTrack.Clear();
            CameraPosition = new Point(0, 0);
            CameraTrack.Add(CameraPosition);
            CameraDirection1 = (float)Math.PI / 2;

            antennaeRelative = new Vector[] { new Vector(0.5, .5), new Vector(-0.5, .5) };
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
                P1 = new Point(0, 1.8),
                P2 = new Point(2, .9),
                theColor = Colors.Red,
                Aroma = -1,
                Temperature = 10,
            };
            objects.Add(newObject);
            //newObject = new physObject
            //{
            //    P1 = new Point(2,1.5),
            //    P2 = new Point(2,2),
            //    theColor = Colors.Blue,
            //    Aroma = -1,
            //    Temperature = -10,
            //};
            //objects.Add(newObject);
            //newObject = new physObject
            //{
            //    P1 = new Point(.75, 4),
            //    P2 = new Point(.85, 3),
            //    theColor = Colors.Blue,
            //    Aroma = -1,
            //    Temperature = -10,
            //};
            //objects.Add(newObject);
            //newObject = new physObject
            //{
            //    P1 = new Point(.75, 5),
            //    P2 = new Point(-.85, 5),
            //    theColor = Colors.Green,
            //    Aroma = 1,
            //    Temperature = 5,
            //};
            //objects.Add(newObject);

            ////add some random obstacles?
            for (int i = 0; i <15; i++)
            {
                float coord = (float)(rand.NextDouble() * 2 * boundarySize - boundarySize);
                Point p1 = new Point(randCoord(), randCoord());
                Point p2 = new Point(p1.X + randCoord(2f), p1.Y + randCoord(2f));
                physObject newobject = new physObject()
                {
                    P1 = p1,
                    P2 = p2,
                    theColor = randColor(),
                    Aroma = 1,
                    Temperature = 5,
                };
                objects.Add(newobject);
            }
            HandleVision();
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });

        }
        public float randCoord()
        {
            return (float)(rand.NextDouble() * 2 * (boundarySize - 2) - (boundarySize - 2));
        }
        public float randCoord(float dist)
        {
            return (float)(rand.NextDouble() * 2 * dist - dist);
        }
        public Color randColor()
        {
            double temp = rand.NextDouble();
            if (temp < .5) return Colors.Blue;
            if (temp < .75) return Colors.Red;
            return Colors.Green;
        }

        public void Rotate(double theta) //(in radians * 1000)
        {
            CameraDirection1 -= (float)theta;
            if (CameraDirection1 > Math.PI) CameraDirection1 -= (float)Math.PI * 2;
            if (CameraDirection1 < -Math.PI) CameraDirection1 += (float)Math.PI * 2;
            HandleTouch();
            HandleVision();
            HandleAroma();
            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
        }

        public bool Move(float motion)
        {
            Point newPosition = new Point()
            {
                X = CameraPosition.X + motion * Math.Cos(CameraDirection1),
                Y = CameraPosition.Y + motion * Math.Sin(CameraDirection1)
            };

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

            HandleTouch();
            HandleVision();
            HandleAroma();

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                //because of multi-theading, dlg may be null by the time we need to use it.
                try
                {
                    dlg.Draw();
                }
                catch { }
            });
            return !collision;
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
                Utils.FindIntersection(P1, P2, CameraPosition, newPosition, out bool linesintersect, out bool segments_intersect,
                    out Point Intersection, out Point close_1, out Point close_p2, out double collisionAngle);
                if (segments_intersect)
                {//we're against an obstacle

                    NeuronArea naEntity = theNeuronArray.FindAreaByLabel("ModuleEntity");
                    if (naEntity != null)
                    {
                        try
                        {
                            naEntity.GetNeuronAt("Collision").CurrentCharge = 1;
                            naEntity.GetNeuronAt("CollisionAngle").SetValue((float)collisionAngle);
                            if (objects[i].theColor == Colors.Green)
                                naEntity.GetNeuronAt("Feed").CurrentCharge = 1;
                        }
                        catch { }
                    }
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
                for (int i = 0; i < antennaeActual.Length; i++)
                {
                    sumGreen = GetColorWeightAtPoint(antennaeActual[i], Colors.Green);
                    naSmell.GetNeuronAt(i, 0).SetValue((float)sumGreen);
                }
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
                pv.theta = CameraDirection1 - pv.theta;
                Point antennaPositionAbs = CameraPosition + (Vector)Utils.ToCartesian(pv);

                //Point antennaPositionAbs = CameraPosition + antennaeRelative[i];
                CheckAntenna(antennaPositionAbs, i);

            }
        }

        private void CheckAntenna(Point antennaPositionAbs, int index)
        {
            //this all works in absolute coordinates
            NeuronArea naTouch = theNeuronArray.FindAreaByLabel("Module2DTouch");
            if (naTouch == null) return;

            antennaeActual[index] = antennaPositionAbs;
            naTouch.GetNeuronAt(0, index).SetValue(0);
            naTouch.GetNeuronAt(8, index).CurrentCharge = 1;

            for (int i = 0; i < objects.Count; i++)
            {
                Point P1 = objects[i].P1;
                Point P2 = objects[i].P2;
                physObject ph = objects[i];
                Utils.FindIntersection(P1, P2, CameraPosition, antennaPositionAbs, out bool linesintersect, out bool segments_intersect,
                    out Point Intersection, out Point close_p1, out Point close_p2, out double collisionAngle);

                if (segments_intersect)
                {//we're touching
                 //can we feel the end of the segment?
                    float p1IsEndpt = 0;
                    float p2IsEndpt = 0;
                    float assumedLength = 0.5f;
                    float l1 = assumedLength; //initial (guess) length of segment
                    float l2 = assumedLength;
                    Vector dist1 = P1 - Intersection;
                    if (dist1.Length <= assumedLength)
                    {
                        p1IsEndpt = 1;
                        l1 = (float)dist1.Length;
                    }
                    Vector dist2 = P2 - Intersection;
                    if (dist2.Length <= assumedLength)
                    {
                        p2IsEndpt = 1;
                        l2 = (float)dist2.Length;
                    }

                    Vector antennaPositionRel = Intersection - CameraPosition;
                    antennaPositionRel = Utils.RotateVector(antennaPositionRel, Math.PI / 2 - CameraDirection1);
                    PolarVector pv = Utils.ToPolar((Point)antennaPositionRel);

                    //everything from here out is relative coordinates
                    //neurons:  0:touch   1:antAngle  2:antDistance 3: sensedLineAngle 4: conf1 5: len1 6: conf2 7: len2 8: Release
                    naTouch.GetNeuronAt(0, index).SetValue(1);
                    naTouch.GetNeuronAt(1, index).CurrentCharge = (float)pv.theta;
                    naTouch.GetNeuronAt(2, index).CurrentCharge = (float)pv.r;
                    naTouch.GetNeuronAt(3, index).CurrentCharge = (float)collisionAngle;
                    naTouch.GetNeuronAt(4, index).CurrentCharge = (float)p1IsEndpt;
                    naTouch.GetNeuronAt(5, index).CurrentCharge = (float)l1;
                    naTouch.GetNeuronAt(6, index).CurrentCharge = (float)p2IsEndpt;
                    naTouch.GetNeuronAt(7, index).CurrentCharge = (float)l2;
                    naTouch.GetNeuronAt(8, index).CurrentCharge = 0;

                    antennaeActual[index] = Intersection;
                    break;
                }
            }
        }

        //do ray tracing to create the view the Entitiy would see
        private void HandleVision()
        {
            NeuronArea naVision = theNeuronArray.FindAreaByLabel("Module2DVision");
            if (naVision == null) return;

            currentView.Clear();
            double deltaTheta = Utils.fieldOfView; //60-degrees
            deltaTheta /= (double)(naVision.Width - 1); //-1 so we get both endpoints
            double direction = CameraDirection1 + Utils.fieldOfView / 2;
            double theta = direction;

            for (int i = 0; i < naVision.Width; i++)
            {
                //create a segment from the view direction for this pixel
                Point p2 = CameraPosition + new Vector(Math.Cos(theta) * 100, Math.Sin(theta) * 100);
                Color theColor = Colors.Pink;
                double closestDistance = 20;
                for (int j = 0; j < objects.Count; j++)
                {
                    Utils.FindIntersection(CameraPosition, p2, objects[j].P1, objects[j].P2,
                        out bool lines_intersect, out bool segments_intersect,
                        out Point intersection, out Point close_p1, out Point closep2, out double collisionAngle);
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
                Point p3 = new Point(CameraPosition.X + p2.X / 100, CameraPosition.Y + p2.Y / 100);
                currentView.Add(new physObject() { P1 = p3, P2 = new Point(0, 0), theColor = theColor });
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
                    double distance = Utils.FindDistanceToSegment(point, P1, P2, out closest);
                    sum += 1 / (distance * distance);
                }
            }
            return sum;
        }

    }
}


