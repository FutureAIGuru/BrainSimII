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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Threading;
using System.Collections;
using System.Reflection;
using BrainSimulator.Modules;
using System.Threading;

namespace BrainSimulator
{
    public partial class NeuronArrayView : UserControl
    {

        private DisplayParams dp = new DisplayParams();

        //for pan
        Point lastPositionOnCanvas = new Point(0, 0); //temp position used for calculating pan positions
        Point lastPositionOnGrid = new Point(0, 0); //temp position used for calculating pan positions
        Vector CanvasOffset = new Vector(0, 0);

        //for scrollbars
        double scrollBarVOldValue = 0;
        double scrollBarHOldValue = 0;
        DispatcherTimer scrollBarRepeatTimer = new DispatcherTimer();

        //these are used to handle scaling with the mouse wheel
        DispatcherTimer zoomRepeatTimer = new DispatcherTimer();
        float scale = 1;

        //for cut-copy-paste
        public NeuronArray myClipBoard = null; //refactor back to private
        public int targetNeuronIndex = -1;

        //needed for handling selections of areas of neurons
        Rectangle dragRectangle = null;
        int firstSelectedNeuron = -1;
        int lastSelectedNeuron = -1;

        //keeps track of the multiple selection areas
        public NeuronSelection theSelection = new NeuronSelection();

        int Rows { get { return MainWindow.theNeuronArray.rows; } }

        //this helper class keeps track of the neurons on the screen so they can change color without repainting
        private List<NeuronOnScreen> neuronsOnScreen = new List<NeuronOnScreen>();
        public class NeuronOnScreen
        {
            public int neuronIndex; public UIElement graphic; public float prevValue; public Label label;
            public NeuronOnScreen(int index, UIElement e, float value, Label Label)
            {
                neuronIndex = index; graphic = e; prevValue = value; label = Label;
            }
        };

        public NeuronArrayView()
        {
            InitializeComponent();
            zoomRepeatTimer.Tick += Dt_Tick;
#if DEBUG
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
#endif
        }

        public DisplayParams Dp { get => dp; set => dp = value; }

        //refresh the display of the neuron network
        public void Update()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            NeuronArray theNeuronArray = MainWindow.theNeuronArray;

            Canvas labelCanvas = new Canvas();
            Canvas.SetLeft(labelCanvas, 0);
            Canvas.SetTop(labelCanvas, 0);

            Canvas synapseCanvas = new Canvas();
            Canvas.SetLeft(synapseCanvas, 0);
            Canvas.SetTop(synapseCanvas, 0);

            int neuronCanvasCount = 2;
            Canvas[] neuronCanvas = new Canvas[neuronCanvasCount];
            for (int i = 0; i < neuronCanvasCount; i++)
                neuronCanvas[i] = new Canvas();

            Canvas legendCanvas = new Canvas();
            Canvas.SetLeft(legendCanvas, 0);
            Canvas.SetTop(legendCanvas, 0);



            if (MainWindow.theNeuronArray == null) return;

            //Debug.WriteLine("Update " + MainWindow.theNeuronArray.Generation);
            dp.NeuronRows = MainWindow.theNeuronArray.rows;
            theCanvas.Children.Clear();
            neuronsOnScreen.Clear();
            int columns = MainWindow.theNeuronArray.arraySize / dp.NeuronRows;

            //draw any module connectors (not implemented)
            //DrawModuleConnectors();

            //draw some background grid and labels
            int boxSize = 250;

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
            for (int i = 0; i < theNeuronArray.rows && false; i += boxSize)
            {
                for (int j = 0; j < columns; j += boxSize)
                {
                    Point p = new Point((j + boxSize/2) * dp.NeuronDisplaySize, (i + boxSize/2) * dp.NeuronDisplaySize);
                    p += (Vector)dp.DisplayOffset;
                    Label l = new Label();
                    l.Content = refNo++;
                    l.FontSize = dp.NeuronDisplaySize * 10;
                    l.Foreground = Brushes.White;
                    l.HorizontalAlignment = HorizontalAlignment.Center;
                    l.VerticalAlignment = VerticalAlignment.Center;
                    Canvas.SetLeft(l, p.X);
                    Canvas.SetTop(l, p.Y);
                    legendCanvas.Children.Add(l);

                }
            }


            //draw the module rectangles
            for (int i = 0; i < MainWindow.theNeuronArray.Modules.Count; i++)
            {
                ModuleView nr = MainWindow.theNeuronArray.Modules[i];
                NeuronSelectionRectangle nsr = new NeuronSelectionRectangle(nr.FirstNeuron, nr.Width, nr.Height);
                Rectangle r = nsr.GetRectangle(dp);
                r.Fill = new SolidColorBrush(Utils.IntToColor(nr.Color));
                r.MouseDown += theCanvas_MouseDown;
                theCanvas.Children.Add(r);
                TextBlock tb = new TextBlock();
                tb.Text = nr.Label;
                tb.Background = new SolidColorBrush(Colors.White);
                Canvas.SetLeft(tb, Canvas.GetLeft(r));
                Canvas.SetTop(tb, Canvas.GetTop(r));
                theCanvas.Children.Add(tb);
                ModuleView.CreateContextMenu(i, nr, r);
                r.MouseLeave += R_MouseLeave;
            }

