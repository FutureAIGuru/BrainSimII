//
// Copyright (c) Charles Simon. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Windows;

using static BrainSimulator.Utils;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class Module2DVision : ModuleBase
    {
        public static float fieldOfView = (float)(PI / 2);
        public static float eyeOffset = .2f;

        public Module2DVision()
        {
            minWidth = 10;
        }

        //this converts the position of a retinal neuron to its angular position
        //we could make this non-linear to create a fovea some day
        public static double GetDirectionFromNeuron(float index, int numPixels)
        {
            float i1 = ((float)(numPixels - 1) / 2 - index);
            float thetaPerPixel = fieldOfView / (numPixels - 1);
            double retVal = thetaPerPixel * i1;
            return retVal;
        }

        public static float GetNeuronFromDirection(float angle, int numPixels)
        {
            float thetaPerPixel = fieldOfView / (numPixels - 1);
            float i1 = angle / thetaPerPixel;
            return (numPixels - 1) / 2 - i1;
        }

        //these arrays are used to determine if any data has changed
        //this is presently used to reduce computation but in future development can also be used to detect object motion
        private List<int> lastValuesL = null;

        private List<int> lastValuesR = null;
        private List<int> curValuesL = null;
        private List<int> curValuesR = null;
        private List<int> textureValues = null;

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            //if (depthValues.Count == 0) Initialize();
            bool retinaChanged = false;
            if (lastValuesL == null || lastValuesL.Count != mv.Width)
            {
                lastValuesL = new List<int>();
                lastValuesR = new List<int>();
                curValuesL = new List<int>();
                curValuesR = new List<int>();
                textureValues = new List<int>();
                for (int i = 0; i < mv.Width; i++)
                {
                    lastValuesL.Add(-1);
                    lastValuesR.Add(-1);
                    curValuesL.Add(-1);
                    curValuesR.Add(-1);
                    textureValues.Add(-1);
                }
                retinaChanged = true;
            }
            for (int i = 0; i < mv.Width; i++)
            {
                curValuesL[i] = mv.GetNeuronAt(i, 1).LastChargeInt;
                curValuesR[i] = mv.GetNeuronAt(i, 0).LastChargeInt;
                textureValues[i] = curValuesL[i];
            }

            for (int i = 0; i < lastValuesL.Count; i++)
            {
                if (lastValuesL[i] != curValuesL[i])
                {
                    retinaChanged = true;
                    break;
                }
                if (lastValuesR[i] != curValuesR[i])
                {
                    retinaChanged = true;
                    break;
                }
            }

            if (retinaChanged)
            {
                for (int i = 0; i < lastValuesL.Count; i++)
                {
                    lastValuesL[i] = curValuesL[i];
                    lastValuesR[i] = curValuesR[i];
                }
                FindPointsOfInterest();
                SetCenterColor();
            }
            viewChanged = 0;
        }

        private int viewChanged = 0;

        public void ViewChanged()
        {
            viewChanged = 1;
        }

        private void SetCenterColor()
        {
            int c1 = GetNeuronValueInt(null, mv.Width / 2, 0);
            int c2 = GetNeuronValueInt(null, mv.Width / 2, 1);
            ModuleUKSN nmUKS = (ModuleUKSN)FindModleu(typeof(ModuleUKSN));
            if (nmUKS != null && nmUKS.Labeled("Color") != null)
            {
                IList<Thing> colors = nmUKS.Labeled("Color").Children;
                //if (c1 != 0)
                {
                    nmUKS.Fire(nmUKS.Valued(c1, colors));
                }
                //if (c2 != 0)
                {
                    nmUKS.Fire(nmUKS.Valued(c2, colors));
                }
            }
        }

        private struct MonocularBoundary
        {
            public ColorInt colorL;
            public ColorInt colorR;
            public int direction;
            public bool changed;
        }

        private struct BinocularBoundary
        {
            public ColorInt theColor;
            public PointPlus p;
            public bool changed;
        }

        private class Area
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

        private List<MonocularBoundary> LBoundaries = new List<MonocularBoundary>();
        private List<MonocularBoundary> RBoundaries = new List<MonocularBoundary>();
        private List<BinocularBoundary> boundaries = new List<BinocularBoundary>();
        private List<Area> areas = new List<Area>();

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
                    //aa.angleFromTexture = GetAngleFromTexture(aa);
                    Segment s = new Segment() { P1 = aa.PL, P2 = aa.PR, theColor = aa.theColor, };
                    Module2DModel nmModel = (Module2DModel)FindModleu(typeof(Module2DModel));
                    if (nmModel != null)
                        aa.t = nmModel.MostLikelySegment(s);
                    areas.Add(aa);
                }
                bb1 = bb2;
            }
        }

        private float GetAngleFromTexture(Area a)
        {
            int start = (int)GetNeuronFromDirection(a.PL.Theta, mv.Width);
            int end = (int)GetNeuronFromDirection(a.PR.Theta, mv.Width);
            int first = 0;
            int last = 0;
            int lCount = 0;
            int rCount = 0;
            for (int i = start; i < end; i++)
            {
                ColorInt color = mv.GetNeuronAt(i, 0).LastChargeInt;
                if (color != a.theColor && first == 0)
                    continue;
                if (first == 0) first = i;
                if (color == System.Windows.Media.Colors.AliceBlue)
                {
                    lCount++;
                }
                else if (lCount > 0) break;
            }
            for (int i = end - 1; i >= start; i--)
            {
                ColorInt color = mv.GetNeuronAt(i, 0).LastChargeInt;
                if (color != a.theColor && last == 0)
                    continue;
                if (last == 0) last = i;
                if (color == System.Windows.Media.Colors.AliceBlue)
                {
                    rCount++;
                }
                else if (rCount > 0) break;
            }
            float retVal = 1;
            if (Abs(lCount - rCount) > 0)
            {
                retVal = (float)lCount / (float)rCount;
                retVal *= (float)(last - first) / (float)(end - start); //correction because texture is not at the end of the segment
            }
            return retVal;
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

        private void RemoveTextures(int eye)
        {
            List<int> values = curValuesL;
            if (eye == 0) values = curValuesR;

            ColorInt color1 = mv.GetNeuronAt(0, eye).LastChargeInt;
            for (int i = 1; i < mv.Width; i++)
            {
                ColorInt color = values[i];
                if (color == System.Windows.Media.Colors.AliceBlue)
                {
                    values[i] = color1;
                }
                color1 = values[i];
            }
        }

        private void FindMonocularBoundaries(int eye, List<MonocularBoundary> boundaries)
        {
            List<int> values = curValuesL;
            if (eye == 0) values = curValuesR;
            List<int> prevValues = lastValuesL;
            if (eye == 0) prevValues = lastValuesR;

            RemoveTextures(eye);

            int color1 = values[0];

            int prevColor1 = prevValues[0];
            int color2 = color1;
            int prevColor2 = prevColor1;
            for (int i = 1; i < mv.Width; i++)
            {
                color2 = values[i];
                prevColor2 = prevValues[i];
                bool changed = false;
                if (color1 != prevColor1 || color2 != prevColor2)
                    changed = true;

                if (color2 != color1)
                {
                    MonocularBoundary b = new MonocularBoundary()
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
            Module2DModel nmModel = (Module2DModel)FindModleu(typeof(Module2DModel));
            if (nmModel == null) return;

            LBoundaries.Clear();
            RBoundaries.Clear();
            FindMonocularBoundaries(0, LBoundaries);
            FindMonocularBoundaries(1, RBoundaries);
            FindBinocularBoundaries();
            FindAreasOfColor();

            //curArea.t being null means this area is not in the model...just add it
            foreach (Area curArea in areas)
            {
                if (curArea.t == null && curArea.theColor != 0)
                {
                    //add the segment to the model
                    curArea.t = nmModel.AddSegmentFromVision(curArea.PL, curArea.PR, curArea.theColor, viewChanged == 0);
                }
            }

            for (int i = 0; i < areas.Count; i++)
            {
                Area curArea = areas[i];
                if (i < areas.Count - 1)
                {
                    Area nextArea = areas[i + 1];
                    //the case of two adjacent colored boundaries means there are adjoining or occluding areas
                    //if occluding, do not update the (possibly) hidden point(s)
                    if (curArea.theColor != 0 && nextArea.theColor != 0 && curArea.t != null && nextArea.t != null)
                    //&&                         curArea.PR.R == nextArea.PL.R && curArea.PR.Theta == nextArea.PL.Theta)
                    {
                        Segment curS = Module2DModel.SegmentFromUKSThing(curArea.t);
                        Module2DModel.OrderSegment(curS);
                        Segment nextS = Module2DModel.SegmentFromUKSThing(nextArea.t);
                        Module2DModel.OrderSegment(nextS);
                        //is aa.PL in front of prevSegment (the little correction hides an occlusion problem where the endpoints nearly match
                        if (nextArea.PL.Theta > curS.P1.Theta - Rad(2) && nextArea.PL.Theta < curS.P2.Theta + Rad(2))
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
                        if (curArea.PR.Theta > nextS.P1.Theta - Rad(2) && curArea.PR.Theta < nextS.P2.Theta + Rad(2))
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
            Angle thetaA = GetDirectionFromNeuron(r - 0.5f, mv.Width);
            thetaA = PI / 2 - thetaA; //get angle to axis
            Angle thetaB = GetDirectionFromNeuron(l - 0.5f, mv.Width);
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
            Angle thetaL = (float)GetDirectionFromNeuron(r, mv.Width);
            PointPlus p2L = new PointPlus() { R = 20, Theta = thetaL };
            p2L.P = p2L.P + (Vector)p1L.P;

            Angle thetaR = (float)GetDirectionFromNeuron(l, mv.Width);
            PointPlus p2R = new PointPlus() { R = 20, Theta = thetaR };
            p2R.P = p2R.P + (Vector)p1R.P;

            Utils.FindIntersection(p1L.P, p2L.P, p1R.P, p2R.P, out bool lines_intersect, out bool segments_intersect, out Point intersection, out Point clost_p1, out Point close_p2, out double collisionAngle);
            PointPlus retVal = new PointPlus() { P = intersection };
            return retVal;
        }

        private float angularResolution = 0;

        public override void Initialize()
        {
            ClearNeurons();
            for (int i = 0; i < mv.Width; i++)
            {
                mv.GetNeuronAt(i, 0).Model = Neuron.modelType.Color;
                mv.GetNeuronAt(i, 1).Model = Neuron.modelType.Color;
            }
            mv.GetNeuronAt(0, 0).Label = "Left";
            mv.GetNeuronAt(0, 1).Label = "Right";
            mv.GetNeuronAt(mv.Width / 2, 0).Label = "  ||";
            lastValuesL = null;
            lastValuesR = null;

            angularResolution = (float)(GetDirectionFromNeuron(0, mv.Width) - GetDirectionFromNeuron(1, mv.Width));
        }
    }
}
