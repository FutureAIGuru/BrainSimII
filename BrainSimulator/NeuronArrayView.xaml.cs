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


namespace BrainSimulator
{


    public partial class NeuronArrayView : UserControl
    {

        private DisplayParams dp = new DisplayParams(); //refactor these back to private

        Point lastPosition = new Point(0, 0); //temp position used for calculating pan positions
        Point lastPosition1 = new Point(0, 0); //temp position used for calculating pan positions

        public NeuronArray myClipBoard = null; //refactor back to private
        public int targetNeuronIndex = -1;

        //needed for handling selections of areas of neurons
        Rectangle dragRectangle = null;
        int firstSelectedNeuron = -1;
        int lastSelectedNeuron = -1;


        //keeps track of the multiple selection areas
        public NeuronSelection theSelection = new NeuronSelection();

        //temporary for panning the canvas
        Vector CanvasOffset = new Vector(0, 0);

        //these are used to handle scaling with the mouse wheel
        DispatcherTimer dt = new DispatcherTimer();
        float scale = 1;

        int Rows { get { return MainWindow.theNeuronArray.rows; } }

        //this helper keeps track of the neurons on the screen so they can change color without repainting
        private List<NeuronOnScreen> neuronsOnScreen = new List<NeuronOnScreen>();
        public class NeuronOnScreen
        {
            public int neuronIndex; public UIElement graphic; public float prevValue;
            public NeuronOnScreen(int index, UIElement e, float value)
            {
                neuronIndex = index; graphic = e; prevValue = value;
            }
        };

        public NeuronArrayView()
        {
            InitializeComponent();
            dt.Tick += Dt_Tick;
        }

        public DisplayParams Dp { get => dp; set => dp = value; }