            //draw any selection rectangle(s)
            for (int i = 0; i < theSelection.selectedRectangles.Count; i++)
            {
                Rectangle r = theSelection.selectedRectangles[i].GetRectangle(dp);
                r.Fill = new SolidColorBrush(Colors.Pink);

                theCanvas.Children.Add(r);
                ModuleView nr = new ModuleView
                {
                    Label = "new",
                    Width = theSelection.selectedRectangles[i].Width,
                    Height = theSelection.selectedRectangles[i].Height,
                    Color = Utils.ColorToInt(Colors.Aquamarine),
                    CommandLine = ""
                };
                ModuleView.CreateContextMenu(-i - 1, nr, r);
                r.MouseLeave += R_MouseLeave;
            }

            //highlight the "target" neuron
            if (targetNeuronIndex != -1)
            {
                Rectangle r = new Rectangle();
                Point p1 = dp.pointFromNeuron(targetNeuronIndex);
                r.Width = r.Height = dp.NeuronDisplaySize;
                Canvas.SetTop(r, p1.Y);
                Canvas.SetLeft(r, p1.X);
                r.Fill = new SolidColorBrush(Colors.LightBlue);
                theCanvas.Children.Add(r);
            }

            //draw the neurons
            if (dp.ShowNeurons())
            {
                dp.GetRowColFromPoint(new Point(0, 0), out int startCol, out int startRow);
                startRow--;
                startCol--;
                dp.GetRowColFromPoint(new Point(theCanvas.ActualWidth, theCanvas.ActualHeight), out int endCol, out int endRow);
                endRow++;
                endCol++;

                for (int col = startCol; col < endCol; col++)
                {
                    for (int row = startRow; row < endRow; row++)
                    {
                        int neuronID = dp.GetAbsNeuronAt(col, row);
                        if (neuronID >= 0 && neuronID < theNeuronArray.arraySize)
                        {
                            UIElement l = NeuronView.GetNeuronView(neuronID, this, out Label lbl);
                            if (l != null)
                            {
                                int canvas = neuronID % neuronCanvasCount;
                                neuronCanvas[canvas].Children.Add(l);
                                if (l is Ellipse || l is Rectangle)
                                    neuronsOnScreen.Add(new NeuronOnScreen(neuronID, l, -10, lbl));
                                if (lbl != null && dp.ShowNeuronLabels())
                                {
                                    labelCanvas.Children.Add(lbl);
                                }
                                if (MainWindow.theNeuronArray.ShowSynapses && dp.ShowSynapses())
                                {
                                    Point p1 = dp.pointFromNeuron(neuronID);
                                    Neuron n = MainWindow.theNeuronArray.GetNeuron(neuronID);
                                    if (n.Synapses != null)
                                    {
                                        foreach (Synapse s in n.Synapses)
                                        {
                                            Shape l1 = SynapseView.GetSynapseView(neuronID, p1, s, this);
                                            if (l1 != null)
                                                synapseCanvas.Children.Add(l1);
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
                                                synapseCanvas.Children.Add(l1);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            theCanvas.Children.Add(legendCanvas);

            for (int i = 0; i < neuronCanvasCount; i++)
            {
                theCanvas.Children.Add(neuronCanvas[i]);
            }

            theCanvas.Children.Add(synapseCanvas);
            theCanvas.Children.Add(labelCanvas);

            UpdateScrollbars();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Debug.WriteLine("Update Done " + elapsedMs + "ms");
        }

        //we might enable this some day
        private void DrawModuleConnectors()
        {
            for (int i = 0; i < MainWindow.theNeuronArray.Modules.Count; i++)
            {
                ModuleView nr = MainWindow.theNeuronArray.Modules[i];
                Point GetCenter(ModuleView na)
                {
                    Point p1 = dp.pointFromNeuron(na.FirstNeuron);
                    p1.X += dp.NeuronDisplaySize / 2;
                    p1.Y += dp.NeuronDisplaySize / 2;
                    return p1;
                }

                //parse parameters
                string[] commands = nr.CommandLine.Split(' ');
                for (int j = 1; j < commands.Length - 1; j += 2)
                {
                    if (commands[j] == "-o")
                    {
                        ModuleView naTarget = MainWindow.theNeuronArray.FindAreaByLabel(commands[j + 1]);
                        if (naTarget != null)
                        {
                            Shape s = SynapseView.DrawLinkArrow(GetCenter(nr), GetCenter(naTarget));
                            s.StrokeThickness = dp.NeuronDisplaySize / 2;
                            s.Stroke = new SolidColorBrush(Colors.White);
                            s.StrokeEndLineCap = PenLineCap.Triangle;
                            s.StrokeStartLineCap = PenLineCap.Round;
                            s.Opacity = 1;
                            theCanvas.Children.Add(s);
                        }
                    }
                }
            }
        }

        private void R_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!dragging && theCanvas.Cursor != Cursors.Hand)
                theCanvas.Cursor = Cursors.Cross;
        }


        //just update the colors of the neurons based on their current charge
        public void UpdateNeuronColors()
        {
            for (int i = 0; i < neuronsOnScreen.Count; i++)
            {
                NeuronOnScreen a = neuronsOnScreen[i];
                Neuron n = MainWindow.theNeuronArray.GetNeuron(a.neuronIndex);
                if (a.graphic is Shape e)
                {
                    float x = n.LastCharge;
                    if (x != a.prevValue)// ||
                                         //    (a.label != null && n.Label != (string)a.label.Content) ||
                                         //    (a.label == null && n.Label != ""))
                    {
                        a.prevValue = x;
                        UIElement el = NeuronView.GetNeuronView(a.neuronIndex, this, out Label lbl);
                        if (el is Shape l1)
                        {
                            e.Fill = l1.Fill;
                        }
                        //if (a.label != null && (string)a.label.Content != n.Label)
                        //{
                        //    a.label.Content = n.Label;
                        //}
                        //if (a.label == null && n.Label != "")
                        //{
                        //    a.label = lbl;
                        //    theCanvas.Children.Add(lbl);
                        //}
                        //if (a.label != null && n.Label == "")
                        //{
                        //    theCanvas.Children.Remove(a.label);
                        //    a.label = null;
                        //}
                    }
                }
            }
            if (MainWindow.theNeuronArray != null)
                MainWindow.UpdateDisplayLabel(dp.NeuronDisplaySize, (int)MainWindow.theNeuronArray.lastFireCount);
        }


        public void CancelSynapseRubberband()
        {
            mouseDownNeuronIndex = -1;
            if (synapseShape != null)
                theCanvas.Children.Remove(synapseShape);
            synapseShape = null;
        }

        public bool dragging = false;
        int mouseDownNeuronIndex = -1;
        static Shape synapseShape = null;  //the shape of the synapses being rubber-banded 
                                           //(so it can be added/removed easily from the canvas)

        public void theCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (MainWindow.theNeuronArray == null) return;
            //Debug.WriteLine("theCanvas_MouseDown" + MainWindow.theNeuronArray.Generation);
            Point currentPosition = e.GetPosition(theCanvas);
            LimitMousePostion(ref currentPosition);
            mouseDownNeuronIndex = dp.NeuronFromPoint(currentPosition);

            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (sender is Label l)
                {
                    l.ContextMenu = new ContextMenu();
                    Neuron n1 = MainWindow.theNeuronArray.GetNeuron(mouseDownNeuronIndex);
                    NeuronView.CreateContextMenu(mouseDownNeuronIndex, n1, l.ContextMenu);
                    l.ContextMenu.IsOpen = true;
                    e.Handled = true;
                }
                else if (sender is Shape s)
                {

                    if (s.ContextMenu == null && (s is Path || s is Line ||
                        (s is Ellipse && (int)s.GetValue(SynapseView.SourceIDProperty) != 0))) // a synapse
                    {
                        int source = (int)s.GetValue(SynapseView.SourceIDProperty);
                        int target = (int)s.GetValue(SynapseView.TargetIDProperty);
                        float weight = (float)s.GetValue(SynapseView.WeightValProperty);
                        Synapse s1 = MainWindow.theNeuronArray.GetNeuron(source).FindSynapse(target);

                        //                        Synapse s1 = new Synapse(target, weight);
                        s.ContextMenu = new ContextMenu();
                        SynapseView.CreateContextMenu(source, s1, s.ContextMenu);
                    }
                    else if (s is Ellipse) //|| s is Rectangle) // a neuron (this will update every time
                    {
                        s.ContextMenu = new ContextMenu();
                        Neuron n1 = MainWindow.theNeuronArray.GetNeuron(mouseDownNeuronIndex);
                        NeuronView.CreateContextMenu(mouseDownNeuronIndex, n1, s.ContextMenu);
                    }
                    if (s.ContextMenu != null)
                        s.ContextMenu.IsOpen = true;
                    e.Handled = true;
                }
                else
                {
                }
                return;
            }

            Neuron n = null;
            if (mouseDownNeuronIndex >= 0 && mouseDownNeuronIndex < MainWindow.theNeuronArray.GetArraySize())
                n = MainWindow.theNeuronArray.GetNeuron(mouseDownNeuronIndex) as Neuron;

            if (theCanvas.Cursor == Cursors.Cross)
            {
                //          Debug.WriteLine("dragStart" + MainWindow.theNeuronArray.Generation);
                if (dragRectangle != null)
                {
                    theCanvas.Children.Remove(dragRectangle);
                }

                if (!MainWindow.ctrlPressed)
                {
                    theSelection.selectedRectangles.Clear();
                }
                else Update();

                //snap to neuron point
                currentPosition = dp.pointFromNeuron(mouseDownNeuronIndex);

                //build the draggable selection rectangle
                dragRectangle = new Rectangle();
                dragRectangle.Width = dragRectangle.Height = dp.NeuronDisplaySize;
                dragRectangle.Stroke = new SolidColorBrush(Colors.Red);
                dragRectangle.Fill = new SolidColorBrush(Colors.Red);
                dragRectangle.Fill.Opacity = 0.5;
                Canvas.SetLeft(dragRectangle, currentPosition.X);
                Canvas.SetTop(dragRectangle, currentPosition.Y);
                theCanvas.Children.Add(dragRectangle);
                firstSelectedNeuron = mouseDownNeuronIndex;
                Mouse.Capture(theCanvas);
            }

            if (theCanvas.Cursor == Cursors.ScrollAll)
            {
                dragging = true;
            }
            if (theCanvas.Cursor == Cursors.UpArrow)
            {
                Mouse.Capture(theCanvas);
                if (mouseRepeatTimer == null)
                    mouseRepeatTimer = new DispatcherTimer();
                if (mouseRepeatTimer.IsEnabled)
                    mouseRepeatTimer.Stop();
                mouseRepeatTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);
                mouseRepeatTimer.Tick += MouseRepeatTimer_Tick;
                mouseRepeatTimer.Start();
                dragging = true;
            }
            if (theCanvas.Cursor == Cursors.Hand)
            {
                StartPan(e.GetPosition((UIElement)theCanvas.Parent));
                Mouse.Capture(theCanvas);
            }
        }

        private void MouseRepeatTimer_Tick(object sender, EventArgs e)
        {
            if (mouseDownNeuronIndex < 0) return;
            Neuron n = MainWindow.theNeuronArray.GetNeuron(mouseDownNeuronIndex);
            n.SetValue(1);
            mouseRepeatTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
        }

        DispatcherTimer mouseRepeatTimer = null;

        public float lastSynapseWeight = 1.0f;
        public void theCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mouseRepeatTimer != null) mouseRepeatTimer.Stop();
            if (MainWindow.theNeuronArray == null) return;
            //Debug.WriteLine("theCanvas_MouseUp" + MainWindow.theNeuronArray.Generation);
            if (e.ChangedButton == MouseButton.Right) return;
            Point currentPosition = e.GetPosition(theCanvas);
            LimitMousePostion(ref currentPosition);
            int mouseUpNeuronIndex = dp.NeuronFromPoint(currentPosition);


            if (theCanvas.Cursor == Cursors.Cross)
            {
                FinishSelection();
                Update();
            }

            //we clicked on a neuron...is it a fire or a synapse drag?
            if (theCanvas.Cursor == Cursors.UpArrow)
            {
                if (synapseShape == null && mouseDownNeuronIndex > -1)  //if no synapseshape exists, then were just clicking in a nueron
                {
                    Neuron n = MainWindow.theNeuronArray.GetNeuron(mouseDownNeuronIndex);
                    if (n != null)
                    {
                        if (n.LastCharge < .99)
                        {
                            n.CurrentCharge = 1;
                            MainWindow.theNeuronArray.AddToFiringQueue(n.Id);
                        }
                        else
                        {
                            n.CurrentCharge = n.LastCharge = 0;
                        }
                        e.Handled = true;
                    }
                }
                else
                {
                    if (mouseDownNeuronIndex > -1)
                    {
                        Point p1 = e.GetPosition(theCanvas);
                        LimitMousePostion(ref p1);
                        int index = dp.NeuronFromPoint(p1);
                        MainWindow.theNeuronArray.GetNeuron(mouseDownNeuronIndex).AddSynapse(index, lastSynapseWeight, MainWindow.theNeuronArray, true);
                    }
                    synapseShape = null;
                    mouseDownNeuronIndex = -1;
                    e.Handled = true;
                    Update();
                }
            }

            if (theCanvas.Cursor == Cursors.Hand)
            {
                FinishPan();
                e.Handled = true;
            }
            mouseDownNeuronIndex = -1;
            dragging = false;
            Mouse.Capture(null);
        }


