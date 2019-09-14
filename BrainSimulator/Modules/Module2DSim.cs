using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;

namespace BrainSimulator
{
    public class Module2DSim : ModuleBase
    {
        //here's how we'll define objects in the simulated environment
        public class physObject
        {
            public Point P1;
            public Point P2;
            public Color theColor;
            public float Aroma = 0;
            public float Temperature = 0;
        }
        public List<physObject> objects = new List<physObject>();

        [XmlIgnore]
        public List<Point> CameraTrack = new List<Point>();
        [XmlIgnore]
        public List<physObject> currentView0 = new List<physObject>();
        [XmlIgnore]
        public List<physObject> currentView1 = new List<physObject>();

        //where the antenna tips are relative to self
        public Vector[] antennaeRelative = { new Vector(.5, .5) };

        [XmlIgnore]
        public Point[] antennaeActual = { new Point(0, 0) };

        //where we are in the world
        public Point CameraPosition = new Point(0, 0);
        public float CameraDirection1 = (float)Math.PI / 2;

        //the size of the universe
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
            CameraDirection1 = 0;

            antennaeRelative = new Vector[] { new Vector(.5, .5), new Vector(.5, -.5) };
            antennaeActual = new Point[2];
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
                P1 = new Point(1.5, 3),
                P2 = new Point(2.5,1.5),
                theColor = Colors.Red,
                Aroma = -1,
                Temperature = 10,
            };
            objects.Add(newObject);
            newObject = new physObject
            {
                P1 = new Point(.5, -2),
                P2 = new Point(-1, -2),
                theColor = Colors.Red,
                Aroma = -1,
                Temperature = 10,
            };
            objects.Add(newObject);
            newObject = new physObject
            {
                P1 = new Point(3.5, -.5),
                P2 = new Point(2.5, -4),
                theColor = Colors.Blue,
                Aroma = -1,
                Temperature = -10,
            };
            objects.Add(newObject);
            newObject = new physObject
            {
                P1 = new Point(-2, 1),
                P2 = new Point(-2.5, -1.5),
                theColor = Colors.Green,
                Aroma = -1,
                Temperature = -10,
            };
            objects.Add(newObject);
            newObject = new physObject
            {
                P1 = new Point(.75, 3),
                P2 = new Point(-.85, 3),
                theColor = Colors.Orange,
                Aroma = 1,
                Temperature = 5,
            };
            objects.Add(newObject);

            ////add some random obstacles
            //for (int i = 0; i < 5; i++)
            //{
            //    float coord = (float)(rand.NextDouble() * 2 * boundarySize - boundarySize);
            //    Point p1 = new Point(randCoord(), randCoord());
            //    Point p2 = new Point(p1.X + randCoord(2f), p1.Y + randCoord(2f));
            //    physObject newobject = new physObject()
            //    {
            //        P1 = p1,
            //        P2 = p2,
            //        theColor = randColor(),
            //        Aroma = 1,
            //        Temperature = 5,
            //    };
            //    objects.Add(newobject);
            //}
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

        //these are called to move and rotate the entity within the simulator
        public void Rotate(double theta) //(in radian CW) 
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

