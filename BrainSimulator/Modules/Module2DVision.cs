//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Collections.Generic;
using System.Windows;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class Module2DVision : ModuleBase
    {
        public static float fieldOfView = (float)(PI / 2);
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


        //these arrays are used to determine if any data has changed
        //this is presently used to reduce computation but in future development can also be used to detect object motion
        List<int> lastValuesL = null;
        List<int> lastValuesR = null;
        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            //if (depthValues.Count == 0) Initialize();
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
                if (lastValuesR[i] != na.GetNeuronAt(i, 0).CurrentChargeInt) retinaChanged = true;
            }

            if (retinaChanged)
            {
                FindPointsOfInterest();
                SetCenterColor();

                for (int i = 0; i < lastValuesL.Count; i++)
                {
                    lastValuesL[i] = na.GetNeuronAt(i, 1).CurrentChargeInt;
                    lastValuesR[i] = na.GetNeuronAt(i, 0).CurrentChargeInt;
                }
            }



            viewChanged = 0;
        }

        int viewChanged = 0;
        public void ViewChanged()
        {
            viewChanged = 1;
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

        struct monocularBoundary
        {
            public ColorInt colorL;
            public ColorInt colorR;
            public int direction;
            public bool changed;
        }
        struct BinocularBoundary
        {
            public ColorInt theColor;
            public PointPlus p;
            public bool changed;
        }
        class Area
        {
            public PointPlus PL;
            public PointPlus PR;
            public ColorInt theColor;
            public bool lChanged; //has this point changed from the previous firing?
            public bool RChanged;
            public Thing t;
            public bool PLHidden;
            public bool PRHidden;

        }

        List<monocularBoundary> LBoundaries = new List<monocularBoundary>();
        List<monocularBoundary> RBoundaries = new List<monocularBoundary>();
        List<BinocularBoundary> boundaries = new List<BinocularBoundary>();
        List<Area> areas = new List<Area>();

        //an area must appear in both eyes to count
        //partial areas as the edge of the field of view will have only one endpoint
        //this relies on having only one area per color in the visual field
        private void FindAreasOfColor()
        {
            areas.Clear();
            if (boundaries.Count == 0) return;
            BinocularBoundary bb1 = boundaries[0];
            for (int i = 1; i < boundaries.Count; i++)
            {
                BinocularBoundary bb2 = boundaries[i];
                if (bb1.theColor == bb2.theColor)
                {
                    Area aa = new Area()
                    {
                        PL = bb1.p,
                        PR = bb2.p,
                        lChanged = bb1.changed,
                        RChanged = bb2.changed,
                        theColor = bb1.theColor,
                    };
                    Segment s = new Segment() { P1 = aa.PL, P2 = aa.PR, theColor = aa.theColor, };
                    Module2DModel nmModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));
                    if (nmModel != null) 
                        aa.t = nmModel.MostLikelySegment(s);
                    areas.Add(aa);
                }
                bb1 = bb2;
            }

        }
        private void FindBinocularBoundaries()
        {
            boundaries.Clear();
            int start = 0;
            for (int i = 0; i < LBoundaries.Count; i++)
            {
                for (int j = start; j < RBoundaries.Count; j++)
                {
                    ColorInt cLL = LBoundaries[i].colorL;
                    ColorInt cLR = LBoundaries[i].colorR;
                    ColorInt cRL = RBoundaries[j].colorL;
                    ColorInt cRR = RBoundaries[j].colorR;
                    if (cLL == cRL || cLR == cRR)
                    {
                        //find distance and error
                        int l1 = LBoundaries[i].direction;
                        int r1 = RBoundaries[j].direction;
                        PointPlus leftPoint = FindDepth(l1, r1);
                        PointPlus leftPointError = FindDepth(l1 - 1, r1);
                        float error = leftPointError.R - leftPoint.R;
                        if (error < 0) break;
                        leftPoint.Conf = error;

                        PointPlus pp = new PointPlus() { R = leftPoint.R, Theta = leftPoint.Theta, Conf = leftPoint.Conf };
                        //if both sides of the point match...this creates two boundaries, one for each color
                        if (cLL == cRL)
                        {
                            BinocularBoundary bb = new BinocularBoundary()
                            {
                                p = leftPoint,
                                theColor = cLL,
                                changed = LBoundaries[i].changed,
                            };
                            boundaries.Add(bb);
                        }
                        if (cLR == cRR)
                        {
                            BinocularBoundary bb = new BinocularBoundary()
                            {
                                p = pp,
                                theColor = cLR,
                                changed = LBoundaries[i].changed,
                            };
                            boundaries.Add(bb);
                        }
                        start = j;
                        break;
                    }
                }
            }
        }

        private void FindMonocularBoundaries(int eye, List<monocularBoundary> boundaries)
        {
            List<int> prevValues = lastValuesL;
            if (eye == 0) prevValues = lastValuesR;

            int color1 = na.GetNeuronAt(0, eye).CurrentChargeInt;
            int prevColor1 = prevValues[0];
            int color2 = color1;
            int prevColor2 = prevColor1;
            for (int i = 1; i < na.Width; i++)
            {
                color2 = na.GetNeuronAt(i, eye).CurrentChargeInt;
                prevColor2 = prevValues[i];
                bool changed = false;
                if (color1 != prevColor1 || color2 != prevColor2)
                    changed = true;

                if (color2 != color1)
                {
                    monocularBoundary b = new monocularBoundary()
                    {
                        colorL = color1,
                        colorR = color2,
                        direction = i,
                        changed = changed,
                    };
                    boundaries.Add(b);
                    color1 = color2;
                }
                prevColor1 = prevColor2;
            }
        }

        private void FindPointsOfInterest()
        {
            Module2DModel nmModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));
            if (nmModel == null) return;

            LBoundaries.Clear();
            RBoundaries.Clear();
            FindMonocularBoundaries(0, LBoundaries);
            FindMonocularBoundaries(1, RBoundaries);
            FindBinocularBoundaries();
            FindAreasOfColor();

            foreach (Area curArea in areas)
            {
                if (curArea.t == null && curArea.theColor != 0)
                {
                    //add the segment to the model
                    curArea.t= nmModel.AddSegmentFromVision(curArea.PL, curArea.PR, curArea.theColor, viewChanged == 0);
                }
            }

            for (int i = 0; i < areas.Count; i++)
            {
                Area curArea = areas[i];
                if (i < areas.Count - 1)
                {
                    Area nextArea = areas[i+1];
                    //the case of two adjacent colored boundaries means there are adjoining or occluding areas
                    //if occluding, do not update the (possibly) hidden point(s)
                    if (curArea.theColor != 0 && nextArea.theColor != 0 && curArea.t != null && nextArea.t != null)
                        //&&                         curArea.PR.R == nextArea.PL.R && curArea.PR.Theta == nextArea.PL.Theta)
                    {
                        Segment curS = Module2DModel.SegmentFromKBThing(curArea.t);
                        Segment nextS = Module2DModel.SegmentFromKBThing(nextArea.t);
                        //is aa.PL in front of prevSegment
                        if (nextArea.PL.Theta < curS.P1.Theta && nextArea.PL.Theta > curS.P2.Theta)
                        {
                            float segDistAtPoint = curS.P1.R;
                            float dr1 = (curS.P2.R - curS.P1.R);
                            float dtp = (curS.P1.Theta - nextArea.PL.Theta);
                            float dtt = (curS.P1.Theta - curS.P2.Theta);
                            segDistAtPoint += dr1 * dtp / dtt;
                            if (nextArea.PL.R < segDistAtPoint)
                            {
                                curArea.PRHidden = true;
                            }
                        }
                        //is prevArea.PR in front of aaSegment
                        if (curArea.PR.Theta < nextS.P1.Theta && curArea.PR.Theta > nextS.P2.Theta)
                        {
                            float segDistAtPoint = nextS.P1.R + (nextS.P2.R - nextS.P1.R) * (nextS.P1.Theta - curArea.PR.Theta) / (nextS.P1.Theta - nextS.P2.Theta);
                            if (curArea.PR.R < segDistAtPoint)
                            {
                                nextArea.PLHidden = true;
                            }
                        }
                    }
                }
            }


            //check for single boundaries and update them in the model
            foreach (Area curArea in areas)
            {
                //if the model data already exists, update points
                if (curArea.t != null)
                {
                    if (!curArea.PLHidden)
                        nmModel.UpdateEndpointFromVision(curArea.PL, curArea.theColor, viewChanged == 0);
                    if (!curArea.PRHidden)
                        nmModel.UpdateEndpointFromVision(curArea.PR, curArea.theColor, viewChanged == 0);
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
        public override void Initialize()
        {
            ClearNeurons();
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
        }
    }
}