        private void FinishSelection()
        {
            if (dragRectangle != null)
            {
                try
                {
                    //get the neuron pointers from the drag rectangle and save in the selection array
                    Point p1 = new Point(Canvas.GetLeft(dragRectangle), Canvas.GetTop(dragRectangle));
                    Point p2 = new Point(p1.X + dragRectangle.Width - 1, p1.Y + dragRectangle.Height - 1);
                    firstSelectedNeuron = dp.NeuronFromPoint(p1);
                    lastSelectedNeuron = dp.NeuronFromPoint(p2);
                    int w = 1 + (lastSelectedNeuron - firstSelectedNeuron) / Rows;
                    int h = 1 + (lastSelectedNeuron - firstSelectedNeuron) % Rows;
                    Debug.Write(firstSelectedNeuron + ", " + lastSelectedNeuron);
                    NeuronSelectionRectangle rr = new NeuronSelectionRectangle(firstSelectedNeuron, w, h);
                    theSelection.selectedRectangles.Add(rr);
                }
                catch
                {
                    dragRectangle = null;
                }
                dragRectangle = null;
            }
        }

        //keep any mouse operations within the bounds of the neruon array
        private void LimitMousePostion(ref Point p1)
        {
            if (p1.X < dp.DisplayOffset.X) p1.X = dp.DisplayOffset.X;
            if (p1.Y < dp.DisplayOffset.Y) p1.Y = dp.DisplayOffset.Y;
            float width = dp.NeuronDisplaySize * MainWindow.theNeuronArray.GetArraySize() / dp.NeuronRows - 1;
            float height = dp.NeuronDisplaySize * dp.NeuronRows - 1;
            if (p1.X > dp.DisplayOffset.X + width) p1.X = dp.DisplayOffset.X + width;
            if (p1.Y > dp.DisplayOffset.Y + height) p1.Y = dp.DisplayOffset.Y + height;

        }

