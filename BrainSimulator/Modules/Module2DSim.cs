//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class Module2DSim : ModuleBase
    {
        //here's how we'll define a few physical objects in the simulated environment
        public class physObject
        {
            public Point P1;
            public Point P2;
            public Color theColor;
            public float Aroma = 0;
            public float Temperature = 0;
        }
        public List<physObject> objects = new List<physObject>();

        public override string ShortDescription { get => "A simulated 2D environment with obstacles"; }
        public override string LongDescription
        {
            get =>
                "This module uses no neurons of its own but fires neurons in various sensory modules if they are in the network. It has methods (Move and Turn and potentially others " +
                "which can be called by other modules to move its point of view around the simulation. " +
                "The mouse wheel can zoom the display and the left mouse button can drag (pan). " +
                "Right-clicking in the dialog box can direct the entity to that location." +
                ""
                ;
        }

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

        [XmlIgnore]
        public float BodyRadius = .2f;

        Random rand = new Random();

        public Module2DSim()
        {
        }

        float antennaTheta1 = 0;
        float antennaTheta2 = 0;

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable

            //antenna motion
            antennaeRelative = new Vector[] {
                new Vector(.4, .4) + new Vector(.1*rand.NextDouble()*Math.Cos(antennaTheta1),.1*Math.Sin(antennaTheta1)),
                new Vector(.4, -.4)+ new Vector(.1*rand.NextDouble()*Math.Cos(antennaTheta2),.1*Math.Sin(antennaTheta2))
            };
            antennaTheta1 += .3f;
            antennaTheta2 += .35f;
            HandleTouch();

            CreateRandomTrainingCase();
            DoTrainingAction();
            CheckForTrainingResult();

            UpdateDialog();
        }

        public override void Initialize()
        {
            CameraTrack.Clear();
            CameraPosition = new Point(0, 0);
            CameraTrack.Add(CameraPosition);
            CameraDirection1 = 0;

            na.GetNeuronAt(0).Label = "Actions";
            na.GetNeuronAt(1).Label = "Colors";
            na.GetNeuronAt(2).Label = "Train";

            antennaeRelative = new Vector[] { new Vector(.4, .4), new Vector(.4, -.4) };
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
                P2 = new Point(2.5, 1.5),
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

            newObject = new physObject
            {
                P1 = new Point(3.5, -.5),
                P2 = new Point(2.5, -4),
                theColor = Colors.Blue,
                Aroma = -1,
                Temperature = -10,
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
            UpdateDialog();
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
            UpdateDialog();
        }

        //returning true said there no collision and it is OK to move there...in the event of a collision, the move is cancelled
        public bool Move(float motion) //move fwd +, rev -
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
                double dist = Utils.FindDistanceToSegment(newPosition, ph.P1, ph.P2, out Point closest);
                if (dist < BodyRadius)
                {
                    SetNeuronValue("ModuleBehavior", "Coll", 1);
                    PointPlus collPt = new PointPlus { P = (Point)(closest - newPosition) };
                    collPt.Theta -= CameraDirection1;
                    SetNeuronValue("ModuleBehavior", "CollAngle", collPt.Theta);
                    return true;
                }
            }
            return false;
        }

        //aroma is a field strength at a given position
        private void HandleAroma()
        {
            ModuleView naSmell = theNeuronArray.FindAreaByLabel("Module2DSmell");
            //find the aroma value
            if (naSmell != null)
            {
                double sumGreen = GetColorWeightAtPoint(CameraPosition, Colors.Green);
                for (int i = 0; i < antennaeActual.Length; i++)
                {
                    sumGreen = GetColorWeightAtPoint(antennaeActual[i], Colors.Green);
                    SetNeuronValue("Module2DSmell", i, 0, (float)sumGreen);
                }
            }
        }


        //touch is the intersection of an arm with an obstacle
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
            SetNeuronValue("Module2DTouch", 0, index, 0);
            SetNeuronValue("Module2DTouch", 8, index, 2);

            //this all works in absolute coordinates
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
                    antennaPositionRel.Theta = antennaPositionRel.Theta - CameraDirection1;

                    float[] neuronValues = new float[9];
                    //everything from here out is  coordinates relative to self
                    //neurons:  0:touch   1:antAngle  2:antDistance 3: sensedLineAngle 4: conf1 5: len1 6: conf2 7: len2 8: Release
                    neuronValues[0] = 1;
                    neuronValues[1] = antennaPositionRel.Theta;
                    neuronValues[2] = antennaPositionRel.R;
                    neuronValues[3] = (float)collisionAngle;
                    neuronValues[4] = (float)p1IsEndpt;
                    neuronValues[5] = (float)l1;
                    neuronValues[6] = (float)p2IsEndpt;
                    neuronValues[7] = (float)l2;
                    neuronValues[8] = 0;
                    SetNeuronVector("Module2DTouch", true, index, neuronValues);

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
            int retinaWidth = GetModuleWidth("Module2DVision");

            if (row == 0)
                currentView0.Clear();
            else
                currentView1.Clear();

            int[] pixels = new int[retinaWidth];
            for (int i = 0; i < retinaWidth; i++)
            {
                double theta = Module2DVision.GetDirectionOfNeuron(i, retinaWidth);
                theta = CameraDirection1 + theta;
                //create a segment from the view direction for this pixel (length 100 assumes the size of the universe)
                Point p2 = CameraPosition + new Vector(Math.Cos(theta) * 100.0, Math.Sin(theta) * 100.0);
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
                pixels[i] = Utils.ToArgb(theColor);
                Point p3 = CameraPosition + new Vector(Math.Cos(theta), Math.Sin(theta));
                if (row == 0)
                    currentView0.Add(new physObject() { P1 = p3, P2 = CameraPosition, theColor = theColor });
                //currentView0.Add(new physObject() { P1 = p3, P2 = new Point(0, 0), theColor = theColor });
                else
                    currentView1.Add(new physObject() { P1 = p3, P2 = CameraPosition, theColor = theColor });
            }
            SetNeuronVector("Module2DVision", true, row, pixels);
        }

        //Training samples are triples... a spoken phrase, an anction, and an outcome (separated by colons)
        //TODO delete the outcome? It is always positive by implication
        string[] TrainingSamples = new string[]
        {
            "what is this:Say:Positive",
            "go:Go:Positive",
            "stop:Stop:Positive",
            "say:Say:Positive",
            "sallie:Attn:Positive",
            "turn around:UTurn:Positive",
            "turn around:UTurn:Positive",
            "turn left:LTurn:Positive",
            "turn right:RTurn:Positive",
            "this is [color]:NoAction:Positive"
,        };

        private void CreateRandomTrainingCase()
        {
            if (GetNeuronValue(null, "Actions") == 1 && actionTrainingState == -1)
            {
                int index = (int)(rand.NextDouble() * TrainingSamples.Length);
                trainingAction = TrainingSamples[index];
                actionTrainingState = 15;
            }
        }

        string trainingAction = ""; //gets the training case from the array of training samples
        string[] trainingCase;
        int actionTrainingState = -1;
        // -1: idle  15: just started 10:phrase out completed (max phrase is 5 wds)
        int colorTrainingDelay = -1;
        private void DoTrainingAction()
        {
            if (trainingAction == "") return;
            //handle color replacement
            //TODO handle other replacements
            if (trainingAction.IndexOf("[color]") !=-1)
                {
                int retinaWidth = GetModuleWidth("Module2DVision");
                int cL = GetNeuronValueInt("Module2DVision", retinaWidth / 2, 0);
                int cR = GetNeuronValueInt("Module2DVision", retinaWidth / 2, 1);
                if (cL != cR) return;
                string colorName = Utils.GetColorName(Utils.FromArgb(cL)).ToLower();
                trainingAction = trainingAction.Replace("[color]", colorName);
            }

            trainingCase = trainingAction.Split(':');
            if (trainingCase.Length != 3) return;
            SimulatePhrase(trainingCase[0]);
            trainingAction = "";
            actionTrainingState -= 4 - (trainingCase[0].Length - trainingCase[0].Replace(" ", "").Length);
        }
        private void CheckForTrainingResult()
        {
            if (actionTrainingState == -1) return;
            Module2DKBN nmKB = (Module2DKBN)FindModuleByType(typeof(Module2DKBN));
            if (nmKB == null) return;
            actionTrainingState--;
            if (actionTrainingState <= 4 || !na.GetNeuronAt("Train").Fired()) return;
            List<Thing> actions = nmKB.GetChildren(nmKB.Labeled("Action"));
            bool bResponded = false;
            foreach (Thing action in actions)
            {
                if (nmKB.FiredOutput(action, 2))
                {
                    if (action.Label == trainingCase[1])
                    {
                        SimulatePhrase("good");
                        bResponded = true;
                        break;
                    }
                    else
                    {
                        SimulatePhrase("no");
                        bResponded = true;
                        break;
                    }
                }
            }
            if (bResponded)
            {
                actionTrainingState = 5;
                return;
            }
            ////we "timed out" with no response to the stimulus
            //if (actionTrainingDelay == 4)
            //{
            //    SimulatePhrase("no");
            //}
        }


        private void SimulatePhrase(string Phrase)
        {
            ModuleHearWords nmWords = (ModuleHearWords)FindModuleByType(typeof(ModuleHearWords));
            if (nmWords != null)
                nmWords.HearPhrase(Phrase);
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


