//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Media;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleImageFile : ModuleBase
    {
        //any public variable you create here will automatically be stored with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;
        [XmlIgnore]
        public string filePath = "";
        [XmlIgnore]
        public bool cycleThroughFolder = false;

        int fileCounter = 0;
        int countDown = -1;
        List<string> fileList = null;

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            if (filePath != "")
            {
                if (cycleThroughFolder)
                {
                    filePath = Path.GetDirectoryName(filePath);
                    fileList = new List<string>(Directory.EnumerateFiles(filePath));
                    LoadImage(fileList[fileCounter++]);
                    countDown = 3;
                }
                else
                {
                    LoadImage(filePath);
                    countDown = -1;
                }
                filePath = "";
            }
            else
            {
                if (countDown == 0)
                {
                    if (fileCounter == fileList.Count) fileCounter = 0;
                    LoadImage(fileList[fileCounter++]);
                    countDown = 3;
                }
                else
                {
                    if (countDown >= 0)
                        countDown--;
                }
            }

        }

        public override void Initialize()
        {
            foreach (Neuron n in na.Neurons())
            {
                n.Model = Neuron.modelType.Color;
            //    na.GetNeuronLocation(n, out int x, out int y);
            //    n.Label = x + "," + y;
            }
            fileList = null;
            fileCounter = 0;
            countDown = -1;
        }

        int GetAveragePixel(Bitmap bitmap1, int x, int y, float vRatio, float hRatio)
        {
            int average = 0;
            int x0 = (int)hRatio / 2;
            int y0 = (int)vRatio / 2;
            for (int i = -x0; i <= x0; i++)
                for (int j = -y0; j <= y0; j++)
                {
                    if (i >= 0 && j >= 0 && i < bitmap1.Width && j < bitmap1.Height)
                    {
                        average += Utils.ColorToInt(bitmap1.GetPixel(i, j));
                    }
                }
            average = average / (81);
            System.Drawing.Color c = bitmap1.GetPixel(x, y);
            int val = Utils.ColorToInt(c);
            return val;
        }

        private void LoadImage(string path)
        {
            if (path != "")
            {
                Bitmap bitmap1 = new Bitmap(path);
                float vRatio = bitmap1.Height / (float)na.Height;
                float hRatio = bitmap1.Width / (float)na.Width;
                for (int i = 0; i < na.Height; i++)
                    for (int j = 0; j < na.Width; j++)
                    {
                        Neuron n = na.GetNeuronAt(j, i);
                        int x = (int)(j * (bitmap1.Width - 1) / (float)(na.Width - 1));
                        int y = (int)(i * (bitmap1.Height - 1) / (float)(na.Height - 1));
                        int val = GetAveragePixel(bitmap1, x, y, vRatio, hRatio);
                        //                        System.Drawing.Color c = bitmap1.GetPixel(x, y);
                        //                        int val = Utils.ColorToInt(c);
                        //if (val != 0xffffff) val = 0;
                        n.LastChargeInt = val;
                    }
            }
        }

    }
}