        ModuleView na = null;
        public void theCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseRepeatTimer != null)
            {
                if (mouseRepeatTimer.IsEnabled && mouseRepeatTimer.Interval == new TimeSpan(0, 0, 0, 0, 100)) return;
                mouseRepeatTimer.Stop();
            }
            if (MainWindow.theNeuronArray == null) return;
            if (e.RightButton == MouseButtonState.Pressed) return;

            Point currentPosition = e.GetPosition(theCanvas);
            LimitMousePostion(ref currentPosition);
            int currentNeuron = dp.NeuronFromPoint(currentPosition);

            //are we dragging a synapse? rubber-band it
            if (e.LeftButton == MouseButtonState.Pressed && theCanvas.Cursor == Cursors.UpArrow && dragging)
            {
                if (mouseDownNeuronIndex > -1)
                {
                    if (synapseShape != null || (mouseDownNeuronIndex != currentNeuron))
                    {
                        if (synapseShape != null)
                            theCanvas.Children.Remove(synapseShape);
                        Shape l = SynapseView.GetSynapseShape(dp.pointFromNeuron(mouseDownNeuronIndex), dp.pointFromNeuron(currentNeuron), this);
                        theCanvas.Children.Add(l);
                        synapseShape = l;
                    }
                }
            }
            else //we may have missed a mouse-up event...clear out the rubber-banding
            {
                synapseShape = null;
                mouseDownNeuronIndex = -1;
            }

