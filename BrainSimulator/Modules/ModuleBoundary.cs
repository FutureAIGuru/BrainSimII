//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;


namespace BrainSimulator.Modules
{
    //The Array2D and 2DI are 2D arrays which return 0 if you reference them out of bounds
    //that way, you don't have to check bounds all the time.
    class Array2D
    {
        int[,] theArray;
        public Array2D(int size1, int size2)
        {
            theArray = new int[size1, size2];
        }

        public int this[int key1, int key2]
        {
            get
            {
                if (key1 < 0 || key2 < 0) return 0;
                if (key1 >= theArray.GetLength(0) || key2 >= theArray.GetLength(1)) return 0;
                return (int)theArray[key1, key2];
            }
            set
            {
                if (key1 < 0 || key2 < 0) return;
                if (key1 >= theArray.GetLength(0) || key2 >= theArray.GetLength(1)) return;
                theArray[key1, key2] = value;
            }
        }
        public int GetLength(int index)
        {
            return theArray.GetLength(index);
        }
    }
    class Array2DF
    {
        float[,] theArray;
        public Array2DF(int size1, int size2)
        {
            theArray = new float[size1, size2];
        }

        public float this[int key1, int key2]
        {
            get
            {
                if (key1 < 0 || key2 < 0) return 0;
                if (key1 >= theArray.GetLength(0) || key2 >= theArray.GetLength(1)) return 0;
                return (float)theArray[key1, key2];
            }
            set
            {
                if (key1 < 0 || key2 < 0) return;
                if (key1 >= theArray.GetLength(0) || key2 >= theArray.GetLength(1)) return;
                theArray[key1, key2] = value;
            }
        }
        public int GetLength(int index)
        {
            return theArray.GetLength(index);
        }
    }

    public class ModuleBoundary : ModuleBase
    {
        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleBoundary()
        {
            minHeight = 2;
            maxHeight = 500;
            minWidth = 2;
            maxWidth = 500;
        }

        public int hueLimit = 10; // minimum hue difference for boundary (in degrees)
        public float brightLimit = .5f; // minimum brightness difference for boundary (0-1)
        public float satLimit = .5f; // minimum saturation difference for boundary (0-1)


        int backgroundColor;

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            ModuleView source = theNeuronArray.FindModuleByLabel("ImageZoom");
            if (source == null) return;

            backgroundColor = source.GetNeuronAt(0, 1).LastChargeInt;

            //read the neuron values into an internal array
            Array2DF neuronValues = new Array2DF(source.Width, source.Height);
            Array2D neuronValuesI = new Array2D(source.Width, source.Height);
            for (int j = 1; j < source.Height; j++)
                for (int i = 0; i < source.Width; i++)
                {
                    int index = source.GetNeuronIndexAt(i, j);
                    neuronValues[i, j] = MainWindow.theNeuronArray.GetNeuronLastCharge(index);
                    neuronValuesI[i, j] = (int)neuronValues[i, j];
                }

            //find all the points which have different color values than their neighbors
            Array2D boundaryValues = new Array2D(mv.Width, mv.Height);
            for (int i = 0; i < source.Width && i < mv.Width; i++)
            {
                for (int j = 1; j < source.Height && i < mv.Height; j++)
                {
                    if (IsBoundary(i, j, neuronValuesI))
                        boundaryValues[i, j] = 1;
                    else
                        boundaryValues[i, j] = 0;
                }
            }

            //seach for corner points
            Array2D cornerValues = new Array2D(mv.Width, mv.Height);
            for (int i = 0; i < source.Width; i++)
            {
                for (int j = 1; j < source.Height; j++)
                {
                    if (IsCorner(i, j, boundaryValues))
                        cornerValues[i, j] = 1;
                    else
                        cornerValues[i, j] = 0;
                }
            }

            //set the neuron values from the internal array
            for (int i = 0; i < mv.Width; i++)
            {
                for (int j = 0; j < mv.Height; j++)
                {
                    int index = mv.GetNeuronIndexAt(i, j);
                    float lastCharge = boundaryValues[i, j];
                    if (cornerValues[i, j] != 0) lastCharge = 0.99f;
                    MainWindow.theNeuronArray.SetNeuronLastCharge(index, lastCharge);
                }
            }
            //if you want the dlg to update, use the following code whenever any parameter changes
            //UpdateDialog();
        }