        //refresh the display of the neuron network
        public void Update()
        {
            if (MainWindow.theNeuronArray == null) return;

            var watch = System.Diagnostics.Stopwatch.StartNew();

            Debug.WriteLine("Update " + MainWindow.theNeuronArray.Generation);
            dp.NeuronRows = MainWindow.theNeuronArray.rows;
            theCanvas.Children.Clear();
            neuronsOnScreen.Clear();

            //draw any module connectors 
            DrawModuleConnectors();

            //draw the module rectangles
            for (int i = 0; i < MainWindow.theNeuronArray.Modules.Count; i++)
            {
                Module nr = MainWindow.theNeuronArray.Modules[i];
                NeuronSelectionRectangle nsr = new NeuronSelectionRectangle(nr.FirstNeuron, nr.Width, nr.Height);
                Rectangle r = nsr.GetRectangle(dp);
                r.Fill = new SolidColorBrush(Utils.FromArgb(nr.Color));
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
                Module nr = new Module
                {
                    Label = "new",
                    Width = theSelection.selectedRectangles[i].Width,
                    Height = theSelection.selectedRectangles[i].Height,
                    Color = Utils.ToArgb(Colors.Aquamarine),
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

            //draw the synapses
            if (dp.ShowSynapses())
                for (int i = 0; i < MainWindow.theNeuronArray.neuronArray.Length; i++)
                {
                    Point p1 = dp.pointFromNeuron(i);
                    Neuron n = MainWindow.theNeuronArray.neuronArray[i];
                    foreach (Synapse s in n.Synapses)
                    {
                        Shape l = SynapseView.GetSynapseView(i, p1, s, this);
                        if (l != null)
                            theCanvas.Children.Add(l);
                    }
                }

            //draw the neurons
            if (dp.ShowNeurons())
            {
                for (int i = 0; i < MainWindow.theNeuronArray.neuronArray.Length; i++)
                {
                    UIElement l = NeuronView.GetNeuronView(i, this);
                    if (l != null)
                    {
                        int element = theCanvas.Children.Add(l);
                        //if (l is Ellipse e && e.Fill.Opacity != 0)
                        if (l is Ellipse || l is Rectangle)
                            neuronsOnScreen.Add(new NeuronOnScreen(i, l, 0));
                        else if (l is Label)
                            neuronsOnScreen.Add(new NeuronOnScreen(i, l, 0));
                    }
                }
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Debug.WriteLine("Update Done " + elapsedMs + "ms");
        }

        private void DrawModuleConnectors()
        {
            for (int i = 0; i < MainWindow.theNeuronArray.Modules.Count; i++)
            {
                Module nr = MainWindow.theNeuronArray.Modules[i];
                Point GetCenter(Module na)
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
                        Module naTarget = MainWindow.theNeuronArray.FindAreaByLabel(commands[j + 1]);
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
                if (a.graphic is Shape e)
                {
                    Neuron n = MainWindow.theNeuronArray.neuronArray[a.neuronIndex];
                    if (n.LastCharge != a.prevValue)
                    {
                        if (NeuronView.GetNeuronView(a.neuronIndex, this) is Shape l)
                        {
                            e.Fill = l.Fill;
                            neuronsOnScreen[i].prevValue = n.LastCharge;
                        }
                    }
                }
            }
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
            Debug.WriteLine("theCanvas_MouseDown" + MainWindow.theNeuronArray.Generation);
            Point currentPosition = e.GetPosition(theCanvas);
            LimitMousePostion(ref currentPosition);
            mouseDownNeuronIndex = dp.NeuronFromPoint(currentPosition);

            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (sender is Label l)
                {
                    l.ContextMenu = new ContextMenu();
                    Neuron n1 = MainWindow.theNeuronArray.neuronArray[mouseDownNeuronIndex];
                    NeuronView.CreateContextMenu(mouseDownNeuronIndex, n1, l.ContextMenu);
                    l.ContextMenu.IsOpen = true;
                    e.Handled = true;
                }
                else if (sender is Shape s)
                {
                    if (s.ContextMenu == null && s is Ellipse) // a neuron
                    {
                        s.ContextMenu = new ContextMenu();
                        Neuron n1 = MainWindow.theNeuronArray.neuronArray[mouseDownNeuronIndex];
                        NeuronView.CreateContextMenu(mouseDownNeuronIndex, n1, s.ContextMenu);
                    }
                    if (s.ContextMenu == null && s is Path) // a synapse
                    {
                        int source = (int)s.GetValue(SynapseView.SourceIDProperty);
                        int target = (int)s.GetValue(SynapseView.TargetIDProperty);
                        float weight = (float)s.GetValue(SynapseView.WeightValProperty);
                        Synapse s1 = new Synapse(target, weight);
                        s.ContextMenu = new ContextMenu();
                        SynapseView.CreateContextMenu(source, s1, s.ContextMenu);
                    }
                    s.ContextMenu.IsOpen = true;
                    e.Handled = true;
                }
                else
                {
                }
                return;
            }

            Neuron n = null;
            if (mouseDownNeuronIndex >= 0 && mouseDownNeuronIndex < MainWindow.theNeuronArray.neuronArray.Length)
                n = MainWindow.theNeuronArray.neuronArray[mouseDownNeuronIndex] as Neuron;

            if (theCanvas.Cursor == Cursors.Cross)
            {
                //          Debug.WriteLine("dragStart" + MainWindow.theNeuronArray.Generation);
                if (dragRectangle != null)
                {
                    theCanvas.Children.Remove(dragRectangle);
                }

                if (!MainWindow.crtlPressed)
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
                dragging = true;
            }
            if (theCanvas.Cursor == Cursors.Hand)
            {
                lastPosition1 = e.GetPosition((UIElement)theCanvas.Parent);
                Mouse.Capture(theCanvas);
            }
        }

        public float lastSynapseWeight = 1.0f;
        public void theCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (MainWindow.theNeuronArray == null) return;
            Debug.WriteLine("theCanvas_MouseUp" + MainWindow.theNeuronArray.Generation);
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
                    Neuron n = MainWindow.theNeuronArray.neuronArray[mouseDownNeuronIndex];
                    if (n != null)
                    {
                        if (n.LastCharge < .99)
                            n.CurrentCharge = 1;
                        else
                            n.CurrentCharge = n.LastCharge = 0;
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
                        MainWindow.theNeuronArray.neuronArray[mouseDownNeuronIndex].AddSynapse(index, lastSynapseWeight, MainWindow.theNeuronArray);
                    }
                    synapseShape = null;
                    mouseDownNeuronIndex = -1;
                    e.Handled = true;
                    Update();
                }
            }

            if (theCanvas.Cursor == Cursors.Hand)
            {
                theCanvas.RenderTransform = new TranslateTransform(0, 0);
                dp.DisplayOffset += CanvasOffset;
                CanvasOffset = new Vector(0, 0);
                e.Handled = true;
                Update();
                theCanvas.Cursor = Cursors.Cross;
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
            float width = dp.NeuronDisplaySize * MainWindow.theNeuronArray.neuronArray.Length / dp.NeuronRows - 1;
            float height = dp.NeuronDisplaySize * dp.NeuronRows - 1;
            if (p1.X > dp.DisplayOffset.X + width) p1.X = dp.DisplayOffset.X + width;
            if (p1.Y > dp.DisplayOffset.Y + height) p1.Y = dp.DisplayOffset.Y + height;

        }

