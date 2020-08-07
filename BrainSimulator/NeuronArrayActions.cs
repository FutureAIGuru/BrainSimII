//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using BrainSimulator.Modules;


namespace BrainSimulator
{
    public partial class NeuronArray
    {
        private void HandleProgrammedActions()
        {
            lock (modules)
            {
                foreach (ModuleView na in modules)
                {
                    if (na.TheModule != null)
                    {
                        na.TheModule.Fire();
                    }
                }
            }
        }


        ////[DO NOT USE} loads an image from a file...will be converted to a module
        //int fileCounter = 0;
        //int countDown = 0;
        //List<string> dirs = null;
        //Bitmap bitmap1;
        //public void LoadImage(ModuleView na)
        //{
        //    if (countDown == 0)
        //    {
        //        if (fileCounter == 0)
        //            dirs = new List<string>(Directory.EnumerateFiles("E:\\Charlie\\Documents\\Brainsim\\Images"));
        //        countDown = 3;
        //        bitmap1 = new Bitmap(dirs[fileCounter]);
        //        fileCounter++;
        //        if (fileCounter >= dirs.Count) fileCounter = 0;
        //    }

        //    na.GetBounds(out int X1, out int Y1, out int X2, out int Y2);
        //    countDown--;

        //    for (int i = X1 + 1; i < X2 - 1; i++)
        //        for (int j = Y1 + 1; j < Y2 - 1; j++)
        //        {
        //            int neuronIndex = GetNeuronIndex(i, j);
        //            Neuron n = MainWindow.theNeuronArray.neuronArray[neuronIndex];
        //            int x = (i - X1) * bitmap1.Width / (X2 - X1);
        //            int y = (j - Y1) * bitmap1.Height / (Y2 - Y1);
        //            System.Drawing.Color c = bitmap1.GetPixel(x, y);
        //            if (c.R != 255 || c.G != 255 || c.B != 255)
        //            {
        //                n.CurrentCharge = n.LastCharge = 1 - (float)c.R / 255.0f;
        //            }
        //            else
        //            {
        //                n.CurrentCharge = n.LastCharge = 0;
        //            }
        //        }
        //}
    }
}
