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
using static System.Math;
using System.Windows;
using System.Diagnostics;

namespace BrainSimulator.Modules
{
    public class Module2DVision : ModuleBase
    {
        public static float fieldOfView = (float)PI / 2;
        public static float eyeOffset = .2f;
        public override string ShortDescription { get => "Retinae"; }
        public override string LongDescription
        {
            get =>
                "This module has 2 rows of neurons representing the retinal views of the right and left eyes. It receives input from the 2DSim module " +
                "and finds points of interest which are color boundaries. Based on the difference in position of these boudaries in the two eyes, " +
                "it estimates the distance (depth perception) of the point and passes this information to the model. As depths are approximate, " +
                "it enters these ae 'possible' points.\r\n" +
                "";
        }


        public Module2DVision()
        {
            minWidth = 10;
        }

        //this converts the position of a retinal neuron to its angular position
        public static double GetDirectionOfNeuron(float index, int numPixels)
        {
            float i1 = ((float)(numPixels - 1) / 2 - index);
            float thetaPerPixel = fieldOfView / (numPixels - 1);
            double retVal = thetaPerPixel * i1;
            return retVal;
        }

        public static double toDegrees(double theta)
        {
            return 180 * theta / PI;
        }

        //these arrays are used to determine if any data has changed
        //this is presently used to reduce computation but in future development can also be used to detect object motion
        List<int> lastValuesL = null;
        List<int> lastValuesR = null;
        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            if (depthValues.Count == 0) Initialize();
            bool retinaChanged = false;
            if (lastValuesL == null || lastValuesL.Count != na.Width)
            {
                lastValuesL = new List<int>();
                lastValuesR = new List<int>();
                for (int i = 0; i < na.Width; i++)
                {
                    lastValuesL.Add(-1);
                    lastValuesR.Add(-1);
                }
                retinaChanged = true;
            }
            for (int i = 0; i < lastValuesL.Count; i++)
            {
                if (lastValuesL[i] != na.GetNeuronAt(i, 1).CurrentChargeInt) retinaChanged = true;
                lastValuesL[i] = na.GetNeuronAt(i, 1).CurrentChargeInt;
                if (lastValuesR[i] != na.GetNeuronAt(i, 0).CurrentChargeInt) retinaChanged = true;
                lastValuesR[i] = na.GetNeuronAt(i, 0).CurrentChargeInt;
            }

            if (retinaChanged)
            {
                FindPointsOfInterest();
                SetCenterColor();
            }
        }

        void SetCenterColor()
        {
            int c1 = GetNeuronValueInt(null, na.Width / 2, 0);
            int c2 = GetNeuronValueInt(null, na.Width / 2, 1);
            Module2DKBN nmKB = (Module2DKBN)FindModuleByType(typeof(Module2DKBN));
            if (nmKB != null && nmKB.Labeled("Color") != null)
            {
                List<Thing> colors = nmKB.Labeled("Color").Children;
                //if (c1 != 0)
                {
                    nmKB.Fire(nmKB.Valued(c1, colors));
                }
                //if (c2 != 0)
                {
                    nmKB.Fire(nmKB.Valued(c2, colors));
                }
            }
        }

        struct Boundary
        {
            public int colorL;
            public int colorR;
            public int direction;
        }
        List<Boundary> LBoundaries = new List<Boundary>();
        List<Boundary> RBoundaries = new List<Boundary>();

        private void FindBoundaries(int eye, List<Boundary> boundaries)
        {
            int color1 = na.GetNeuronAt(0, eye).CurrentChargeInt;
            int color2 = color1;
            for (int i = 1; i < na.Width; i++)
            {
                color2 = na.GetNeuronAt(i, eye).CurrentChargeInt;
                if (color2 != color1)
                {
                    Boundary b = new Boundary()
                    {
                        colorL = color1,
                        colorR = color2,
                        direction = i
                    };
                    boundaries.Add(b);
                    color1 = color2;
                }
            }
        }

