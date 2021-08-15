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

    public class ModuleBoundary2 : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleBoundary2()
        {
            minHeight = 2;
            maxHeight = 500;
            minWidth = 2;
            maxWidth = 500;
        }

        public int hueLimit = 10; // minimum hue difference for boundary (in degrees)
        public float brightLimit = .5f; // minimum hue difference for boundary (in degrees)
        public float satLimit = .5f; // minimum hue difference for boundary (in degrees)


        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            ModuleView source = theNeuronArray.FindModuleByLabel("ImageZoom");
            if (source == null) return;

            //read the neuron values into an internal array
            Array2D neuronValues = new Array2D(source.Width, source.Height);
            for (int j = 1; j < source.Height; j++)
                for (int i = 0; i < source.Width; i++)
                {
                    neuronValues[i, j] = source.GetNeuronAt(i, j).LastChargeInt;
                }

            //find all the points which have different color values than their neighbors
            Array2DF neuronValues1 = new Array2DF(na.Width, na.Height);
            for (int i = 0; i < source.Width && i < na.Width; i++)
            {
                for (int j = 1; j < source.Height && i < na.Height; j++)
                {
                    if (IsBoundary(i, j, neuronValues))
                        neuronValues1[i, j] = 1;
                    else
                        neuronValues1[i, j] = 0;
                }
            }

            //every boundary is twice as wide as needed...thin them
            Array2DF neuronValues2 = new Array2DF(neuronValues1.GetLength(0), neuronValues1.GetLength(0));
            for (int i = 0; i < neuronValues1.GetLength(0) - 1; i++)
            {
                for (int j = 0; j < neuronValues1.GetLength(1) - 1; j++)
                {
                    //neuronValues2[i, j] = neuronValues1[i, j];
                    if (neuronValues1[i, j] == 1 &&
                        neuronValues1[i + 1, j] == 1 &&
                        neuronValues1[i, j + 1] == 1 &&
                        neuronValues1[i + 1, j + 1] == 1)
                    {
                        neuronValues2[i, j] = 1;
                    }
                }
            }

            //corner areas need to be cleaned up
            CleanUpBoundaries(neuronValues2);

            //specific corner pixels are located
            FindCorners(neuronValues2);

            //set the neuron values from the internal array
            for (int i = 0; i < na.Width; i++)
            {
                for (int j = 1; j < na.Height; j++)
                {
                    if (na.GetNeuronAt(i, j) is Neuron n)
                    {
                        n.LastCharge = neuronValues2[i, j];
                        n.Update();
                    }
                }
            }
            //if you want the dlg to update, use the following code whenever any parameter changes
            //UpdateDialog();
        }

        void CleanUpBoundaries(Array2DF neuronValues)
        {
            //0 must be 0, 1 must be 1, 2 don't care, -1 must be one, change to 0, -2 don't care, change to 0
            float[,,] matchPatterns = new float[,,]
            {
                {{ 0, 0, 0, 1,},
                { 1, -1, -1, 1,},
                { 0, 1, 1, 1,},
                { 0, 0, 0, 0,},
                },
                {{ 0, 0, 0, 0,},
                { 0, -1, -1, 1,},
                { 1, 1, 1, 0,},
                { 0, 0, 0, 0,},
                },
                {{ 0, 0, 0, 0,},
                { 1, 1, 1, 0,},
                { 0, -1, 1, 0,},
                { 0, 1, 0, 0,},
                },
                {{ 0, 0, 0, 0,},
                { 0, 1, 1, 0,},
                { 1, -1, -1, 1,},
                { 1, 0, 0, 0,},
                },
                {{ 0, 0, 1, 1,},
                { 1, -1, 1, 0,},
                { 0, 1, 1, 0,},
                { 0, 0, 0, 0,},
                },
                {{ 1, 0, 0, 0,},
                { 1, 1, 1, 0,},
                { 0, -1, 1, 0,},
                { 0, 1, 0, 0,},
                },
                {{ 0, 0, 1, 0,},
                { 0, 1, -1, 0,},
                { 0, 1, -1, 0,},
                { 0, 1, 1, 1,},
                },
                {{ 0, 1, 1, 0,},
                { 0, 1, -1, 0,},
                { 0, 1, 1, 1,},
                { 0, 0, 0, 1,},
                },
                {{ 0, 0, 0, 0,},
                { 0, 1, 1, 0,},
                { 0, 1, -1, 1,},
                { 1, -1,- 1, -1,},
                },
                {{ 0, 1, 1, 0,},
                { 0, 1, -1, 1,},
                { 1, -1,- 1, 1,},
                { 1, 0, 0, 0,},
                },
                {{ 1, -1, 0, 0,},
                { 1, -1, -1, 0,},
                { 1, 1, 1, 1,},
                { 0, 0, 0, 0,},
                },
                {{ 0, 0, 0, 1,},
                { 1, -1, -1, 1,},
                { 0, 1, 1, 0,},
                { 0, 0, 0, 0,},
                },
                {{ 1, 1, 0, 0,},
                { 0, -1, 1, 0,},
                { 0, -1, 1, 0,},
                { 0, 1, 0, 0,},
                },
                {{ 0, 0, 1, 1,},
                { 0, 1, -1, 0,},
                { 0, 1, -1, 0,},
                { 0, 1, 1, 1,},
                },
                {{ 0, 0, 0, 0,},
                { 0, 1, 1, 1,},
                { 1, -1, -1, 1,},
                { 1, 0, 0, 1,},
                },
                {{ 0, 0, 0, 0,},
                { 1, 1, 1, 0,},
                { 1, -1, 1, 0,},
                { 0, 0, 1, 1,},
                },
                {{ 0, 0, 0, 0,},
                { 1, -1, -1, 1,},
                { 0, 1, -1, 1,},
                { 0, 1, 1, 0,},
                },
                {{ 1, -1, -1, 1,},
                { 0, 1, -1, 1,},
                { 0, 1, 1, 0,},
                { 0, 0, 0, 0,},
                },
                {{ 0, 0, 0, 1,},
                { 1, -1, -1, 1,},
                { 1, -1, 1, 0,},
                { 1, 1, 0, 0,},
                },
                {{ 0, 1, 0, 0,},
                { 0, 1, 1, 1,},
                { 0, 1, -1, 1,},
                { 0, 1, 1, 0,},
                },
                {{ 0, 1, 1, 1,},
                { 0, -1,- 1, 1,},
                { 0, 1, 1, 0,},
                { 1, 1, 0, 0,},
                },
                {{ 1, 0, 0, 0,},
                { -1, 1, 1, 0,},
                { -1, -1, 1, 0,},
                { -1, 1, 0, 0,},
                },

                {{ 0, 0, 1, 0,},
                { 0, -1, 1, 0,},
                { 0, 1, 1, 0,},
                { 1, 1, 0, 0,},
                },
            };
            for (int i = 0; i < neuronValues.GetLength(0) - 1; i++)
            {
                for (int j = 0; j < neuronValues.GetLength(1) - 1; j++)
                {
                    //remove remaining "fours": a cluster of 4 firing neurons confuses the segment-finder
                    if (neuronValues[i, j] > 0.8f &&
                        neuronValues[i + 1, j] > 0.8f &&
                        neuronValues[i, j + 1] > 0.8f &&
                        neuronValues[i + 1, j + 1] > 0.8f)
                    {
                        for (int k = 0; k < matchPatterns.GetLength(0); k++)
                        {
                            for (int orientation = 0; orientation < 8; orientation++)
                            {
                                for (int i1 = 0; i1 < 4; i1++)
                                {
                                    for (int j1 = 0; j1 < 4; j1++)
                                    {
                                        float arrayVal = GetArrayValue(k, orientation, i1, j1, matchPatterns);
                                        if (Math.Abs(arrayVal) != 2)
                                        {
                                            //TODO add range limiting throughout
                                            int x = i1 + i - 1;
                                            int y = j1 + j - 1;
                                            if (neuronValues[x, y] != Math.Abs(arrayVal))
                                                goto noMatch;
                                        }
                                    }
                                }
                                for (int i1 = 0; i1 < 4; i1++)
                                {
                                    for (int j1 = 0; j1 < 4; j1++)
                                    {
                                        float arrayVal = GetArrayValue(k, orientation, i1, j1, matchPatterns);
                                        if (arrayVal < 0)
                                        {
                                            neuronValues[i1 + i - 1, j1 + j - 1] = 0;// 0.99f; change tis to tag removed points for testing
                                        }
                                    }
                                }
                                goto MatchFound;
                            noMatch:
                                {
                                }
                            }
                        }
                        //no match found

                        string pattern = "x=" + i + " y=" + j + "\n{";
                        for (int j1 = 0; j1 < 4; j1++)
                        {
                            pattern += "{";
                            for (int i1 = 0; i1 < 4; i1++)
                            {
                                pattern += neuronValues[i1 + i - 1, j1 + j - 1] == 1 ? " 1," : " 0,";
                            }
                            pattern += "},\n";
                        }
                        pattern += "},";
                        { } //SET A BREAKPOINT HERE.  If it is hit, open the "pattern" variable with the text visualizer and copy/paste the content to the pattern list above
                            //negate the 1's to be removed

                    MatchFound:
                        { }
                    }
                }
            }

        }

        void FindCorners(Array2DF neuronValues)
        {
            //0 must be 0, 1 must be 1, 2 don't care, center is marked as corner
            //special case: -2 where corner point is initially 0 & -3 point to be cleared
            float[,,] matchPatternsCorner = new float[,,]
            {
         {{ 2, 2, 2, 2, 2 },
          { 2, 0, 0 , 0, 2 },
          { 0, 1, 1, 1, 0 },
          { 1, 1, 2, 2, 1 },
          { 2, 2, 2, 2, 2 } },
         {{ 2, 2, 0, 0, 2 },
          { 2, 0, 0, 0, 0 },
          { 2, 1,-1, 0, 0 },
          { 2, 2, 1, 0, 2 },
          { 2, 2, 2, 2, 2 } },
         {{ 2, 2, 2, 2, 2 },
          { 2, 0, 0, 0, 2 },
          { 2, 0,-1, 0, 2 },
          { 2, 2, 2, 2, 2 },
          { 2, 2, 2, 2, 2 } },
         {{ 2, 2, 2, 2, 2 },
          { 0, 0, 0, 0, 2 },
          { 2, 0, 1, 1, 0 },
          { 2, 1, 2, 2, 1 },
          { 2, 1, 2, 2, 2 } },
         {{ 0, 0, 0, 0, 2 },
          { 0,-2, 1, 1, 1 },
          { 0, 1, 2, 2, 2 },
          { 0, 1, 2, 2, 2 },
          { 2, 1, 2, 2, 2 } },
         {{ 0, 0, 0, 0, 0},
          { 0,-2, 1, 1, 1},
          { 0, 1, 2, 2, 2},
          { 2, 1, 2, 2, 2},
          { 2, 1, 2, 2, 2 } },
         //{{ 2, 2, 2, 2, 2 },
         // { 0, 0, 0, 0, 0 },
         // { 0, 0, 1, 1, 1 },
         // { 0, 1, 2, 2, 2 },
         // { 1, 2, 2, 2, 2 } },
            };
            for (int i = 0; i < neuronValues.GetLength(0) - 1; i++)
            {
                for (int j = 0; j < neuronValues.GetLength(1) - 1; j++)
                {
                    //Tag corner points
                    if (neuronValues[i, j] == 1)
                    {
                        for (int k = 0; k < matchPatternsCorner.GetLength(0); k++)
                        {
                            if (k == 4)
                            { }
                            for (int orientation = 0; orientation < 8; orientation++)
                            {
                                int cx = 2;
                                int cy = 2;
                                for (int i1 = 0; i1 < 5; i1++)
                                {
                                    for (int j1 = 0; j1 < 5; j1++)
                                    {
                                        float arrayVal = GetArrayValue(k, orientation, i1, j1, matchPatternsCorner);
                                        if (Math.Abs(arrayVal) != 2)
                                        {
                                            if (neuronValues[i1 + i - 2, j1 + j - 2] != Math.Abs(arrayVal))
                                                goto noMatch;
                                        }
                                        if (arrayVal < 0)
                                        {
                                            cx = i1;
                                            cy = j1;
                                        }
                                    }
                                }
                                //match
                                int x = i + cx - 2;
                                int y = j + cy - 2;
                                neuronValues[x, y] = 0.99f;


                                break;
                            noMatch:
                                {

                                }

                            }
                        }
                    }
                }
            }
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

        //TODO These should migrate to the base of NA
        float GetNeuronAndValue(int i, int j)
        {
            return GetNeuronAndValue(i, j, out Neuron n);
        }
        float GetNeuronAndValue(int i, int j, out Neuron n)
        {
            n = null;
            if (na.GetNeuronAt(i, j) is Neuron n0)
            {
                n = n0;
                return n0.LastCharge;
            }
            return -1;
        }

        int GetFiringNieghborCount(int i, int j)
        {
            int count = 0;
            for (int k = 0; k < 8; k++)
            {
                GetDeltasFromDirection(k, out int dx, out int dy);
                if (na.GetNeuronAt(i + dx, j + dy) is Neuron n)
                {
                    if (n.LastCharge == 1)
                        count++;
                }
            }
            return count;
        }

        //        bool IsBoundary(int x, int y, ModuleView mv)
        bool IsBoundary(int x, int y, Array2D neuronValues)//int[,] neuronValues)
        {
            //if (mv.GetNeuronAt(x, y) is Neuron n)
            {

                Color c = Utils.IntToDrawingColor(neuronValues[x, y]);
                for (int i = 0; i < 8; i++)
                {
                    GetDeltasFromDirection(i, out int dx, out int dy);
                    int x1 = x + dx;
                    int y1 = y + dy;

                    Color c1 = Utils.IntToDrawingColor(neuronValues[x1, y1]);// n1.LastChargeInt);

                    float hue1 = c1.GetHue();
                    float hue2 = c.GetHue();
                    if (Math.Abs(hue1 - hue2) > hueLimit)
                        return true;

                    float bright1 = c1.GetBrightness();
                    float bright2 = c.GetBrightness();
                    if (Math.Abs(bright1 - bright2) > brightLimit)
                        return true;

                    float sat1 = c1.GetSaturation();
                    float sat2 = c.GetSaturation();
                    if (Math.Abs(sat1 - sat2) > satLimit)
                        return true;
                }
            }
            return false;
        }
        public static void GetDeltasFromDirection(int dir, out int dx, out int dy)
        {
            while (dir < 0) dir += 8;
            while (dir > 7) dir -= 8;
            switch (dir)
            {
                case 0: dx = 1; dy = 0; break;
                case 1: dx = 1; dy = -1; break;
                case 2: dx = 0; dy = -1; break;
                case 3: dx = -1; dy = -1; break;
                case 4: dx = -1; dy = 0; break;
                case 5: dx = -1; dy = 1; break;
                case 6: dx = 0; dy = 1; break;
                case 7: dx = 1; dy = 1; break;
                default: dx = 0; dy = 0; break;
            }
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            foreach (Neuron n in na.Neurons1)
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
            if (na == null) return; //this is called the first time before the module actually exists
        }

        public override MenuItem GetCustomMenuItems()
        {
            StackPanel s2 = new StackPanel { Orientation = Orientation.Vertical };
            s2.Children.Add(new Label { Content = "Thresholds:" });

            StackPanel s = new StackPanel { Orientation = Orientation.Horizontal };
            s.Children.Add(new Label { Content = "Hue (0-360):",Width=70 });
            Slider sl1 = new Slider { Name = "Hue", Maximum = 1, Width = 100, Height = 20,Value=hueLimit/360f };
            sl1.ValueChanged += Sl1_ValueChanged;
            s.Children.Add(sl1);
            s2.Children.Add(s);


            s = new StackPanel { Orientation = Orientation.Horizontal };
            s.Children.Add(new Label { Content = "Bright (0-1):", Width = 70 });
            sl1 = new Slider { Name = "Brt", Maximum = 1, Width = 100, Height = 20,Value=brightLimit };
            sl1.ValueChanged += Sl1_ValueChanged;
            s.Children.Add(sl1);
            s2.Children.Add(s);

            s = new StackPanel { Orientation = Orientation.Horizontal };
            s.Children.Add(new Label { Content = "Sat. (0-1):", Width = 70 });
            sl1 = new Slider { Name = "Sat", Maximum = 1, Width = 100, Height = 20,Value=satLimit };
            sl1.ValueChanged += Sl1_ValueChanged;
            s.Children.Add(sl1);
            s2.Children.Add(s);

            return new MenuItem { Header = s2, StaysOpenOnClick = true };
        }

        private void Sl1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider sl)
            {
                if (sl.Name == "Hue") hueLimit = (int)(sl.Value * 360);
                if (sl.Name == "Brt") brightLimit = (float)sl.Value;
                if (sl.Name == "Sat") satLimit = (float)sl.Value;

            }
        }
    }
}