        //returning true said there no collision and it is OK to move there...in the event of a collision, the move is cancelled
        public bool Move(float motion) //move fwd (in inches)
        {
            Point newPosition = new Point()
            {
                X = CameraPosition.X + motion * Math.Cos(CameraDirection1),
                Y = CameraPosition.Y + motion * Math.Sin(CameraDirection1)
            };

            //check for collisions  ollision can impede motion
            //collision is actual intersection of desired motion path and obstacle as opposed to a touch
            //which is an intersection between an antenna and does not impede motion
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
                    NeuronArea naBehavior = theNeuronArray.FindAreaByLabel("ModuleBehavior");
                    if (naBehavior != null)
                    {
                        try
                        {
                            naBehavior.GetNeuronAt("Coll").CurrentCharge = 1;
                            naBehavior.GetNeuronAt("CollAngle").SetValue((float)collisionAngle);
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

        //this is a debug feature which copies the complete simulation to the internal model
        public void SetModel()
        {
            NeuronArea naModel = theNeuronArray.FindAreaByLabel("Module2DModel");
            Module2DModel nmModel = (Module2DModel)naModel.TheModule;
            for (int i = 0; i < objects.Count; i++)
            {
                PointPlus p1 = new PointPlus { Conf = 1, P = objects[i].P1 };
                PointPlus p2 = new PointPlus { Conf = 1, P = objects[i].P2 };
                nmModel.AddSegment(p1, p2, objects[i].theColor);
            }
        }

        //touch is the intersection of an antenna with an obstacle
        public void HandleTouch()
        {
            //antenna[0] is left [1] is right
            antennaeActual = new Point[antennaeRelative.Length];
            //is there an object intersecting the antennaew
            for (int i = 0; i < antennaeRelative.Length; i++)
            {
                PointPlus pv = new PointPlus { P = (Point)antennaeRelative[i] };
                pv.Theta = CameraDirection1 + pv.Theta;
                Point antennaPositionAbs = CameraPosition + (Vector)pv.P;
                CheckAntenna(antennaPositionAbs, i);

            }
        }

        private void CheckAntenna(Point antennaPositionAbs, int index)
        {

            antennaeActual[index] = antennaPositionAbs;
            //this all works in absolute coordinates
            NeuronArea naTouch = theNeuronArray.FindAreaByLabel("Module2DTouch");
            if (naTouch == null) return;
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

                    PointPlus antennaPositionRel = new PointPlus { P = (Point)(Intersection - CameraPosition) };
                    antennaPositionRel.Theta = antennaPositionRel.Theta-CameraDirection1;

                    //everything from here out is  coordinates relative to self
                    //neurons:  0:touch   1:antAngle  2:antDistance 3: sensedLineAngle 4: conf1 5: len1 6: conf2 7: len2 8: Release
                    naTouch.GetNeuronAt(0, index).SetValue(1);
                    naTouch.GetNeuronAt(1, index).CurrentCharge = antennaPositionRel.Theta;
                    naTouch.GetNeuronAt(2, index).CurrentCharge = antennaPositionRel.R;
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

        //do offsets to handle two eyes
        private void HandleVision()
        {
            Point oldCamerPosition = CameraPosition;
            Double offsetDirection = CameraDirection1 + Math.PI / 2;
            Vector offset = new Vector(Math.Cos(offsetDirection), Math.Sin(offsetDirection));
            offset = Vector.Multiply(Module2DVision.eyeOffset, offset);
            CameraPosition += offset;
            HandleVision(0);

            CameraPosition = oldCamerPosition;
            offsetDirection = CameraDirection1 - Math.PI / 2;
            offset = new Vector(Math.Cos(offsetDirection), Math.Sin(offsetDirection));
            offset = Vector.Multiply(Module2DVision.eyeOffset, offset);
            CameraPosition += offset;
            HandleVision(1);
            CameraPosition = oldCamerPosition;
        }
        //do ray tracing to create the view the Entitiy would see
        private void HandleVision(int row)
        {
            NeuronArea naVision = theNeuronArray.FindAreaByLabel("Module2DVision");
            if (naVision == null) return;

            if (row == 0)   
                currentView0.Clear();
            else
                currentView1.Clear();

            for (int i = 0; i < naVision.Width; i++)
            {
                double theta = Module2DVision.GetDirectionOfNeuron(i, naVision.Width);
                theta = CameraDirection1 + theta;
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
                naVision.GetNeuronAt(i, row).SetValueInt(Utils.ToArgb(theColor));
                Point p3 = new Point(CameraPosition.X + p2.X / 100, CameraPosition.Y + p2.Y / 100);
                if (row == 0)
                    currentView0.Add(new physObject() { P1 = p3, P2 = new Point(0, 0), theColor = theColor });
                else
                    currentView1.Add(new physObject() { P1 = p3, P2 = new Point(0, 0), theColor = theColor });
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


