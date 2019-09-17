//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace BrainSimulator
{
    public class ModuleSimView : ModuleBase
    {

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable

            if (MainWindow.realSim == null) return;

            //NeuronArea naFovea = FindAreaByLabel("Fovea");
            //if (naFovea != null && FoveaBitmap != null) return;

            System.Drawing.Bitmap bitmap1;
            Application.Current.Dispatcher.Invoke((Action)delegate { MainWindow.realSim.GetBitMap(); });

            //the transfer from the sim to the view is double-buffered.
            if (MainWindow.realSim.theBitMap1 != null)
            {
                bitmap1 = MainWindow.realSim.theBitMap1;
                MainWindow.realSim.theBitMap1 = null;
            }
            else if (MainWindow.realSim.theBitMap2 != null)
            {
                bitmap1 = MainWindow.realSim.theBitMap2;
                MainWindow.realSim.theBitMap2 = null;
            }
            else
                return;

            if (na.Height == 0 || na.Width == 0) return;
            float ratio = bitmap1.Width / na.Width;
            float ratio2 = bitmap1.Height / na.Height;
            if (ratio2 < ratio) ratio = ratio2;

            for (int i = 0; i < na.Width; i++)
            {
                for (int j = 0; j < na.Height; j++)
                {
                    Neuron n = na.GetNeuronAt(i, j);
                    int x = (int)(i * ratio);
                    int y = (int)(j * ratio);
                    if (x >= bitmap1.Width) break;
                    if (y >= bitmap1.Height) break;
                    System.Drawing.Color c = bitmap1.GetPixel(x, y);
                    System.Windows.Media.Color c1 = new System.Windows.Media.Color
                    { A = c.A, R = c.R, G = c.G, B = c.B };
                    int theColor = Utils.ToArgb(c1);

                    if (theColor != 0 && theColor != 303)
                        n.SetValueInt(theColor);
                    else
                        n.SetValueInt(0);
                }
            }


            //            if (naFovea != null)
            ///                FoveaBitmap = bitmap1;
        }
        public override void Initialize()
        {
            //the simulation view is opened and remains open for the duration of the Application
            //if closed by alt-F4, it cannot be reopened...oh well
            if (MainWindow.realSim == null)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate { OpenTheSimWindow(); });
            }

        }
        protected void OpenTheSimWindow()
        {
            //this code will be executed the first time the module is fired
            MainWindow.realSim = new RealitySimulator();
            MainWindow.realSim.Show();

        }
    }
}
