//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class CenterPoint
    {
        public float length;
        public Point loc;
        public Angle angle;
    }

    public class ModuleStrokeCenter : ModuleBase
    {
        public static int numDirs = 16;

        int backgroundColor;
        public override void Fire()
        {
            Init();  //be sure to leave this here
            ModuleView naSource = theNeuronArray.FindModuleByLabel("ModuleImageFile");
            foreach (Neuron n in mv.Neurons)
                n.SetValueInt(0);

            if (naSource != null)
            {
                backgroundColor = naSource.GetNeuronAt(0, 0).LastChargeInt;
                List<CenterPoint> ptList = new List<CenterPoint>();
                foreach (Neuron n in mv.Neurons)
                {
                    if (n.LastChargeInt != 0) continue;

                    mv.GetNeuronLocation(n.id, out int x2, out int y2);
                    MapToSource(naSource, x2, y2, out int x1, out int y1);
                    Neuron nSource = naSource.GetNeuronAt(x1, y1);

                    if (nSource.LastChargeInt != backgroundColor)
                    {
                        double bestDir = 0;
                        float bestLength = 10000;
                        Point bestPoint = new Point(0, 0);
                        int bestDirNum = -1;
                        for (int i = 0; i < numDirs; i++)
                        {
                            double direction1 = i * PI / (float)numDirs;
                            Point loc = GetStrokeMiddle(x1, y1, direction1, out float length);
                            if (length < bestLength && length != -1)
                            {
                                bestLength = length;
                                bestDir = direction1;
                                bestPoint = loc;
                                bestDirNum = i;
                            }
                        }
                        if (bestLength != -1)
                        {                        //is this location already in the list...only add if length is shorter
                            MapToDest(naSource, (int)Round(bestPoint.X), (int)Round(bestPoint.Y), out int x3, out int y3);
                            Point newPt = new Point(x3, y3);
                            int index = PointAlreadyInList(ptList, newPt);
                            if (index == -1)
                            {
                                ptList.Add(new CenterPoint { loc = newPt, angle = bestDir, length = bestLength });
                                int value = 0xf00000 | bestDirNum << 12 | (int)bestLength;
                                mv.GetNeuronAt((int)newPt.X, (int)newPt.Y).SetValueInt(value);
                            }
                            else if (ptList[index].length > bestLength)
                            {
                                ptList[index] = new CenterPoint { loc = newPt, angle = bestDir, length = bestLength };
                                int value = 0xf00000 | bestDirNum << 12 | (int)bestLength;
                                mv.GetNeuronAt((int)newPt.X, (int)newPt.Y).SetValueInt(value);
                            }
                        }
                    }
                }
            }
        }

        public static int PointAlreadyInList(List<CenterPoint> theList, Point loc)
        {
            int retVal = -1;

            for (int i = 0; i < theList.Count; i++)
            {
                Point loc1 = theList[i].loc;
                double dx2 = (loc.X - loc1.X) * (loc.X - loc1.X);
                double dy2 = (loc.Y - loc1.Y) * (loc.Y - loc1.Y);
                if (dx2 + dy2 < 1)
                    return i;
            }
            return retVal;
        }

        private void MapToSource(ModuleView naSource, int x2, int y2, out int x1, out int y1)
        {
            x1 = (int)Round((x2 * (naSource.Width - 1) / (float)(mv.Width - 1)));
            y1 = (int)Round((y2 * (naSource.Height - 1) / (float)(mv.Height - 1)));
        }
        private void MapToDest(ModuleView naSource, int x2, int y2, out int x1, out int y1)
        {
            x1 = (int)Round((x2 * (mv.Width - 1) / (float)(naSource.Width - 1)));
            y1 = (int)Round((y2 * (mv.Height - 1) / (float)(naSource.Height - 1)));
        }

        Point GetStrokeMiddle(int x1, int y1, double direction, out float length)
        {
            //TODO add improved boundary detection to replace == backgroundColor
            int maxSearch = 20;
            Point retVal = new Point(0, 0);
            length = -1;
            ModuleView naSourceImage = theNeuronArray.FindModuleByLabel("ModuleImageFile");
            if (naSourceImage == null) return retVal;
            double dx = Cos(direction);
            double dy = Sin(direction);
            int start = 0;
            int end = 0;
            double x = x1;
            double y = y1;
            for (int i = 1; i < maxSearch; i++)
            {
                x += dx;
                y += dy;
                Neuron n = naSourceImage.GetNeuronAt((int)x, (int)y);
                if (n != null && n.LastChargeInt == backgroundColor)
                {
                    end = i;
                    break;
                }
                if (i == maxSearch - 1)
                    return retVal;
            }
            x = x1;
            y = y1;
            for (int i = 1; i < maxSearch; i++)
            {
                x -= dx;
                y -= dy;
                Neuron n = naSourceImage.GetNeuronAt((int)x, (int)y);
                if (n != null && n.LastChargeInt == backgroundColor)
                {
                    start = -i;
                    break;
                }
                if (i == maxSearch - 1)
                    return retVal;
            }
            double midPoint = (start + end) / 2f;
            retVal = new Point(x1 + midPoint * dx, y1 + midPoint * dy);
            length = end - start;

            return retVal;
        }


        public override void Initialize()
        {
            foreach (Neuron n in mv.Neurons)
            {
                n.Model = Neuron.modelType.Color;
                n.SetValue(0);
            }
        }
    }
}