        bool IsBoundary(int x, int y, Array2D neuronValues)
        {
            //if (neuronValues[x, y] == backgroundColor) return false;
            Color c = Utils.IntToDrawingColor(neuronValues[x, y]);
            float hue2 = c.GetHue();
            float bright2 = c.GetBrightness();
            float sat2 = c.GetSaturation();

            //Color background = Utils.IntToDrawingColor(backgroundColor);
            //float backHue = background.GetHue();
            //float backSat = background.GetSaturation();
            //float backBrightness = background.GetBrightness();
            //if (Math.Abs(backHue - hue2) < hueLimit &&
            //    Math.Abs(backSat - sat2) < satLimit &&
            //    Math.Abs(backBrightness - bright2) < brightLimit) 
            //    return false;

            for (int i = 0; i < 3; i++)
            {
                GetDeltasFromDirection(i, out int dx, out int dy);
                int x1 = x + dx;
                int y1 = y + dy;

                Color c1 = Utils.IntToDrawingColor(neuronValues[x1, y1]);

                float hue1 = c1.GetHue();
                float bright1 = c1.GetBrightness();
                float sat1 = c1.GetSaturation();

                if (Math.Abs(hue1 - hue2) > hueLimit || Math.Abs(bright1 - bright2) > brightLimit || Math.Abs(sat1 - sat2) > satLimit)
                    return true;
            }
            return false;
        }
        public static void GetDeltasFromDirection(int dir, out int dx, out int dy)
        {
            switch (dir)
            {
                case 0: dx = 1; dy = 0; break;
                case 1: dx = 1; dy = 1; break;
                case 2: dx = 0; dy = 1; break;
                case 3: dx = -1; dy = 1; break;
                case 4: dx = -1; dy = 0; break;
                case 5: dx = -1; dy = -1; break;
                case 6: dx = 0; dy = -1; break;
                case 7: dx = 1; dy = -1; break;
                default: dx = 0; dy = 0; break;
            }
            //while (dir < 0) dir += 8;
            //while (dir > 7) dir -= 8;
            //switch (dir)
            //{
            //    case 0: dx = 1; dy = 0; break;
            //    case 1: dx = 1; dy = -1; break;
            //    case 2: dx = 0; dy = -1; break;
            //    case 3: dx = -1; dy = -1; break;
            //    case 4: dx = -1; dy = 0; break;
            //    case 5: dx = -1; dy = 1; break;
            //    case 6: dx = 0; dy = 1; break;
            //    case 7: dx = 1; dy = 1; break;
            //    default: dx = 0; dy = 0; break;
            //}
        }

        bool IsCorner(int x, int y, Array2D boundaryValues)
        {
            if (boundaryValues[x, y] == 0) return false;
            //does it have 5 cosecutive immediate neighbors?
            int consecutive = 0;
            for (int i = 0; i < 12; i++)
            {
                GetDeltasFromDirection(i, out int dx, out int dy);
                int x1 = x + dx;
                int y1 = y + dy;
                if (boundaryValues[x1, y1] == 0) consecutive++;
                else consecutive = 0;
                if (consecutive == 5) return true;
            }
            if (CornerMatchPattern(x, y, boundaryValues)) return true;
            return false;
        }