        Module na = null;
        public void theCanvas_MouseMove(object sender, MouseEventArgs e)
        {
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
                        na.FirstNeuron += currentNeuron - firstSelectedNeuron;
                    }
                    firstSelectedNeuron = currentNeuron;
                    SortAreas();
                    Update();
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
                dragging = true;
                na.GetBounds(out int X1, out int Y1, out int X2, out int Y2);
                na.GetAbsNeuronLocation(firstSelectedNeuron, out int Xf, out int Yf);
                na.GetAbsNeuronLocation(currentNeuron, out int Xc, out int Yc);
                na.GetAbsNeuronLocation(na.LastNeuron, out int Xl, out int Yl);

                //move the top?
                if (theCanvas.Cursor == Cursors.ScrollN || theCanvas.Cursor == Cursors.ScrollNE || theCanvas.Cursor == Cursors.ScrollNW)
                {
                    if (Yc != Yf)
                    {
                        int newTop = Y1 + Yc - Yf;
                        if (newTop <= Y2)
                        {
                            na.FirstNeuron += Yc - Yf;
                            na.Height -= Yc - Yf;
                            firstSelectedNeuron = currentNeuron;
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
                            na.FirstNeuron += (Xc - Xf) * MainWindow.theNeuronArray.rows;
                            na.Width -= Xc - Xf;
                            firstSelectedNeuron = currentNeuron;
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
                            firstSelectedNeuron = currentNeuron;
                            Update();
                        }
                    }
                }
            }

            if (theCanvas.Cursor == Cursors.Hand)
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    if (lastPosition != new Point(0, 0))
                    {
                        //this is the calculation used for updates
                        dp.DisplayOffset += currentPosition - lastPosition;

                        //this shifts the display with a transform (but doesn't repaint to fill in the edges
                        Point CurrentPosition1 = e.GetPosition((UIElement)theCanvas.Parent);
                        Vector v = lastPosition1 - CurrentPosition1;
                        if (v.X != 0 || v.Y != 0)
                        {
                            CanvasOffset -= v;
                            TranslateTransform tt = new TranslateTransform(CanvasOffset.X, CanvasOffset.Y);
                            theCanvas.RenderTransform = tt;
                        }
                    }
                    lastPosition = currentPosition;
                    lastPosition1 = e.GetPosition((UIElement)theCanvas.Parent);
                }
                else
                {
                    lastPosition = new Point(0, 0);
                }
        }

        public static void SortAreas()
        {
            MainWindow.SuspendEngine();
            MainWindow.theNeuronArray.Modules.Sort();
            MainWindow.ResumeEngine();
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

        public void theCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //zoom in-out the display
            float oldNeuronDisplaySize = dp.NeuronDisplaySize;
            dp.NeuronDisplaySize += e.Delta / 60;
            if (dp.NeuronDisplaySize < 1) dp.NeuronDisplaySize = 1;
            Point mousePostion = e.GetPosition(theCanvas);
            Vector v = (Vector)mousePostion;
            v -= (Vector)dp.DisplayOffset;
            v *= dp.NeuronDisplaySize / oldNeuronDisplaySize;
            dp.DisplayOffset = mousePostion - v;

            scale *= dp.NeuronDisplaySize / oldNeuronDisplaySize;
            ScaleTransform st = new ScaleTransform(scale, scale, mousePostion.X, mousePostion.Y);
            theCanvas.RenderTransform = st;

            //start a timer to do an update so we don't get an update for every wheel click
            dt.Stop();
            dt.Interval = TimeSpan.FromMilliseconds(250);
            dt.Start();
            MainWindow.UpdateDisplayLabel(dp.NeuronDisplaySize, 0);
        }

        private void Dt_Tick(object sender, EventArgs e)
        {
            dt.Stop();
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

        //problem of processing order in neuron areas
        //finish help-about page
        //right-click area selection broken when clicked in neuron
        //ctl key shortcuts...cut paste, copy delete, undo
        //standard weights on synapse context menu
        //UI Cleanup
        //optimize engine for bigger array (GPU) ?
        //Refactor the Main.neuron array

        //XML documentation

    }



}
