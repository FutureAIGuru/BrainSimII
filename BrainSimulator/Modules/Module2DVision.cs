﻿//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BrainSimulator
{
    public class Module2DVision : ModuleBase
    {
        public static float fieldOfView = (float)Math.PI / 3;
        public static float eyeOffset = .2f;

        public static double GetDirectionOfNeuron(float index, int numPixels)
        {
            float i1 = ((float)numPixels / 2 - index) - .5f;
            float thetaPerPixel = fieldOfView / (numPixels - 1);
            double a = toDegrees(thetaPerPixel);
            double retVal = thetaPerPixel * i1;
            a = toDegrees(retVal);
            return retVal;
        }
        public static double toDegrees(double theta)
        {
            return 180 * theta / Math.PI;
        }

        List<int> lastValuesL = null;
        List<int> lastValuesR = null;
        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            if (lastValuesL == null || lastValuesL.Count != na.Width)
            {
                lastValuesL = new List<int>();
                lastValuesR = new List<int>();
                for (int i = 0; i < na.Width; i++)
                {
                    lastValuesL.Add(-1);
                    lastValuesR.Add(-1);
                }
            }
            bool retinaChanged = false;
            for (int i = 0; i < lastValuesL.Count; i++)
            {
                if (lastValuesL[i] != na.GetNeuronAt(i, 1).CurrentChargeInt) retinaChanged = true;
                lastValuesL[i] = na.GetNeuronAt(i, 1).CurrentChargeInt;
                if (lastValuesR[i] != na.GetNeuronAt(i, 0).CurrentChargeInt) retinaChanged = true;
                lastValuesR[i] = na.GetNeuronAt(i, 0).CurrentChargeInt;
            }
            if (!retinaChanged) return;

            Module2DModel nmModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));
            if (nmModel != null)
            {
                int start = 0;
                while (start != -1)
                    start = GetPointsWithDepth(nmModel, start);
                nmModel.MarkVisibleObjects();
            }

            //for (int i = 0; i < na.Width; i++)
            //{
            //    Color theColor = Utils.FromArgb(na.GetNeuronAt(i, 0).CurrentChargeInt);
            //    if (theColor != Colors.Black)
            //    {
            //        naModel.SetColor((float)GetDirectionOfNeuron(i), theColor);
            //    }
            //}


        }

        private int GetPointsWithDepth(Module2DModel naModel, int start)
        {
            //find a color transition in left eye
            int color1 = na.GetNeuronAt(start, 1).CurrentChargeInt;
            int color2 = color1;
            int l;
            for (l = start + 1; l < na.Width; l++)
            {
                color2 = na.GetNeuronAt(l, 1).CurrentChargeInt;
                if (color2 != color1) break;
            }
            if (color1 != color2)
            {
                //There was a transision in the left eye, is there a similar transision in the right eye
                int j = l - 1;
                int color3 = na.GetNeuronAt(j, 0).CurrentChargeInt;
                int color4 = color3;
                if (color3 == color1)
                {
                    for (j = j; j < na.Width; j++)
                    {
                        color4 = na.GetNeuronAt(j, 0).CurrentChargeInt;
                        if (color3 != color4) break;

                    }
                    if (color4 == color2 && color1 == color3)
                    {
                        //we have an equivelant transition in the other eye...how far away is it?
                        //using law of sines
                        //(if the field-of-view and eye separation were constant, this could all be a lookup table)
                        double thetaA = GetDirectionOfNeuron(l - 0.5f, na.Width);
                        double a = toDegrees(thetaA);
                        thetaA = Math.PI / 2 - thetaA; //get angle to axis
                        a = toDegrees(thetaA);
                        double thetaB = GetDirectionOfNeuron(j - 0.5f, na.Width);
                        thetaB = Math.PI / 2 - thetaB;
                        thetaB = Math.PI - thetaB; //to get an inside angle
                        a = toDegrees(thetaB);
                        double thetaC = Math.PI - thetaA - thetaB;
                        a = toDegrees(thetaC);

                        double A = Math.Sin(thetaA) * (2 * eyeOffset) / Math.Sin(thetaC);
                        Color c = Utils.FromArgb(color1);
                        if (c == Colors.Black)
                            c = Utils.FromArgb(color2);
                        thetaA -= Math.PI / 2;
                        PointPlus P = new PointPlus { Theta = (float)-thetaA, R = (float)A};  //relative to left eye
                        P.Y -= eyeOffset;
                        if (P.R < 5 && P.R > 1) //innacuracy if too close or too far
                            naModel.AddPosiblePointToKB(P,c);
                        return l;
                    }
                }
            }
            return -1;
        }

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
        }
        public override void ShowDialog() //delete this function if it isn't needed
        {
            base.ShowDialog();
        }
    }


}