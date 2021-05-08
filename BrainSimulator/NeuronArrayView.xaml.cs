//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BrainSimulator
{
    public partial class NeuronArrayView : UserControl
    {

        private DisplayParams dp = new DisplayParams();

        public int targetNeuronIndex = -1;

        //needed for handling selections of areas of neurons
        Rectangle dragRectangle = null;
        int firstSelectedNeuron = -1;
        int lastSelectedNeuron = -1;

        //keeps track of the multiple selection areas
        public Selection theSelection = new Selection();

        int Rows { get { return MainWindow.theNeuronArray.rows; } }

        //this helper class keeps track of the neurons on the screen so they can change color without repainting
        private List<NeuronOnScreen> neuronsOnScreen = new List<NeuronOnScreen>();
        public class NeuronOnScreen
        {
            public int neuronIndex;
            public UIElement graphic;
            public float prevValue;
            public Label label;
            public List<synapseOnScreen> synapsesOnScreen = null;
            public struct synapseOnScreen
            {
                public int target;
                public float prevWeight;
                public Shape graphic;
            }
            public NeuronOnScreen(int index, UIElement e, float value, Label Label)
            {
                neuronIndex = index; graphic = e; prevValue = value; label = Label;
            }
        };

        List<int> showSynapses = new List<int>();
        public void AddShowSynapses(int neuronID)
        {
            if (!showSynapses.Contains(neuronID))
                showSynapses.Add(neuronID);
        }
        public void RemoveShowSynapses(int neuronID)
        {
            showSynapses.Remove(neuronID);
        }
        public bool IsShowingSnapses(int neuronID)
        {
            return showSynapses.Contains(neuronID);
        }
        public void ClearShowingSynapses()
        {
            showSynapses.Clear();
        }

        public NeuronArrayView()
        {
            InitializeComponent();
            zoomRepeatTimer.Tick += Dt_Tick;
#if DEBUG
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
#endif
        }

        public DisplayParams Dp { get => dp; set => dp = value; }

        private Canvas targetNeuronCanvas = null;

        //refresh the display of the neuron network
        public void Update()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            NeuronArray theNeuronArray = MainWindow.theNeuronArray;

            Canvas labelCanvas = new Canvas();
            Canvas.SetLeft(labelCanvas, 0);
            Canvas.SetTop(labelCanvas, 0);


            //Two canvases for synapses, ALL is for synapses behing neurons, Special is for synapses in front of neurons
            Canvas allSynapsesCanvas = new Canvas();
            Canvas.SetLeft(allSynapsesCanvas, 0);
            Canvas.SetTop(allSynapsesCanvas, 0);

            Canvas specialSynapsesCanvas = new Canvas();
            Canvas.SetLeft(specialSynapsesCanvas, 0);
            Canvas.SetTop(specialSynapsesCanvas, 0);


            int neuronCanvasCount = 2;
            Canvas[] neuronCanvas = new Canvas[neuronCanvasCount];
            for (int i = 0; i < neuronCanvasCount; i++)
                neuronCanvas[i] = new Canvas();

            Canvas legendCanvas = new Canvas();
            Canvas.SetLeft(legendCanvas, 0);
            Canvas.SetTop(legendCanvas, 0);

            targetNeuronCanvas = new Canvas();
            Canvas.SetLeft(targetNeuronCanvas, 0);
            Canvas.SetTop(targetNeuronCanvas, 0);

            if (MainWindow.IsArrayEmpty()) return;

            //Debug.WriteLine("Update " + MainWindow.theNeuronArray.Generation);
            dp.NeuronRows = MainWindow.theNeuronArray.rows;
            theCanvas.Children.Clear();
            neuronsOnScreen.Clear();
            int columns = MainWindow.theNeuronArray.arraySize / dp.NeuronRows;

            //draw some background grid and labels
            int boxSize = 250;
            if (columns > 2500) boxSize = 1000;
            for (int i = 0; i <= theNeuronArray.rows; i += boxSize)
            {
                Line l = new Line
                {
                    X1 = dp.DisplayOffset.X + 0,
                    X2 = dp.DisplayOffset.X + columns * dp.NeuronDisplaySize,
                    Y1 = dp.DisplayOffset.Y + i * dp.NeuronDisplaySize,
                    Y2 = dp.DisplayOffset.Y + i * dp.NeuronDisplaySize,
                    Stroke = new SolidColorBrush(Colors.Red),
                };
                legendCanvas.Children.Add(l);
            }
            for (int j = 0; j <= columns; j += boxSize)
            {
                Line l = new Line
                {
                    X1 = dp.DisplayOffset.X + j * dp.NeuronDisplaySize,
                    X2 = dp.DisplayOffset.X + j * dp.NeuronDisplaySize,
                    Y1 = dp.DisplayOffset.Y + 0,
                    Y2 = dp.DisplayOffset.Y + theNeuronArray.rows * dp.NeuronDisplaySize,
                    Stroke = new SolidColorBrush(Colors.Red),
                };
                legendCanvas.Children.Add(l);
            }

            int refNo = 1;
            for (int i = 0; i < theNeuronArray.rows; i += boxSize)
            {
                for (int j = 0; j < columns; j += boxSize)
                {
                    Point p = new Point((j + boxSize / 2) * dp.NeuronDisplaySize, (i + boxSize / 2) * dp.NeuronDisplaySize);
                    p += (Vector)dp.DisplayOffset;
                    Label l = new Label();
                    l.Content = refNo++;
                    l.FontSize = dp.NeuronDisplaySize * 10;
                    if (l.FontSize < 25) l.FontSize = 25;
                    if (l.FontSize > boxSize * dp.NeuronDisplaySize * 0.75)
                        l.FontSize = boxSize * dp.NeuronDisplaySize * 0.75;
                    l.Foreground = Brushes.White;
                    l.HorizontalAlignment = HorizontalAlignment.Center;
                    l.VerticalAlignment = VerticalAlignment.Center;
                    l.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                    Canvas.SetLeft(l, p.X - l.DesiredSize.Width / 2);
                    Canvas.SetTop(l, p.Y - l.DesiredSize.Height / 2);
                    legendCanvas.Children.Add(l);

                }
            }

            //draw the module rectangles
            lock (theNeuronArray.Modules)
            {
                for (int i = 0; i < MainWindow.theNeuronArray.Modules.Count; i++)
                {
                    ModuleView nr = MainWindow.theNeuronArray.Modules[i];
                    SelectionRectangle nsr = new SelectionRectangle(nr.FirstNeuron, nr.Width, nr.Height);
                    Rectangle r = nsr.GetRectangle(dp);
                    r.Fill = new SolidColorBrush(Utils.IntToColor(nr.Color));
                    r.SetValue(ShapeType, shapeType.Module);
                    r.SetValue(ModuleView.AreaNumberProperty, i);
                    theCanvas.Children.Add(r);

                    Label moduleLabel = new Label();
                    moduleLabel.Content = nr.Label;
                    moduleLabel.Background = new SolidColorBrush(Colors.White);
                    moduleLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    Canvas.SetLeft(moduleLabel, Canvas.GetLeft(r));
                    Canvas.SetTop(moduleLabel, Canvas.GetTop(r)-moduleLabel.DesiredSize.Height);
                    moduleLabel.SetValue(ShapeType, shapeType.Module);
                    moduleLabel.SetValue(ModuleView.AreaNumberProperty, i);
                    labelCanvas.Children.Add(moduleLabel);
                }
            }
            //draw any selection rectangle(s)
            for (int i = 0; i < theSelection.selectedRectangles.Count; i++)
            {
                Rectangle r = theSelection.selectedRectangles[i].GetRectangle(dp);
                r.Fill = new SolidColorBrush(Colors.Pink);
                r.SetValue(ModuleView.AreaNumberProperty, i);
                r.SetValue(ShapeType, shapeType.Selection);

                theCanvas.Children.Add(r);
                ModuleView nr = new ModuleView
                {
                    Label = "new",
                    Width = theSelection.selectedRectangles[i].Width,
                    Height = theSelection.selectedRectangles[i].Height,
                    Color = Utils.ColorToInt(Colors.Aquamarine),
                    CommandLine = ""
                };
                //r.MouseDown += theCanvas_MouseDown;
                //r.MouseLeave += R_MouseLeave;

                if (!dp.ShowNeurons())
                {
                    //TODO is any part of the rectangle visible?
                    int height = (int)r.Height;
                    int width = (int)r.Width;
                    float vRatio = (float)(r.Height / (float)nr.Height);
                    float hRatio = (float)(r.Width / (float)nr.Width);

                    if (height > 1 && width > 1)
                    {
                        theSelection.selectedRectangles[i].bitmap = new System.Windows.Media.Imaging.WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);

                        uint[] pixels = new uint[width * height]; ;
                        for (int x = 0; x < width; x++)
                            for (int y = 0; y < height; y++)
                            {
                                {
                                    int k = width * y + x;
                                    int index = theSelection.selectedRectangles[i].GetNeuronIndex((int)(x / hRatio), (int)(y / vRatio));
                                    Neuron n = MainWindow.theNeuronArray.GetNeuronForDrawing(index);
                                    uint val = (uint)Utils.ColorToInt(NeuronView.GetNeuronColor(n).Color);
                                    pixels[k] = val;
                                }
                            }
                        // apply pixels to bitmap
                        theSelection.selectedRectangles[i].bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);

                        Image img = new Image();
                        img.Source = theSelection.selectedRectangles[i].bitmap;
                        Canvas.SetLeft(img, Canvas.GetLeft(r));
                        Canvas.SetTop(img, Canvas.GetTop(r));
                        theCanvas.Children.Add(img);
                        img.SetValue(ModuleView.AreaNumberProperty, -i - 1);
                    }
                }
            }
            if (dragRectangle != null) theCanvas.Children.Add(dragRectangle);


            //highlight the "target" neuron
            SetTargetNeuronSymbol();

            //draw the neurons
            int synapseCount = 0;
            if (dp.ShowNeurons())
            {
                dp.GetRowColFromPoint(new Point(0, 0), out int startCol, out int startRow);
                startRow--;
                startCol--;
                dp.GetRowColFromPoint(new Point(theCanvas.ActualWidth, theCanvas.ActualHeight), out int endCol, out int endRow);
                endRow++;
                endCol++;
                if (startRow < 0) startRow = 0;
                if (startCol < 0) startCol = 0;
                if (endRow > Rows) endRow = Rows;
                if (endCol > columns) endCol = columns;
                for (int col = startCol; col < endCol; col++)
                {
                    List<Neuron> neuronColumn = null;
                    for (int row = startRow; row < endRow; row++)
                    {
                        int neuronID = dp.GetAbsNeuronAt(col, row);
                        if (neuronID >= 0 && neuronID < theNeuronArray.arraySize)
                        {
                            Neuron n;
                            if (MainWindow.useServers)
                            {
                                //buffering a column of neurons makes a HUGE performance difference
                                if (neuronColumn == null)
                                    neuronColumn = NeuronClient.GetNeurons(neuronID, endRow - startRow);
                                n = neuronColumn[row - startRow];
                            }
                            else
                            {
                                n = theNeuronArray.GetCompleteNeuron(neuronID);
                            }
                            UIElement l = NeuronView.GetNeuronView(n, this, out Label lbl);
                            if (l != null)
                            {
                                int canvas = neuronID % neuronCanvasCount;
                                neuronCanvas[canvas].Children.Add(l);

                                if (lbl != null && dp.ShowNeuronLabels())
                                {
                                    if (l is Shape s && s.Fill is SolidColorBrush b && b.Color == Colors.White)
                                        lbl.Foreground = new SolidColorBrush(Colors.Black);
                                    labelCanvas.Children.Add(lbl);
                                }

                                NeuronOnScreen neuronScreenCache = null;
                                if ((n.inUse || n.Label != "") && (l is Ellipse || l is Rectangle))
                                {
                                    neuronScreenCache = new NeuronOnScreen(neuronID, l, -10, lbl);
                                }

                                if (synapseCount < dp.maxSynapsesToDisplay &&
                                    dp.ShowSynapses() && (MainWindow.theNeuronArray.ShowSynapses || IsShowingSnapses(n.id)))
                                {
                                    if (MainWindow.useServers && n.inUse)
                                        n = theNeuronArray.AddSynapses(n);
                                    Point p1 = dp.pointFromNeuron(neuronID);

                                    if (n.synapses != null)
                                    {
                                        foreach (Synapse s in n.synapses)
                                        {
                                            Shape l1 = SynapseView.GetSynapseView(neuronID, p1, s, this);
                                            if (l1 != null)
                                            {
                                                if (IsShowingSnapses(n.id))
                                                    specialSynapsesCanvas.Children.Add(l1);
                                                else
                                                    allSynapsesCanvas.Children.Add(l1);
                                                synapseCount++;
                                                if (neuronScreenCache != null && s.model != Synapse.modelType.Fixed)
                                                {
                                                    if (neuronScreenCache.synapsesOnScreen == null)
                                                        neuronScreenCache.synapsesOnScreen = new List<NeuronOnScreen.synapseOnScreen>();
                                                    neuronScreenCache.synapsesOnScreen.Add(
                                                        new NeuronOnScreen.synapseOnScreen { target = s.targetNeuron, prevWeight = s.weight, graphic = l1 });
                                                }
                                            }
                                        }
                                    }
                                    if (n.SynapsesFrom != null)
                                    {
                                        foreach (Synapse s in n.SynapsesFrom)
                                        {
                                            //check the synapesFrom to draw synapes which source outside the window
                                            dp.GetAbsNeuronLocation(s.targetNeuron, out int x, out int y);
                                            if (x >= startCol && x < endCol && y >= startRow && y < endRow) continue;
                                            Point p2 = dp.pointFromNeuron(s.targetNeuron);
                                            Synapse s1 = new Synapse() { targetNeuron = neuronID, Weight = s.Weight };
                                            Shape l1 = SynapseView.GetSynapseView(s.targetNeuron, p2, s1, this);
                                            if (l1 != null)
                                                allSynapsesCanvas.Children.Add(l1);
                                        }
                                    }
                                }
                                if (neuronScreenCache != null)
                                    neuronsOnScreen.Add(neuronScreenCache);

                            }
                        }
                    }
                }
            }

            theCanvas.Children.Add(legendCanvas);

            theCanvas.Children.Add(allSynapsesCanvas);
            theCanvas.Children.Add(targetNeuronCanvas);
            for (int i = 0; i < neuronCanvasCount; i++)
            {
                theCanvas.Children.Add(neuronCanvas[i]);
            }

            theCanvas.Children.Add(specialSynapsesCanvas);
            theCanvas.Children.Add(labelCanvas);
            if (synapseShape != null) //synapse rubber-banding
                theCanvas.Children.Add(synapseShape);

            UpdateScrollbars();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            if (synapseCount >= dp.maxSynapsesToDisplay)
                MainWindow.thisWindow.SetStatus(0,"Too many synapses to display",1);
            else
            {
                if (!dp.ShowNeurons())
                    MainWindow.thisWindow.SetStatus(0,"Grid Size: " + (boxSize * boxSize).ToString("#,##"),1);
                else
                    MainWindow.thisWindow.SetStatus(0,"OK",0);
            }
            MainWindow.thisWindow.UpdateFreeMem();
            //Debug.WriteLine("Update Done " + elapsedMs + "ms");
        }


        //just update the colors of the neurons based on their current charge
        //and synapses based on current weight
        public void UpdateNeuronColors()
        {
            if (MainWindow.useServers)
            {
                int index = 0; //current index into neuronsOnScreen array
                int begin = 0; //beginning of a continuout sequences of neurons to retrieve
                while (index < neuronsOnScreen.Count)
                {
                    if (index == neuronsOnScreen.Count - 1 || neuronsOnScreen[index].neuronIndex + 1 != neuronsOnScreen[index + 1].neuronIndex)
                    {
                        List<Neuron> neuronColumn = NeuronClient.GetNeurons(neuronsOnScreen[begin].neuronIndex, index - begin + 1);
                        for (int i = 0; i < neuronColumn.Count; i++)
                        {
                            int nosIndex = i + begin;
                            if (nosIndex < neuronsOnScreen.Count && neuronColumn[i].lastCharge != neuronsOnScreen[nosIndex].prevValue)
                            {
                                neuronsOnScreen[nosIndex].prevValue = neuronColumn[i].lastCharge;
                                if (neuronsOnScreen[nosIndex].graphic is Shape e)
                                {
                                    e.Fill = NeuronView.GetNeuronColor(neuronColumn[i]);
                                }
                            }
                        }
                        begin = index + 1;
                    }
                    index++;
                }
            }
            else
            {
                //for small arrays, repaint everything so synapse weights will update
                if (false) //use this for testing of 
                //if (neuronsOnScreen.Count < 451 && scale == 1)
                {
                    Update();
                    if (MainWindow.theNeuronArray != null)
                    {
                        MainWindow.UpdateDisplayLabel(dp.NeuronDisplaySize);
                        MainWindow.UpdateEngineLabel((int)MainWindow.theNeuronArray.lastFireCount);
                    }
                    return;
                }

                SetTargetNeuronSymbol();

                for (int i = 0; i < neuronsOnScreen.Count; i++)
                {
                    NeuronOnScreen a = neuronsOnScreen[i];
                    Neuron n = MainWindow.theNeuronArray.GetNeuronForDrawing(a.neuronIndex);
                    if (neuronsOnScreen[i].synapsesOnScreen != null)
                    {
                        n.synapses = MainWindow.theNeuronArray.GetSynapsesList(n.id);
                        for (int j = 0; j < neuronsOnScreen[i].synapsesOnScreen.Count; j++)
                        {
                            NeuronOnScreen.synapseOnScreen sOnS = neuronsOnScreen[i].synapsesOnScreen[j];
                            foreach (Synapse s in n.synapses)
                            {
                                if (sOnS.target == s.targetNeuron)
                                {
                                    if (sOnS.prevWeight != s.weight)
                                    {
                                        sOnS.graphic.Stroke = sOnS.graphic.Fill = new SolidColorBrush(Utils.RainbowColorFromValue(s.weight));
                                        sOnS.prevWeight = s.weight;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    if (a.graphic is Shape e)
                    {
                        float x = n.lastCharge;
                        SolidColorBrush newColor = null;
                        if (x != a.prevValue)
                        {
                            a.prevValue = x;

                            newColor = NeuronView.GetNeuronColor(n);
                            e.Fill = newColor;
                            if (n.lastCharge != 0 && e.Fill.Opacity != 1)
                                e.Fill.Opacity = 1;
                        }

                        string newLabel = NeuronView.GetNeuronLabel(n);
                        if (newLabel.IndexOf('|') != -1) newLabel = newLabel.Substring(0, newLabel.IndexOf('|'));
                        if (a.label != null && newLabel != (string)a.label.Content)
                        {
                            a.label.Content = newLabel;
                            if (e.Fill.Opacity != 1)
                                e.Fill.Opacity = 1;
                        }
                        if (a.label == null && newLabel != "")
                        {
                            UIElement l = NeuronView.GetNeuronView(n, this, out Label lbl);
                            if (e.Fill.Opacity != 1)
                                e.Fill.Opacity = 1;
                            a.label = lbl;
                            theCanvas.Children.Add(lbl);
                        }
                        if (newColor != null && a.label != null)
                        {
                            if (newColor.Color == Colors.White)
                                a.label.Foreground = new SolidColorBrush(Colors.Black);
                            else
                                a.label.Foreground = new SolidColorBrush(Colors.White);
                        }
                    }
                }
            }

            //Update the content of modules on small display scales
            for (int i = 0; i < theSelection.selectedRectangles.Count; i++)
            {
                Rectangle r = theSelection.selectedRectangles[i].GetRectangle(dp);
                ModuleView nr = new ModuleView
                {
                    Label = "new",
                    Width = theSelection.selectedRectangles[i].Width,
                    Height = theSelection.selectedRectangles[i].Height,
                    Color = Utils.ColorToInt(Colors.Aquamarine),
                    CommandLine = ""
                };

                if (!dp.ShowNeurons())
                {
                    //TODO is any part of the rectangle visible?
                    int height = (int)r.Height;
                    int width = (int)r.Width;
                    float vRatio = (float)(r.Height / (float)nr.Height);
                    float hRatio = (float)(r.Width / (float)nr.Width);

                    uint[] pixels = new uint[width * height]; ;
                    for (int x = 0; x < width; x++)
                        for (int y = 0; y < height; y++)
                        {
                            {
                                int k = width * y + x;
                                int index = theSelection.selectedRectangles[i].GetNeuronIndex((int)(x / hRatio), (int)(y / vRatio));
                                Neuron n = MainWindow.theNeuronArray.GetNeuronForDrawing(index);
                                uint val = (uint)Utils.ColorToInt(NeuronView.GetNeuronColor(n).Color);
                                pixels[k] = val;
                            }
                        }
                    // apply pixels to bitmap
                    if (theSelection.selectedRectangles[i].bitmap != null)
                        try
                        {
                            theSelection.selectedRectangles[i].bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
                        }
                        catch (Exception)
                        {
                            //this exception can occur on a collision between Update and UpdateNeuronColors...
                            //if we ignore it, it may be benign.
                            //MessageBox.Show("Bitmap Write failed: " + e.Message);
                        }
                }
            }


            if (MainWindow.theNeuronArray != null)
            {
                MainWindow.UpdateDisplayLabel(dp.NeuronDisplaySize);
                MainWindow.UpdateEngineLabel((int)MainWindow.theNeuronArray.lastFireCount);
            }
        }

        private void SetTargetNeuronSymbol()
        {
            if (targetNeuronIndex != -1 && scale == 1)
            {
                Ellipse r = new Ellipse();
                Point p1 = dp.pointFromNeuron(targetNeuronIndex);
                r.Width = r.Height = dp.NeuronDisplaySize;
                Canvas.SetTop(r, p1.Y);
                Canvas.SetLeft(r, p1.X);
                r.Fill = new SolidColorBrush(Colors.LightBlue);
                targetNeuronCanvas.Children.Clear();
                targetNeuronCanvas.Children.Add(r);
            }
        }

        private void theCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            NeuronView.theCanvas = theCanvas;//??  
            SynapseView.theCanvas = theCanvas;//??
            if (MainWindow.IsArrayEmpty()) return;
            Update();
        }

        public static void SortAreas()
        {
            lock (MainWindow.theNeuronArray.Modules)
            {
                MainWindow.theNeuronArray.Modules.Sort();
            }
        }



    }
}
