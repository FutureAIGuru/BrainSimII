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

        public string filePath = "";
        public bool cycleThroughFolder = false;

        Bitmap bitmap1;
        public Bitmap GetBitMap()
        {
            return bitmap1;
        }

        public ModuleImageFile()
        {
            minHeight = 10;
            maxHeight = 500;
            minWidth = 10;
            maxWidth = 500;
        }

        int fileCounter = 0;
        int countDown = -1;
        List<string> fileList = new List<string>();
        [XmlIgnore]
        public bool fileAlreadyLoaded = false;

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            if (fileCounter >= fileList.Count) fileCounter = 0;
            if (fileCounter < 0) fileCounter = fileCounter = fileList.Count - 1;

            if (!fileAlreadyLoaded)
            {
                if (filePath != "")
                {
                    if (cycleThroughFolder)
                    {
                        GetFileList();
                        countDown = 3;
                    }
                    else if (fileList.Count == 0)
                    {
                        LoadImage(filePath);
                        countDown = -1;
                    }
                    else
                    {
                        LoadImage(fileList[fileCounter]);
                    }
                    fileAlreadyLoaded = true; ;
                }
            }
            else
            {
                if (countDown == 0 && cycleThroughFolder)
                {
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

        private void GetFileList()
        {
            string dir = filePath;
            FileAttributes attr = File.GetAttributes(filePath);
            if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
                dir = Path.GetDirectoryName(filePath);
            fileList = new List<string>(Directory.EnumerateFiles(dir));
            LoadImage(fileList[fileCounter++]);
        }

        public override void Initialize()
        {
            foreach (Neuron n in na.Neurons())
            {
                n.Model = Neuron.modelType.Color;
                //    na.GetNeuronLocation(n, out int x, out int y);
                //    n.Label = x + "," + y;
            }
            fileList.Clear();
            fileCounter = 0;
            countDown = -1;
        }

        //NOT IMPLEMENTED
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
                try
                {
                    bitmap1 = new Bitmap(path);
                }
                catch
                {
                    return;
                }
                float vRatio = bitmap1.Height / (float)na.Height;
                float hRatio = bitmap1.Width / (float)na.Width;
                for (int i = 0; i < na.Height; i++)
                    for (int j = 0; j < na.Width; j++)
                    {
                        Neuron n = na.GetNeuronAt(j, i);
                        int x = (int)(j * (bitmap1.Width - 1) / (float)(na.Width - 1));
                        int y = (int)(i * (bitmap1.Height - 1) / (float)(na.Height - 1));

                        int val = GetAveragePixel(bitmap1, x, y, vRatio, hRatio);
                        n.LastChargeInt = val;
                    }
            }
        }

        public void SetNewPath(string path, bool cycle)
        {
            if (cycle)
                countDown = 3;
            else
                countDown = -1;
            filePath = path;
            fileAlreadyLoaded = false;
            cycleThroughFolder = cycle;
            fileCounter = 0;
            fileList.Clear();
        }
        public void NextFile()
        {
            if (fileList.Count == 0)
            {
                GetFileList();
                fileCounter = fileList.IndexOf(filePath);
            }
            fileCounter++;
            if (fileCounter >= fileList.Count)
                fileCounter = 0;
            fileAlreadyLoaded = false;
            countDown = -1;
            cycleThroughFolder = false;
        }

        public void PrevFile()
        {
            if (fileList.Count == 0)
            {
                GetFileList();
                fileCounter = fileList.IndexOf(filePath);
            }
            fileCounter--;
            if (fileCounter < 0)
                fileCounter = fileList.Count - 1;
            fileAlreadyLoaded = false;
            countDown = -1;
            cycleThroughFolder = false;
        }

        public override void SetUpAfterLoad()
        {
        }

    }
}