        private void FindPointsOfInterest()
        {
            Module2DModel nmModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));
            if (nmModel == null) return;

            LBoundaries.Clear();
            RBoundaries.Clear();
            FindBoundaries(0, LBoundaries);
            FindBoundaries(1, RBoundaries);
            //PointPlus prevPoint = null;

            //find matching areas within the visual field
            for (int i = 0; i < LBoundaries.Count-1; i++)
            {
                //the color of area is the color to the right of the left boundary
                //TODO this hack requires that a visible area is surround by black to eliminate occlusion problems
                if (LBoundaries[i].colorL != 0) continue;
                if (LBoundaries[i+1].colorR != 0) continue;

                int areaColorL = LBoundaries[i].colorR;
                for (int j = 0; j < RBoundaries.Count-1; j++)
                {
                    if (RBoundaries[j].colorL != 0) continue;
                    if (RBoundaries[j + 1].colorR != 0) continue;
                    int areaColorR = RBoundaries[j].colorR;
                    //does the same boundary appear in both eyes? 
                    //TODO: This doesn't handle the case where more than one area of the same color is in the visual field
                    //if (areaColorL == areaColorR && areaColorL != 0)
                        if (areaColorL == areaColorR && areaColorL != 0)
                        {
                            int l1 = LBoundaries[i].direction;
                        int r1 = RBoundaries[j].direction;
                        int l2 = LBoundaries[i + 1].direction;
                        int r2 = RBoundaries[j+1].direction;
                        PointPlus leftPoint = FindDepth(LBoundaries[i].direction, RBoundaries[j].direction);
                        PointPlus leftPointError = FindDepth(LBoundaries[i].direction-1, RBoundaries[j].direction);
                        float error = leftPointError.R- leftPoint.R ;
                        if (error < 0) break;
                        leftPoint.Conf = error;

                        PointPlus rightPoint = FindDepth(LBoundaries[i + 1].direction, RBoundaries[j + 1].direction);
                        PointPlus rightPointError = FindDepth(LBoundaries[i + 1].direction-1, RBoundaries[j + 1].direction);
                        error = rightPointError.R - rightPoint.R;
                        if (error < 0) break;
                        rightPoint.Conf = error;

                        nmModel.AddSegmentFromVision(leftPoint,rightPoint,areaColorR);
                    }
                }
            }
        }

        private PointPlus FindDepth(int l, int r)
        {
            //calculation using trig
            Angle thetaA = GetDirectionOfNeuron(r - 0.5f, na.Width);
            thetaA = PI / 2 - thetaA; //get angle to axis
            Angle thetaB = GetDirectionOfNeuron(l - 0.5f, na.Width);
            thetaB = PI / 2 - thetaB;
            thetaB = PI - thetaB; //to get an inside angle
            Angle thetaC = PI - thetaA - thetaB;
            double A = Sin(thetaA) * (2 * eyeOffset) / Sin(thetaC);
            thetaA -= PI / 2;
            PointPlus P = new PointPlus { Theta = (float)-thetaA, R = (float)A };  //relative to left eye
            P.Y -= eyeOffset; //correct to be centered between eyes

            //alternate using vectors
            PointPlus p1L = new PointPlus() { Y = -eyeOffset, X = 0 };
            PointPlus p1R = new PointPlus() { Y = +eyeOffset, X = 0 };
            Angle thetaL = (float)GetDirectionOfNeuron(r, na.Width);
            PointPlus p2L = new PointPlus() { R = 20, Theta = thetaL };
            p2L.P = p2L.P + (Vector)p1L.P;

            Angle thetaR = (float)GetDirectionOfNeuron(l, na.Width);
            PointPlus p2R = new PointPlus() { R = 20, Theta = thetaR };
            p2R.P = p2R.P + (Vector)p1R.P;

            Utils.FindIntersection(p1L.P, p2L.P, p1R.P, p2R.P, out bool lines_intersect, out bool segments_intersect, out Point intersection, out Point clost_p1, out Point close_p2, out double collisionAngle);
            PointPlus retVal = new PointPlus() { P = intersection };
            return retVal;
        }

        float angularResolution = 0;
        List<float> depthValues = new List<float>();
        public override void Initialize()
        {
            for (int i = 0; i < na.Width; i++)
            {
                na.GetNeuronAt(i, 0).Model = Neuron.modelType.Color;
                na.GetNeuronAt(i, 1).Model = Neuron.modelType.Color;
            }
            na.GetNeuronAt(0, 0).Label = "Left";
            na.GetNeuronAt(0, 1).Label = "Rigit";
            na.GetNeuronAt(na.Width / 2, 0).Label = "  ||";
            lastValuesL = null;
            lastValuesR = null;

            angularResolution = (float)(GetDirectionOfNeuron(0, na.Width) - GetDirectionOfNeuron(1, na.Width));
            depthValues.Clear();
            for (int i = 1; i < na.Width / 4; i++)
                depthValues.Add(FindDepth(na.Width / 2, na.Width / 2 + i).R);
        }
    }
}