        private float GetArrayValue(int k, int orientation, int i1, int j1, float[,,] matchPatterns)
        {
            float arrayVal = -1;
            int height = matchPatterns.GetLength(1) - 1;
            int width = matchPatterns.GetLength(2) - 1;

            switch (orientation)
            {
                case 0:
                    arrayVal = matchPatterns[k, j1, i1];
                    break;
                case 1:
                    arrayVal = matchPatterns[k, width - j1, i1];
                    break;
                case 2:
                    arrayVal = matchPatterns[k, j1, height - i1];
                    break;
                case 3:
                    arrayVal = matchPatterns[k, width - j1, height - i1];
                    break;
                case 4:
                    arrayVal = matchPatterns[k, i1, j1];
                    break;
                case 5:
                    arrayVal = matchPatterns[k, width - i1, j1];
                    break;
                case 6:
                    arrayVal = matchPatterns[k, i1, height - j1];
                    break;
                case 7:
                    arrayVal = matchPatterns[k, width - i1, height - j1];
                    break;
            }

            return arrayVal;
        }
        bool CornerMatchPattern(int x, int y, Array2D boundaryValues)
        {
            float[,,] matchPatternsCorner = new float[,,]
            {
         {{ 2, 2, 2, 2, 2 },
          { 2, 0, 0, 0, 0 },
          { 0, 0, 1, 1, 1 },
          { 0, 1, 2, 2, 2 },
          { 2, 2, 2, 2, 2 } },
         {{ 2, 2, 2, 2, 2 },
          { 0, 0, 0, 0, 0 },
          { 0, 1, 1, 1, 0 },
          { 1, 2 , 2, 2, 1 },
          { 1, 2, 2, 2, 2 } },
         {{ 2, 2, 2, 2, 2 },
          { 2, 0, 0, 0, 0 },
          { 0, 0, 1, 1, 0 },
          { 0, 1, 2, 2, 1 },
          { 2, 1, 2, 2, 2 } },
            };

            for (int k = 0; k < matchPatternsCorner.GetLength(0); k++)
            {
                for (int orientation = 0; orientation < 8; orientation++)
                {
                    for (int i1 = 0; i1 < 5; i1++)
                    {
                        for (int j1 = 0; j1 < 5; j1++)
                        {
                            float arrayVal = GetArrayValue(k, orientation, i1, j1, matchPatternsCorner);
                            if (Math.Abs(arrayVal) != 2)
                            {
                                if (boundaryValues[i1 + x - 2, j1 + y - 2] != Math.Abs(arrayVal))
                                    goto noMatch;
                            }
                        }
                    }
                    //match
                    return true;
                noMatch:
                    {

                    }

                }
            }

            return false;
        }



        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            Init();
            foreach (Neuron n in mv.Neurons)
                n.Model = Neuron.modelType.FloatValue;
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }

        public override MenuItem CustomContextMenuItems()
        {
            StackPanel s2 = new StackPanel { Orientation = Orientation.Vertical };
            s2.Children.Add(new Label { Content = "Thresholds:" });

            StackPanel s = new StackPanel { Orientation = Orientation.Horizontal };
            s.Children.Add(new Label { Content = "Hue (0-360): " + (int)hueLimit, Width = 110 });
            Slider sl1 = new Slider { Name = "Hue", Maximum = 1, Width = 100, Height = 20, Value = hueLimit / 360f };
            sl1.ValueChanged += Sl1_ValueChanged;
            s.Children.Add(sl1);
            s2.Children.Add(s);


            s = new StackPanel { Orientation = Orientation.Horizontal };
            s.Children.Add(new Label { Content = "Bright (0-1): " + brightLimit.ToString("f2"), Width = 110 });
            sl1 = new Slider { Name = "Brt", Maximum = 1, Width = 100, Height = 20, Value = brightLimit };
            sl1.ValueChanged += Sl1_ValueChanged;
            s.Children.Add(sl1);
            s2.Children.Add(s);

            s = new StackPanel { Orientation = Orientation.Horizontal };
            s.Children.Add(new Label { Content = "Sat. (0-1): " + satLimit, Width = 110 });
            sl1 = new Slider { Name = "Sat", Maximum = 1, Width = 100, Height = 20, Value = satLimit };
            sl1.ValueChanged += Sl1_ValueChanged;
            s.Children.Add(sl1);
            s2.Children.Add(s);

            return new MenuItem { Header = s2, StaysOpenOnClick = true };
        }


        private void Sl1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider sl)
            {
                string newLabelText = "";
                switch (sl.Name)
                {
                    case "Hue":
                        hueLimit = (int)(sl.Value * 360);
                        newLabelText = "Hue (0-360): " + (int)hueLimit;
                        break;
                    case "Brt":
                        brightLimit = (float)sl.Value;
                        newLabelText = "Bright (0-1): " + brightLimit.ToString("f2");
                        break;
                    case "Sat":
                        satLimit = (float)sl.Value;
                        newLabelText = "Sat. (0-1): " + satLimit;
                        break;
                }

                //set the value in the label
                if (sl.Parent is StackPanel sp)
                {
                    if (sp.Children[0] is Label l)
                    {
                        l.Content = newLabelText;
                    }
                }
            }
        }
    }
}
