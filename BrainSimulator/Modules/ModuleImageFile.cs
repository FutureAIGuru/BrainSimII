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
        public bool useDescription = true;

        //Bitmap bitmap1;
        public string GetFilePath()
        {
            return filePath;
        }

        public ModuleImageFile()
        {
            minHeight = 10;
            maxHeight = 500;
            minWidth = 10;
            maxWidth = 500;
        }

        public int fileCounter = 0;
        int countDown = -1;
        public List<string> fileList = new List<string>();

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            if (fileCounter >= fileList.Count) fileCounter = 0;
            if (fileCounter < 0) fileCounter = fileCounter = fileList.Count - 1;

            if (countDown == 0 && cycleThroughFolder)
            {
                if (fileList.Count > fileCounter)
                {
                    filePath = fileList[fileCounter++];
                    LoadImage(filePath);
                    countDown = 1; //minimum cycles to show a file
                    SetDescription(filePath);
                }
            }
            else
            {
                if (countDown >= 0)
                    countDown--;
            }

        }
        //check to see if there is a description and if not 
        //if a file name contains at least one space, it is treated as the description of the content 
        void SetDescription(string fileName)
        {
            if (!useDescription) return;
            ModuleBoundaryDescription mbd = (ModuleBoundaryDescription)FindModule("BoundaryDescription");
            if (mbd == null) return;

            string textFilePath = Path.ChangeExtension(fileName, "txt");
            if (File.Exists(textFilePath))
            {
                string fullCommandText = File.ReadAllText(textFilePath);
                mbd.SetDescription(fullCommandText);
                string[] words = fullCommandText.Split(' ');
                countDown = words.Length + 1;
                return;
            }

            string name = Path.GetFileNameWithoutExtension(fileName);

            if (useDescription)
            {
                //remove digits and text within parentheses
                string name1 = "";
                bool inParen = false;
                foreach (char c in name)
                {
                    if (c == '(')
                        inParen = true;
                    if (!inParen && !char.IsDigit(c))
                        name1 += c;
                    if (c == ')')
                        inParen = false;
                }
                mbd.SetDescription(name1);
                string[] words = name1.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                countDown = words.Length + 1;
            }
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
            Bitmap bitmap1 = null;

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
                float vRatio = bitmap1.Height / (float)mv.Height;
                float hRatio = bitmap1.Width / (float)mv.Width;
                for (int i = 0; i < mv.Height; i++)
                    for (int j = 0; j < mv.Width; j++)
                    {
                        Neuron n = mv.GetNeuronAt(j, i);
                        int x = (int)(j * (bitmap1.Width - 1) / (float)(mv.Width - 1));
                        int y = (int)(i * (bitmap1.Height - 1) / (float)(mv.Height - 1));

                        int val = GetAveragePixel(bitmap1, x, y, vRatio, hRatio);
                        n.LastChargeInt = val;
                    }
            }
        }

        public void SetParameters(List<string> paths, string curPath, bool cycle, bool useFileName)
        {
            Init();

            cycleThroughFolder = cycle;
            useDescription = useFileName;

            //if the path is empty, don't mess with the file list
            if (paths != null)
            {
                fileList = paths;
                filePath = curPath;
                fileCounter = fileList.IndexOf(curPath);
            }
            if (fileList != null && fileList.Count > 0 && paths != null)
            {
                if (fileCounter >= fileList.Count) fileCounter = 0;
                fileList = paths;
                LoadImage(fileList[fileCounter]);
                SetDescription(fileList[fileCounter]);
            }
            if (cycleThroughFolder)
            {
                fileCounter++;
                countDown = 3;
            }
            else
                countDown = -1; //setting the decrement counter to -1 suspends future file loading
        }
        public void NextFile()
        {
            fileCounter++;
            if (fileCounter >= fileList.Count)
                fileCounter = 0;
            countDown = -1;
            if (fileList.Count > 0)
            {
                LoadImage(fileList[fileCounter]);
                filePath = fileList[fileCounter];
            }
        }

        public void PrevFile()
        {
            fileCounter--;
            if (fileCounter < 0)
                fileCounter = fileList.Count - 1;
            countDown = -1;
            if (fileList.Count > 0)
            {
                LoadImage(fileList[fileCounter]);
                filePath = fileList[fileCounter];
            }
        }

        public void ResendDescription()
        {
            SetDescription(filePath);
        }

        public override void Initialize()
        {
            Init();
            if (mv == null) return;
            foreach (Neuron n in mv.Neurons)
            {
                n.Model = Neuron.modelType.Color;
                n.LastChargeInt = 0;
            }
            fileList.Clear();
            fileCounter = 0;
            countDown = -1;
        }

       
        public override void SetUpAfterLoad()
        {
            Init();
            foreach (Neuron n in mv.Neurons)
            {
                n.Model = Neuron.modelType.Color;
                n.LastChargeInt = 0;
            }

            //check to see if the filelist is legit
            foreach (string path in fileList)
            {
                if (!File.Exists(path))
                {
                    fileList.Clear();
                    filePath = "";
                    break;
                }
            }


            if (fileList.Count > 0)
                LoadImage(fileList[fileCounter]);
        }
        public override void SizeChanged()
        {
            base.SizeChanged();
            Initialize();
        }
    }
}
