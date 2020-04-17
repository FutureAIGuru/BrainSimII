//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using static System.Math;
using static BrainSimulator.Utils;


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
            public bool isMobile;
            public Vector motion = new Vector(0,0);
            public float rotation = 0;
        }
        public List<physObject> objects = new List<physObject>();

        public override string ShortDescription { get => "A simulated 2D environment with obstacles"; }
        public override string LongDescription
        {
            get =>
                "This module uses no neurons of its own but fires neurons in various sensory modules if they are in the network. It has methods (Move and Turn and potentially others " +
                "which can be called by other modules to move its point of view around the simulation. " +
                "Shift-mouse wheel can zoom the display and Shift-left mouse button can drag (pan). " +
                "Right-clicking in the dialog box can direct the entity to that location." +
                ""
                ;
        }

        [XmlIgnore]
        public List<Point> entityTrack = new List<Point>();
        [XmlIgnore]
        public List<physObject> currentView0 = new List<physObject>();
        [XmlIgnore]
        public List<physObject> currentView1 = new List<physObject>();
        [XmlIgnore]
        public int inMotion = 0; //+1 move objects fwd, -1 reverse

        //where the arm tips are relative to self
        public Vector[] armRelative = { new Vector(.5, .5), new Vector(.5, -.5) };

        [XmlIgnore]
        public Point[] armActual = { new Point(0, 0) };

        //where we are in the world
        public Point entityPosition = new Point(0, 0);
        public float entityDirection1 = (float)PI / 2;

        //the size of the universe
        public double boundarySize = 5;

        [XmlIgnore]
        public float bodyRadius = .2f;

        Random rand = new Random();

        public Module2DSim()
        {
        }

        float armTheta1 = 0;
        float armTheta2 = 0;

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable

            ModuleView naArmL = theNeuronArray.FindAreaByLabel("ModuleArmL");
            ModuleView naArmR = theNeuronArray.FindAreaByLabel("ModuleArmR");

            if (naArmL == null || naArmR == null)
            {
                //random arm motion if no arm modules are loaded
                armRelative = new Vector[] {
                new Vector(.5, .5) + new Vector(.25*rand.NextDouble()*Cos(armTheta1),.25*Sin(armTheta1)),
                new Vector(.5, -.5)+ new Vector(.25*rand.NextDouble()*Cos(armTheta2),.25*Sin(armTheta2))
            };
                armTheta1 += .3f;
                armTheta2 += .35f;
            }
            else
            {
                float armLX = GetNeuronValue("ModuleArmL", "X");
                float armLY = GetNeuronValue("ModuleArmL", "Y");
                float armRX = GetNeuronValue("ModuleArmR", "X");
                float armRY = GetNeuronValue("ModuleArmR", "Y");
                if (armLX != armRelative[0].X ||
                    armLY != armRelative[0].Y ||
                    armRX != armRelative[1].X ||
                    armRY != armRelative[1].Y
                    )
                {
                    armRelative = new Vector[] {
                        new Vector(armLX,armLY),
                        new Vector(armRX,armRY)
                    };
                }
            }
            MoveObjects();

            HandleTouch();
            HandleVision();
            HandleAroma();

            CreateRandomTrainingCase();
            DoTrainingAction();
            CheckForTrainingResult();

            UpdateDialog();
        }

        private void MoveObjects()
        {
            foreach(physObject ph in objects)
            {
                ph.P1 += ph.motion*inMotion;
                ph.P2 += ph.motion*inMotion;
            }
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
            entityDirection1 -= (float)theta;
            if (entityDirection1 > PI) entityDirection1 -= (float)PI * 2;
            if (entityDirection1 < -PI) entityDirection1 += (float)PI * 2;
        }

        //returning true said there no collision and it is OK to move there...in the event of a collision, the move is cancelled
        public bool Move(float motion) //move fwd +, rev -
        {
            Point newPosition = new Point()
            {
                X = entityPosition.X + motion * Cos(entityDirection1),
                Y = entityPosition.Y + motion * Sin(entityDirection1)
            };

            //check for collisions  collision can impede motion
            //collision is actual intersection of desired motion path and obstacle as opposed to a touch
            //which is an intersection between an arm and does not impede motion
            bool collision = CheckForCollisions(newPosition);

            //update position and add track...only if moving is OK
            Vector v1 = newPosition - entityPosition;
            if (!collision)
            {
                entityPosition = newPosition;
                entityTrack.Add(entityPosition);
            }
            return !collision;
        }

        //a collision is the intersection of the desired newPosition and an obstacle
        private bool CheckForCollisions(Point newPosition)
        {
            bool retVal = false;
            for (int i = 0; i < objects.Count; i++)
            {
                Point P1 = objects[i].P1;
                Point P2 = objects[i].P2;
                physObject ph = objects[i];
                double dist = Utils.FindDistanceToSegment(newPosition, ph.P1, ph.P2, out Point closest);
                if (dist < bodyRadius)
                {
                    PointPlus collPt = new PointPlus { P = (Point)(closest - newPosition) };
                    if (!objects[i].isMobile) //collision
                    {
                        SetNeuronValue("ModuleBehavior", "Coll", 1);
                        collPt.Theta -= entityDirection1;
                        SetNeuronValue("ModuleBehavior", "CollAngle", collPt.Theta);
                        retVal = true; ;
                    }
                    else //move the object
                    {
                        float distToMoveObject = bodyRadius - (float)dist;
                        PointPlus motion = new PointPlus() { P = collPt.P };
                        motion.R = distToMoveObject;
                        Point oldPoint1 = new Point(ph.P1.X, ph.P1.Y);
                        Point oldPoint2 = new Point(ph.P2.X, ph.P2.Y);

                        MovePhysObject(ph, closest, motion);
                        //TODO check for collisions with this object and other objects
                        for (int j = 0; j < objects.Count; j++)
                        {
                            if (j == i) continue;
                            physObject ph2 = objects[j];
                            if (!ph2.isMobile) continue;
                            FindIntersection(ph.P1, ph.P2, ph2.P1, ph2.P2, out bool lines_intersect, out bool segments_intersect,
                                out Point intersection, out Point close_p1, out Point close_p2, out double collisionAngle);
                            if (segments_intersect)
                            {
                                PointPlus endMotion = new PointPlus();
                                float dist1 = (float)((Vector)(intersection - (Vector)ph.P1)).Length;
                                float dist2 = (float)((Vector)(intersection - (Vector)ph.P2)).Length;
                                if (dist1 < dist2)
                                {
                                    endMotion.P = ph.P1 - (Vector)oldPoint1;
                                }
                                else 
                                {
                                    endMotion.P = ph.P2 - (Vector)oldPoint2;
                                }

                                MovePhysObject(objects[j], intersection, endMotion);
                            }
                        }

                    }
                }
            }
            return retVal;
        }

        private static void MovePhysObject(physObject ph, Point closest, PointPlus motion)
        {
            ph.P1 = ph.P1 + motion.V;
            ph.P2 = ph.P2 + motion.V;

            //handle rotation if object not hit in the middle
            //we know that our point "closest" moves with "motion"...rotation should be about "closest"
            Segment s = new Segment
            {
                P1 = new PointPlus() { P = ph.P1 },
                P2 = new PointPlus() { P = ph.P2 }
            };
            PointPlus contactPoint = new PointPlus() { P = closest };
            PointPlus offset = new PointPlus() { P = (Point)(contactPoint.V - s.MidPoint().V) };

            double cross = Vector.CrossProduct(offset.V, motion.V);

            float rotation = 10 * Sign(cross);

            float rotationRatio = (float)(offset.V.Length / s.Length() * 2);

            PointPlus V1 = new PointPlus() { P = (Point)(s.P1.P - contactPoint.P) };
            PointPlus V2 = new PointPlus() { P = (Point)(s.P2.P - contactPoint.P) };

            V1.Theta += Rad(rotation * rotationRatio);
            V2.Theta += Rad(rotation * rotationRatio);

            ph.P1 = (Point)V1.V + contactPoint.V;
            ph.P2 = (Point)V2.V + contactPoint.V;
        }

        //aroma is a field strength at a given position
        private void HandleAroma()
        {
            ModuleView naSmell = theNeuronArray.FindAreaByLabel("Module2DSmell");
            //find the aroma value
            if (naSmell != null)
            {
                double sumGreen = GetColorWeightAtPoint(entityPosition, Colors.Green);
                for (int i = 0; i < armActual.Length; i++)
                {
                    sumGreen = GetColorWeightAtPoint(armActual[i], Colors.Green);
                    SetNeuronValue("Module2DSmell", i, 0, (float)sumGreen);
                }
            }
        }


        //touch is the intersection of an arm with an obstacle
        public void HandleTouch()
        {
            //arm[0] is left [1] is right
            armActual = new Point[armRelative.Length];
            //is there an object intersecting the arm?
            for (int i = 0; i < armRelative.Length; i++)
            {
                PointPlus pv = new PointPlus { P = (Point)armRelative[i] };
                pv.Theta = entityDirection1 + pv.Theta;
                Point armPositionAbs = entityPosition + (Vector)pv.P;
                HandleTouch(armPositionAbs, i);
            }
        }

        private void HandleTouch(Point armPositionAbs, int index)
        {
            armActual[index] = armPositionAbs;
            SetNeuronValue("Module2DTouch", 0, index, 0);
            SetNeuronValue("Module2DTouch", 8, index, 2);

            //this all works in absolute coordinates
            for (int i = 0; i < objects.Count; i++)
            {
                Point P1 = objects[i].P1;
                Point P2 = objects[i].P2;
                physObject ph = objects[i];
                Utils.FindIntersection(P1, P2, entityPosition, armPositionAbs, out bool linesintersect, out bool segments_intersect,
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

                    PointPlus armPositionRel = new PointPlus { P = (Point)(Intersection - entityPosition) };
                    armPositionRel.Theta = armPositionRel.Theta - entityDirection1;

                    float[] neuronValues = new float[9];
                    //everything from here out is  coordinates relative to self
                    //neurons:  0:touch   1:armAngle  2:armDistance 3: sensedLineAngle 4: conf1 5: len1 6: conf2 7: len2 8: Release
                    neuronValues[0] = 1;
                    neuronValues[1] = armPositionRel.Theta;
                    neuronValues[2] = armPositionRel.R;
                    neuronValues[3] = (float)collisionAngle;
                    neuronValues[4] = (float)p1IsEndpt;
                    neuronValues[5] = (float)l1;
                    neuronValues[6] = (float)p2IsEndpt;
                    neuronValues[7] = (float)l2;
                    neuronValues[8] = 0;
                    SetNeuronVector("Module2DTouch", true, index, neuronValues);

                    armActual[index] = Intersection;
                    break;
                }
            }
        }

        //do offsets to handle two eyes
        private void HandleVision()
        {
            Point oldCamerPosition = entityPosition;
            Double offsetDirection = entityDirection1 + PI / 2;
            Vector offset = new Vector(Cos(offsetDirection), Sin(offsetDirection));
            offset = Vector.Multiply(Module2DVision.eyeOffset, offset);
            entityPosition += offset;
            HandleVision(0);

            entityPosition = oldCamerPosition;
            offsetDirection = entityDirection1 - PI / 2;
            offset = new Vector(Cos(offsetDirection), Sin(offsetDirection));
            offset = Vector.Multiply(Module2DVision.eyeOffset, offset);
            entityPosition += offset;
            HandleVision(1);
            entityPosition = oldCamerPosition;
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
                theta = entityDirection1 + theta;
                //create a segment from the view direction for this pixel (length 100 assumes the size of the universe)
                Point p2 = entityPosition + new Vector(Cos(theta) * 100.0, Sin(theta) * 100.0);
                Color theColor = Colors.Pink;
                double closestDistance = 20;
                for (int j = 0; j < objects.Count; j++)
                {
                    Utils.FindIntersection(entityPosition, p2, objects[j].P1, objects[j].P2,
                        out bool lines_intersect, out bool segments_intersect,
                        out Point intersection, out Point close_p1, out Point closep2, out double collisionAngle);
                    if (segments_intersect)
                    {
                        double distance = Point.Subtract(intersection, entityPosition).Length;
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            theColor = objects[j].theColor;
                        }
                    }
                }
                pixels[i] = Utils.ColorToInt(theColor);
                Point p3 = entityPosition + new Vector(Cos(theta), Sin(theta));
                if (row == 0)
                    currentView0.Add(new physObject() { P1 = p3, P2 = entityPosition, theColor = theColor });
                //currentView0.Add(new physObject() { P1 = p3, P2 = new Point(0, 0), theColor = theColor });
                else
                    currentView1.Add(new physObject() { P1 = p3, P2 = entityPosition, theColor = theColor });
            }
            SetNeuronVector("Module2DVision", true, row, pixels);
        }

        //Training samples are triples... a spoken phrase, an anction, and an outcome (separated by colons)
        //TODO delete the outcome? It is always positive by implication
        string[] TrainingSamples = new string[]
        {
            //what color is this:Say:Positive",
            //this color is black:Say:Positive"
            //"what is this:Say:Positive",
            //"go:Go:Positive",
            //"stop:Stop:Positive",
            //"say:Say:Positive",
            //"sallie:Attn:Positive",
            //"turn around:UTurn:Positive",
            //"turn around:UTurn:Positive",
            //"turn left:LTurn:Positive",
            //"turn right:RTurn:Positive",
            "This is [color]:NoAction:Positive",
            "This is [color]:NoAction:Positive"
,        };
        int index = 1;
        private void CreateRandomTrainingCase()
        {
            if (GetNeuronValue(null, "Actions") == 1 && actionTrainingState < 0)
            {
                int index = (int)(rand.NextDouble() * TrainingSamples.Length);
                trainingAction = TrainingSamples[index];
                actionTrainingState = 15;
                //index = index == 0 ? 1 : 0;
            }
        }

        string trainingAction = ""; //gets the training case from the array of training samples
        string[] trainingCase;
        int actionTrainingState = -1;

        // -1: idle  15: just started 10:phrase out completed (max phrase is 5 wds)
        private void DoTrainingAction()
        {
            if (trainingAction == "") return;
            //handle color replacement
            //TODO handle other replacements
            if (trainingAction.IndexOf("[color]") != -1)
            {
                int retinaWidth = GetModuleWidth("Module2DVision");
                int cL = GetNeuronValueInt("Module2DVision", retinaWidth / 2, 0);
                int cR = GetNeuronValueInt("Module2DVision", retinaWidth / 2, 1);
                if (cL != cR) return;
                string colorName = Utils.GetColorName(Utils.IntToColor(cL)).ToLower();
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
                if (nmKB.Fired(action, 2, false))
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
                    Point P1 = objects[i].P1;
                    Point P2 = objects[i].P2;
                    double distance = Utils.FindDistanceToSegment(point, P1, P2, out Point closest);
                    sum += 1 / (distance * distance);
                }
            }
            return sum;
        }

        //for debugging it is handy to bypass the exploration to establish the internal model...just set it
        public void SetModel()
        {
            MainWindow.SuspendEngine();
            Initialize();
            Module2DModel nmModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));
            //clear out any existing 
            nmModel.Initialize();

            for (int i = 0; i < objects.Count; i++)
            {
                PointPlus P1 = new PointPlus() { P = objects[i].P1 };
                PointPlus P2 = new PointPlus() { P = objects[i].P2 };
                int theColor = Utils.ColorToInt(objects[i].theColor);
                nmModel.AddSegmentFromVision(P1, P2, theColor,false);
            }
            MainWindow.ResumeEngine();
        }
        public override void Initialize()
        {
            TrainingSamples = new string[]
            {
            "what color is this:Say:Positive",
            "what is this:Say:Positive",
            "go:Go:Positive",
            "stop:Stop:Positive",
            //"sallie:Attn:Positive",
            //"turn around:UTurn:Positive",
            "turn left:LTurn:Positive",
            "turn right:RTurn:Positive",
            "This is [color]:NoAction:Positive",
            "[color]:NoAction:Positive"
        };


            index = 0;
            entityTrack.Clear();
            entityPosition = new Point(0, 0);
            entityTrack.Add(entityPosition);
            entityDirection1 = 0;

            na.GetNeuronAt(0).Label = "Actions";
            na.GetNeuronAt(1).Label = "Colors";
            na.GetNeuronAt(2).Label = "Train";

            armRelative = new Vector[] { new Vector(.4, .4), new Vector(.4, -.4) };
            armActual = new Point[2];
            for (int i = 0; i < armRelative.Length; i++)
                armActual[i] = entityPosition + armRelative[i];

            objects.Clear();

            //build a pen to keep the entity inside
            objects.Add(new physObject() { P1 = new Point(boundarySize, boundarySize), P2 = new Point(boundarySize, -boundarySize), theColor = Colors.Black, isMobile = false });
            objects.Add(new physObject() { P1 = new Point(boundarySize, -boundarySize), P2 = new Point(-boundarySize, -boundarySize), theColor = Colors.Black, isMobile = false });
            objects.Add(new physObject() { P1 = new Point(-boundarySize, -boundarySize), P2 = new Point(-boundarySize, boundarySize), theColor = Colors.Black, isMobile = false });
            objects.Add(new physObject() { P1 = new Point(-boundarySize, boundarySize), P2 = new Point(boundarySize, boundarySize), theColor = Colors.Black, isMobile = false });

            void TransformPoint(ref int x, ref int y)
            {
                //the middle of the area
                int mx = na.Width / 2; int my = na.Height / 2;
                x -= mx;
                y -= my;
                int temp = x;
                x = y;
                y = temp; ;

                y = -y;
                x = -x;
            }

            int colorCount = 0;
            //Color[] theColors = new Color[] {
            //    Colors.Red,Colors.Blue,Colors.Orange,Colors.Magenta,Colors.Pink,
            //    Colors.Lime,Colors.MediumAquamarine,Colors.LightBlue,Colors.Yellow,Colors.PeachPuff,
            //    Colors.GreenYellow,Colors.Cyan,Colors.DarkBlue,Colors.DarkGreen,Colors.LawnGreen,
            //    Colors.BlueViolet,Colors.DarkRed,Colors.DarkSeaGreen,Colors.LightCoral,Colors.Lavender,Colors.DarkOrange};

            Color currentColor = Colors.Blue;

            List<Color> theColors = new List<Color>();
            theColors.Add(Colors.Red);
            theColors.Add(Colors.Lime);
            theColors.Add(Colors.Blue);
            theColors.Add(Colors.Magenta);
            theColors.Add(Colors.Cyan);
            theColors.Add(Colors.Orange);
            theColors.Add(Colors.Purple);
            theColors.Add(Colors.Maroon);
            theColors.Add(Colors.Green);
            theColors.Add(Colors.Crimson);
            PropertyInfo[] p1 = typeof(Colors).GetProperties();
            int count = 0;
            foreach (PropertyInfo p in p1)
            {
                if (count++ > 7)
                {
                    Color c = (Color)p.GetValue(null);
                    if (c != Colors.White && c != Colors.Black && c != Colors.AliceBlue && c != Colors.GhostWhite && c != Colors.Honeydew
                        && c != Colors.Azure && c != Colors.Beige && c != Colors.Bisque && c != Colors.Cornsilk && c != Colors.AntiqueWhite
                        && c != Colors.Cyan && c != Colors.FloralWhite && c != Colors.Gray && c != Colors.Cyan)
                        if (!theColors.Contains(c))
                            theColors.Add(c);
                }
            }

            foreach (Neuron n in na.Neurons())
            {
                na.GetNeuronLocation(n, out int x1, out int y1);
                TransformPoint(ref x1, ref y1);
                foreach (Synapse s in n.Synapses)
                {
                    if (s.TargetNeuron != n.Id)
                    {
                        na.GetNeuronLocation(s.TargetNeuron, out int x2, out int y2);
                        TransformPoint(ref x2, ref y2);
                        physObject newObject = new physObject
                        {
                            P1 = new Point(x1 - 0.5, y1 - 0.5),
                            P2 = new Point(x2 - 0.5, y2 - 0.5),
                            theColor = theColors[colorCount],
                            Aroma = -1,
                            Temperature = 10,
                            isMobile = (s.Weight == 1) ? true : false,
                        };
                        if (s.Weight < 1)
                            newObject.motion = new Vector(s.Weight-.5f, 0);
                        if (s.Weight < 0)
                            newObject.motion = new Vector(0,-.5f-s.Weight);
                        objects.Add(newObject);
                        colorCount = (colorCount + 1) % theColors.Count;
                        int currentColorInt = Utils.ColorToInt(currentColor);
                        currentColorInt--;
                        currentColor = Utils.IntToColor(currentColorInt);
                    }
                }
            }
            HandleVision();
            UpdateDialog();
        }

    }
}