            if (theCanvas.Cursor == Cursors.Cross || theCanvas.Cursor == Cursors.ScrollN || theCanvas.Cursor == Cursors.ScrollS || theCanvas.Cursor == Cursors.ScrollE ||
                theCanvas.Cursor == Cursors.ScrollW || theCanvas.Cursor == Cursors.ScrollNW || theCanvas.Cursor == Cursors.ScrollNE ||
                theCanvas.Cursor == Cursors.ScrollSW || theCanvas.Cursor == Cursors.ScrollSE || theCanvas.Cursor == Cursors.ScrollAll)
            {
                //set the cursor if are we inside an existing module rectangle?
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    na = null;
                    ////is the mouse in a module?
                    for (int i = 0; i < MainWindow.theNeuronArray.modules.Count; i++)
                    {
                        Rectangle r = MainWindow.theNeuronArray.modules[i].GetRectangle(dp);
                        double left = Canvas.GetLeft(r);
                        double top = Canvas.GetTop(r);
                        if (SetScrollCursor(currentPosition, r, left, top))
                        {
                            na = MainWindow.theNeuronArray.modules[i];
                            firstSelectedNeuron = currentNeuron;
                        }
                    }
                }

                //handle the creation/updating of a selection rectangle
                if (e.LeftButton == MouseButtonState.Pressed && dragRectangle != null)
                {
                    //swap to keep the rectangle in positive territory
                    Point p1 = dp.pointFromNeuron(firstSelectedNeuron);
                    Point p2 = dp.pointFromNeuron(currentNeuron);
                    if (p1.X > p2.X)
                    {
                        double temp = p1.X; p1.X = p2.X; p2.X = temp;
                    }
                    if (p1.Y > p2.Y)
                    {
                        double temp = p1.Y; p1.Y = p2.Y; p2.Y = temp;
                    }

                    //snap mouse position to neuron
                    currentNeuron = dp.NeuronFromPoint(p2);
                    p2 = dp.pointFromNeuron(currentNeuron);

                    //update graphic rectangle 
                    dragRectangle.Width = p2.X - p1.X + dp.NeuronDisplaySize;
                    dragRectangle.Height = p2.Y - p1.Y + dp.NeuronDisplaySize;
                    Canvas.SetLeft(dragRectangle, p1.X);
                    Canvas.SetTop(dragRectangle, p1.Y);
                    if (!theCanvas.Children.Contains(dragRectangle)) theCanvas.Children.Add(dragRectangle);
                }
            }

            //handle moving of a module
            if (e.LeftButton == MouseButtonState.Pressed && theCanvas.Cursor == Cursors.ScrollAll && na != null)
            {
                if (currentNeuron != firstSelectedNeuron)
                {
                    int newFirst = na.FirstNeuron + currentNeuron - firstSelectedNeuron;
                    int newLast = na.LastNeuron + currentNeuron - firstSelectedNeuron;
                    na.GetAbsNeuronLocation(newFirst, out int xf, out int yf);
                    na.GetAbsNeuronLocation(newLast, out int xl, out int yl);

                    if (newFirst >= 0 && newLast < MainWindow.theNeuronArray.arraySize &&
                            xf <= xl && yf <= yl)
                    {
                        //move all the neurons
                        int delta = currentNeuron - firstSelectedNeuron;
                        if (delta > 0) //move all the nerons...opposite order depending on the direction of the move
                        {
                            for (int i = na.NeuronCount - 1; i >= 0; i--)
                            {
                                Neuron src = na.GetNeuronAt(i);
                                Neuron dest = MainWindow.theNeuronArray.GetNeuron(src.Id + delta);
                                MainWindow.thisWindow.theNeuronArrayView.MoveOneNeuron(src, dest);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < na.NeuronCount; i++)
                            {
                                Neuron src = na.GetNeuronAt(i);
                                Neuron dest = MainWindow.theNeuronArray.GetNeuron(src.Id + delta);
                                MainWindow.thisWindow.theNeuronArrayView.MoveOneNeuron(src, dest);
                            }
                        }

                        //move the box
                        na.FirstNeuron += currentNeuron - firstSelectedNeuron;
                        SortAreas();
                        Update();
                    }
                    firstSelectedNeuron = currentNeuron;
                }
            }
            //handle sizing of a module
            if (e.LeftButton == MouseButtonState.Pressed && na != null &&
                (theCanvas.Cursor == Cursors.ScrollN ||
                theCanvas.Cursor == Cursors.ScrollS ||
                theCanvas.Cursor == Cursors.ScrollE ||
                theCanvas.Cursor == Cursors.ScrollW ||
                theCanvas.Cursor == Cursors.ScrollNW ||
                theCanvas.Cursor == Cursors.ScrollNE ||
                theCanvas.Cursor == Cursors.ScrollSW ||
              theCanvas.Cursor == Cursors.ScrollSE)
                )
            {
                //TODO: Add rearrangement of neurons
                //TODO: Add clone of neurons to handle ALL properties
                dragging = true;
                na.GetBounds(out int X1, out int Y1, out int X2, out int Y2);
                na.GetAbsNeuronLocation(firstSelectedNeuron, out int Xf, out int Yf);
                na.GetAbsNeuronLocation(currentNeuron, out int Xc, out int Yc);
                na.GetAbsNeuronLocation(na.LastNeuron, out int Xl, out int Yl);
                int minHeight = na.TheModule.MinHeight;
                int minWidth = na.TheModule.MinWidth;

                //move the top?
                if (theCanvas.Cursor == Cursors.ScrollN || theCanvas.Cursor == Cursors.ScrollNE || theCanvas.Cursor == Cursors.ScrollNW)
                {
                    if (Yc != Yf)
                    {
                        int newTop = Y1 + Yc - Yf;
                        if (newTop <= Y2)
                        {
                            na.Height -= Yc - Yf;
                            if (na.Height < minHeight)
                                na.Height = minHeight;
                            else
                            {
                                na.FirstNeuron += Yc - Yf;
                                firstSelectedNeuron = currentNeuron;
                            }
                            SortAreas();
                            Update();
                        }
                    }
                }
                //move the left?
                if (theCanvas.Cursor == Cursors.ScrollW || theCanvas.Cursor == Cursors.ScrollNW || theCanvas.Cursor == Cursors.ScrollSW)
                {
                    if (Xc != Xf)
                    {
                        int newLeft = X1 + Xc - Xf;
                        if (newLeft <= X2)
                        {
                            na.Width -= Xc - Xf;
                            if (na.Width < minWidth)
                                na.Width = minWidth;
                            else
                            {
                                na.FirstNeuron += (Xc - Xf) * MainWindow.theNeuronArray.rows;
                                firstSelectedNeuron = currentNeuron;
                            }
                            SortAreas();
                            Update();
                        }
                    }
                }
                //Move the Right
                if (theCanvas.Cursor == Cursors.ScrollE || theCanvas.Cursor == Cursors.ScrollNE || theCanvas.Cursor == Cursors.ScrollSE)
                {
                    if (Xc != Xf)
                    {
                        int newRight = X2 + Xc - Xf;
                        if (newRight >= X1)
                        {
                            na.Width += Xc - Xf;
                            if (na.Width < minWidth)
                                na.Width = minWidth;
                            else
                                firstSelectedNeuron = currentNeuron;
                            Update();
                        }
                    }
                }
                //Move the Bottom
                if (theCanvas.Cursor == Cursors.ScrollS || theCanvas.Cursor == Cursors.ScrollSE || theCanvas.Cursor == Cursors.ScrollSW)
                {
                    if (Yc != Yf)
                    {
                        int newBottom = Y2 + Yc - Yf;
                        if (newBottom >= Y1)
                        {
                            na.Height += Yc - Yf;
                            if (na.Height < minHeight)
                                na.Height = minHeight;
                            else
                                firstSelectedNeuron = currentNeuron;
                            Update();
                        }
                    }
                }
            }

            if (theCanvas.Cursor == Cursors.Hand)
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    ContinuePan(e.GetPosition((UIElement)theCanvas.Parent));
                    //lastPositionOnGrid = e.GetPosition((UIElement)theCanvas.Parent);
                }
                else
                {
                    lastPositionOnCanvas = new Point(0, 0);
                }
        }

        public static void SortAreas()
        {
            lock (MainWindow.theNeuronArray.Modules)
            {
                MainWindow.theNeuronArray.Modules.Sort();
            }
        }

        private bool SetScrollCursor(Point currentPosition, Rectangle r, double left, double top)
        {
            if (currentPosition.X > left && currentPosition.X < left + r.Width &&
                currentPosition.Y > top && currentPosition.Y < top + r.Height)
            {
                double edgeToler = dp.NeuronDisplaySize / 2.5;
                bool nearTop = currentPosition.Y - top < edgeToler;
                bool nearBottom = top + r.Height - currentPosition.Y < edgeToler;
                bool nearLeft = currentPosition.X - left < edgeToler;
                bool nearRight = left + r.Width - currentPosition.X < edgeToler;
                if (nearTop && nearLeft) theCanvas.Cursor = Cursors.ScrollNW;
                else if (nearTop && nearRight) theCanvas.Cursor = Cursors.ScrollNE;
                else if (nearBottom && nearLeft) theCanvas.Cursor = Cursors.ScrollSW;
                else if (nearBottom && nearRight) theCanvas.Cursor = Cursors.ScrollSE;
                else if (nearTop) theCanvas.Cursor = Cursors.ScrollN;
                else if (nearBottom) theCanvas.Cursor = Cursors.ScrollS;
                else if (nearLeft) theCanvas.Cursor = Cursors.ScrollW;
                else if (nearRight) theCanvas.Cursor = Cursors.ScrollE;
                else theCanvas.Cursor = Cursors.ScrollAll;
                return true;
            }
            return false;
        }
        const float minZoom = 0.5f;
        public void Zoom(int change)
        {
            dp.DisplayOffset = (Point)(((Vector)dp.DisplayOffset) * (dp.NeuronDisplaySize + change) / dp.NeuronDisplaySize);
            dp.NeuronDisplaySize += change;
            if (dp.NeuronDisplaySize < minZoom) dp.NeuronDisplaySize = minZoom;
            Update();
        }
        public void theCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!MainWindow.shiftPressed) return;
            //zoom in-out the display
            float oldNeuronDisplaySize = dp.NeuronDisplaySize;
            dp.NeuronDisplaySize += e.Delta / 60;
            if (dp.NeuronDisplaySize < minZoom) dp.NeuronDisplaySize = minZoom;
            Point mousePostion = e.GetPosition(theCanvas);
            Vector v = (Vector)mousePostion;
            v -= (Vector)dp.DisplayOffset;
            v *= dp.NeuronDisplaySize / oldNeuronDisplaySize;
            dp.DisplayOffset = mousePostion - v;

            scale *= dp.NeuronDisplaySize / oldNeuronDisplaySize;
            ScaleTransform st = new ScaleTransform(scale, scale, mousePostion.X, mousePostion.Y);
            theCanvas.RenderTransform = st;

            //start a timer to do an update so we don't get an update for every wheel click
            zoomRepeatTimer.Stop();
            zoomRepeatTimer.Interval = TimeSpan.FromMilliseconds(250);
            zoomRepeatTimer.Start();
            MainWindow.UpdateDisplayLabel(dp.NeuronDisplaySize, 0);
        }

        private void Dt_Tick(object sender, EventArgs e)
        {
            zoomRepeatTimer.Stop();
            scale = 1;
            theCanvas.RenderTransform = new ScaleTransform(1, 1);
            Update();
        }

        //sets neuron 0 to the upper left of the neuron array display
        public void Origin()
        {
            dp.DisplayOffset = new Point(0, 0);
            dp.NeuronDisplaySize = 25;
        }

        private void theCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            NeuronView.theCanvas = theCanvas;//??  
            SynapseView.theCanvas = theCanvas;//??
            if (MainWindow.theNeuronArray == null) return;
            Update();
        }

        private void theCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            if (MainWindow.theNeuronArray == null) return;
        }

        private void TheCanvas_MouseEnter(object sender, MouseEventArgs e)
        {
            if (theCanvas.Cursor != Cursors.Hand)
                theCanvas.Cursor = Cursors.Cross;
        }

        private void TheCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            theCanvas.Cursor = Cursors.Arrow;
        }

        //set up the scrollbars to match the coordinates of the display
        private void UpdateScrollbars()
        {
            NeuronArray theNeuronArray = MainWindow.theNeuronArray;
            double totalWidth = theNeuronArray.arraySize / theNeuronArray.rows * dp.NeuronDisplaySize;
            double visibleWidth = theCanvas.ActualWidth;
            scrollBarH.Minimum = -visibleWidth + 3 * dp.NeuronDisplaySize;
            scrollBarH.Maximum = totalWidth - 3 * dp.NeuronDisplaySize;
            scrollBarH.Value = -dp.DisplayOffset.X;
            scrollBarH.ViewportSize = visibleWidth;
            scrollBarH.SmallChange = dp.NeuronDisplaySize * 1.1;
            scrollBarH.LargeChange = visibleWidth;
            scrollBarHOldValue = scrollBarH.Value;

            double totalHeight = theNeuronArray.rows * dp.NeuronDisplaySize;
            double visibleHeight = theCanvas.ActualHeight;
            scrollBarV.Minimum = -visibleHeight + 3 * dp.NeuronDisplaySize;
            scrollBarV.Maximum = totalHeight - 3 * dp.NeuronDisplaySize;
            scrollBarV.Value = -dp.DisplayOffset.Y;
            scrollBarV.ViewportSize = visibleHeight;
            scrollBarV.SmallChange = dp.NeuronDisplaySize * 1.1;
            scrollBarV.LargeChange = visibleHeight;
            scrollBarVOldValue = scrollBarV.Value;
        }


        private void ScrollBarH_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            double value = e.NewValue;
            Point simMousePosition = new Point(-scrollBarHOldValue, -scrollBarVOldValue);
            if (!scrollBarRepeatTimer.IsEnabled)
            {
                scrollBarRepeatTimer.Tick += ScrollBarRepeatTimer_Tick;
                scrollBarRepeatTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
                StartPan(simMousePosition);
            }
            simMousePosition.X = -value;
            scrollBarHOldValue = value;
            ContinuePan(simMousePosition);
            scrollBarRepeatTimer.IsEnabled = false;
            scrollBarRepeatTimer.Start();
        }

        private void ScrollBarV_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            double value = e.NewValue;
            Point simMousePosition = new Point(-scrollBarHOldValue, -scrollBarVOldValue);
            if (!scrollBarRepeatTimer.IsEnabled)
            {
                scrollBarRepeatTimer.Tick += ScrollBarRepeatTimer_Tick;
                scrollBarRepeatTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
                StartPan(simMousePosition);
            }
            simMousePosition.Y = -value;
            scrollBarVOldValue = value;
            ContinuePan(simMousePosition);
            scrollBarRepeatTimer.IsEnabled = false;
            scrollBarRepeatTimer.Start();
        }

        private void ScrollBarRepeatTimer_Tick(object sender, EventArgs e)
        {
            if (!scrollBarRepeatTimer.IsEnabled) return; //there are spurious extra events after the stop
            scrollBarRepeatTimer.Stop();
            FinishPan();
        }

        private void StartPan(Point currentPositionOnGrid)
        {
            lastPositionOnGrid = currentPositionOnGrid;
            lastPositionOnCanvas = new Point(0, 0);
            CanvasOffset = new Vector(0, 0);
        }

        private void ContinuePan(Point currentPositionOnGrid)
        {
            //this shifts the display with a transform (but doesn't repaint to fill in the edges
            Vector v = lastPositionOnGrid - currentPositionOnGrid;
            if (v.X != 0 || v.Y != 0)
            {
                CanvasOffset -= v;
                TranslateTransform tt = new TranslateTransform(CanvasOffset.X, CanvasOffset.Y);
                theCanvas.RenderTransform = tt;
            }
            lastPositionOnGrid = currentPositionOnGrid;
        }


        private void FinishPan()
        {
            theCanvas.RenderTransform = new TranslateTransform(0, 0);
            dp.DisplayOffset += CanvasOffset;
            CanvasOffset = new Vector(0, 0);
            Update();
            theCanvas.Cursor = Cursors.Cross;
        }

        //problem of processing order in neuron areas
        //finish help-about page
        //Refactor the Main.neuron array
        //XML documentation
    }



}
