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
using System.Xml.Serialization;
using System.Drawing;
using System.IO;

namespace BrainSimulator.Modules
{
    public class ModuleImageFile : ModuleBase
    {
        //DO NOT USE THIS MODULE...IT NEEDS TO BE REWRITTEN

        //any public variable you create here will automatically be stored with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code 
            //because the thread you are in is not the UI thread
            //if (dlg != null)
            //     Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
        }

        //[DO NOT USE} loads an image from a file...will be converted to a module
        int fileCounter = 0;
        int countDown = 0;
        List<string> dirs = null;
        Bitmap bitmap1;
        public void LoadImage(ModuleView na)
        {
            if (countDown == 0)
            {
                if (fileCounter == 0)
                    dirs = new List<string>(Directory.EnumerateFiles("E:\\Charlie\\Documents\\Brainsim\\Images"));
                countDown = 3;
                bitmap1 = new Bitmap(dirs[fileCounter]);
                fileCounter++;
                if (fileCounter >= dirs.Count) fileCounter = 0;
            }

            na.GetBounds(out int X1, out int Y1, out int X2, out int Y2);
            countDown--;

            for (int i = X1 + 1; i < X2 - 1; i++)
                for (int j = Y1 + 1; j < Y2 - 1; j++)
                {
                    int neuronIndex = MainWindow.theNeuronArray.GetNeuronIndex(i, j);
                    Neuron n = MainWindow.theNeuronArray.GetNeuron(neuronIndex);
                    int x = (i - X1) * bitmap1.Width / (X2 - X1);
                    int y = (j - Y1) * bitmap1.Height / (Y2 - Y1);
                    System.Drawing.Color c = bitmap1.GetPixel(x, y);
                    if (c.R != 255 || c.G != 255 || c.B != 255)
                    {
                        n.CurrentCharge = n.LastCharge = 1 - (float)c.R / 255.0f;
                    }
                    else
                    {
                        n.CurrentCharge = n.LastCharge = 0;
                    }
                }
        }

    }
}
