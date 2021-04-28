//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BrainSimulator
{
    public partial class NeuronArrayView
    {
        public enum shapeType { None, Canvas, Neuron, Synapse, Selection, Module };
        public static readonly DependencyProperty ShapeType =
                DependencyProperty.Register("ShapeType", typeof(shapeType), typeof(Shape));

        //mouse autorepeat timer
        DispatcherTimer mouseRepeatTimer = null;

        //current default synapse params
        public float lastSynapseWeight = 1.0f;
        public Synapse.modelType lastSynapseModel = Synapse.modelType.Fixed;

        ModuleView theCurrentModule = null;
        public bool dragging = false;
        int mouseDownNeuronIndex = -1;
        static Shape synapseShape = null;  //the shape of the synapses being rubber-banded 
                                           //(so it can be added/removed easily from the canvas)

        //This sets the mouse cursor depending on the mouse location and current operation.
        private shapeType SetMouseCursorShape(MouseEventArgs e, out FrameworkElement theShape)
        {
            theShape = null;
            //don't change the cursor if dragging, panning
            if (theCanvas.Cursor == Cursors.Hand) return shapeType.None;
            //if busy, make sure the cursor is hourglass
            if (MainWindow.Busy())
            {
                theCanvas.Cursor = Cursors.Wait;
                return shapeType.None;
            }

            //what type of shape is the mouse  over?
            HitTestResult result = VisualTreeHelper.HitTest(theCanvas, e.GetPosition(theCanvas));

            if (result.VisualHit is FrameworkElement theShape0)
            {
                //When you put a Label into a Canvas, you get a hit back on the internal TextBlock
                //So you need to dig up the parent in order to get the original Label
                theShape = theShape0;
                if (theShape0 is TextBlock)
                {
                    ContentPresenter x = (ContentPresenter)VisualTreeHelper.GetParent(result.VisualHit);
                    theShape = (Label)x.TemplatedParent;
                }

                //Get the type of the hit...
                shapeType st = (shapeType)theShape.GetValue(ShapeType);

                //if dragging, don't change the cursor but return the hit
                if (dragging) return st;
                if (dragRectangle != null) return st;

                //Set the cursor shape based on the type of object the mouse is over
                switch (st)
                {
                    case shapeType.None:
                        theCanvas.Cursor = Cursors.Cross;
                        st = shapeType.Canvas;
                        break;
                    case shapeType.Synapse:
                        theCanvas.Cursor = Cursors.Arrow;
                        break;
                    case shapeType.Neuron:
                        theCanvas.Cursor = Cursors.UpArrow;
                        break;
                    case shapeType.Module: //for a module, set directional stretch arrow cursors
                        theCanvas.Cursor = Cursors.ScrollAll;
                        double left = Canvas.GetLeft(theShape);
                        double top = Canvas.GetTop(theShape);
                        SetScrollCursor(e.GetPosition(this), (Rectangle)theShape, left, top);
                        break;
                    case shapeType.Selection: //TODO add directional arrow code
                        theCanvas.Cursor = Cursors.ScrollAll;
                        break;
                }
                return st;
            }
            //mouse is not over anything
            theCanvas.Cursor = Cursors.Cross;
            return shapeType.Canvas;
        }


        public void theCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //set the cursor before checking for busy so the wait cursor can be set
            shapeType theShapeType = SetMouseCursorShape(e, out FrameworkElement theShape);
            if (MainWindow.Busy()) return;

            if (MainWindow.theNeuronArray == null) return;
            MainWindow.theNeuronArray.SetUndoPoint();
            Debug.WriteLine("theCanvas_MouseDown" + MainWindow.theNeuronArray.Generation);
            Point currentPosition = e.GetPosition(theCanvas);
            LimitMousePostion(ref currentPosition);
            mouseDownNeuronIndex = dp.NeuronFromPoint(currentPosition);

            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (sender is Image img) //TODO: fix this, it should be a selection hit
                {
                    int i = (int)img.GetValue(ModuleView.AreaNumberProperty);
                    int i1 = -i - 1;
                    ModuleView nr = new ModuleView
                    {
                        Label = "new",
                        Width = theSelection.selectedRectangles[i1].Width,
                        Height = theSelection.selectedRectangles[i1].Height,
                        Color = Utils.ColorToInt(Colors.Aquamarine),
                        CommandLine = ""
                    };
                    img.ContextMenu = new ContextMenu();
                    ModuleView.CreateContextMenu(i, nr, img, img.ContextMenu);
                    img.ContextMenu.IsOpen = true;
                }
                else if (theShapeType == shapeType.Module)
                {
                    int i = (int)theShape.GetValue(ModuleView.AreaNumberProperty);
                    ModuleView nr = MainWindow.theNeuronArray.Modules[i];
                    theShape.ContextMenu = new ContextMenu();
                    ModuleView.CreateContextMenu(i, nr, theShape, theShape.ContextMenu);
                    theShape.ContextMenu.IsOpen = true;
                }
                else if (theShapeType == shapeType.Selection)
                {
                    int i = (int)theShape.GetValue(ModuleView.AreaNumberProperty);
                    Rectangle r = theSelection.selectedRectangles[i].GetRectangle(dp);
                    theShape.ContextMenu = new ContextMenu();
                    SelectionView.CreateSelectionContextMenu(i, theShape.ContextMenu);
                    theShape.ContextMenu.IsOpen = true;
                }
                else if (theShapeType == shapeType.Synapse)
                {
                    int source = (int)theShape.GetValue(SynapseView.SourceIDProperty);
                    int target = (int)theShape.GetValue(SynapseView.TargetIDProperty);
                    float weight = (float)theShape.GetValue(SynapseView.WeightValProperty);
                    Neuron n1 = MainWindow.theNeuronArray.GetCompleteNeuron(source);
                    n1 = MainWindow.theNeuronArray.AddSynapses(n1);
                    Synapse s1 = n1.FindSynapse(target);
                    theShape.ContextMenu = new ContextMenu();
                    SynapseView.CreateContextMenu(source, s1, theShape.ContextMenu);
                }
                else if (theShapeType == shapeType.Neuron && theShape is Label l)
                {
                    l.ContextMenu = new ContextMenu();
                    Neuron n1 = MainWindow.theNeuronArray.GetNeuron(mouseDownNeuronIndex);
                    NeuronView.CreateContextMenu(mouseDownNeuronIndex, n1, l.ContextMenu);
                    l.ContextMenu.IsOpen = true;
                }
                else if (theShapeType == shapeType.Neuron)
                {
                    theShape.ContextMenu = new ContextMenu();
                    Neuron n1 = MainWindow.theNeuronArray.GetNeuron(mouseDownNeuronIndex);
                    NeuronView.CreateContextMenu(mouseDownNeuronIndex, n1, theShape.ContextMenu);
                    targetNeuronIndex = mouseDownNeuronIndex;
                    theShape.ContextMenu.IsOpen = true;
                }
                else
                {
                }
                e.Handled = true;
                return;
            } //end of right-button pressed

            Neuron n = null;
            if (mouseDownNeuronIndex >= 0 && mouseDownNeuronIndex < MainWindow.theNeuronArray.arraySize)
                n = MainWindow.theNeuronArray.GetNeuron(mouseDownNeuronIndex) as Neuron;

            //start a new selection rectangle drag
            if (theShapeType == shapeType.Canvas)
            {
                //          Debug.WriteLine("dragStart" + MainWindow.theNeuronArray.Generation);
                if (dragRectangle != null)
                {
                    theCanvas.Children.Remove(dragRectangle);
                }
                MainWindow.theNeuronArray.SetUndoPoint();
                MainWindow.theNeuronArray.AddSelectionUndo();
                if (!MainWindow.ctrlPressed)
                    theSelection.selectedRectangles.Clear();
                else
                    Update();

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
                lastSelectedNeuron = mouseDownNeuronIndex;
                Mouse.Capture(theCanvas);
            }

            //either a module or a selection, we're dragging it
            if (theShapeType == shapeType.Module)
            {
                int i = (int)theShape.GetValue(ModuleView.AreaNumberProperty);
                theCurrentModule = MainWindow.theNeuronArray.modules[i];
                firstSelectedNeuron = mouseDownNeuronIndex;
                dragging = true;
            }
            if (theShapeType == shapeType.Selection)
            {
                Mouse.Capture(theShape);
                dragging = true;
            }

            //mouse down in a neuron...it's firing OR the beginning of a synapse drag
            if (theShapeType == shapeType.Neuron)
            {
                if (e.ClickCount == 2 && sender is Canvas)
                {
                    //double-click detected
                    n = MainWindow.theNeuronArray.GetNeuron(mouseDownNeuronIndex);
                    n.leakRate = -n.leakRate;
                    n.Update();
                }

                Mouse.Capture(theCanvas);
                if (mouseRepeatTimer == null)
                    mouseRepeatTimer = new DispatcherTimer();
                if (mouseRepeatTimer.IsEnabled)
                    mouseRepeatTimer.Stop();
                mouseRepeatTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);
                mouseRepeatTimer.Tick += MouseRepeatTimer_Tick;
                mouseRepeatTimer.Start();
                dragging = true;
                targetNeuronIndex = mouseDownNeuronIndex;
            }

            //starting pan
            if (theCanvas.Cursor == Cursors.Hand)
            {
                StartPan(e.GetPosition((UIElement)theCanvas.Parent));
                Mouse.Capture(theCanvas);
            }
        }

        //this is an autorepeat on a neuron firing
        private void MouseRepeatTimer_Tick(object sender, EventArgs e)
        {
            if (mouseDownNeuronIndex < 0) return;
            Neuron n = MainWindow.theNeuronArray.GetNeuron(mouseDownNeuronIndex);
            n.SetValue(1);
            mouseRepeatTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
        }

        public void theCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (MainWindow.Busy()) return;
            //SetMouseCursorShape(e);

            if (mouseRepeatTimer != null) mouseRepeatTimer.Stop();
            if (MainWindow.IsArrayEmpty()) return;
            //Debug.WriteLine("theCanvas_MouseUp" + MainWindow.theNeuronArray.Generation);
            if (e.ChangedButton == MouseButton.Right) return;

            Point currentPosition = e.GetPosition(theCanvas);
            LimitMousePostion(ref currentPosition);
            dragging = false;

            //we were dragging a new selection, create it
            if (theCanvas.Cursor == Cursors.Cross)
            {
                FinishSelection();
                Update();
            }

            //we clicked on a neuron...was it a fire or a synapse drag?
            if (theCanvas.Cursor == Cursors.UpArrow)
            {
                if (synapseShape == null && mouseDownNeuronIndex > -1) 
                    //if no synapseshape exists, then were just clicking in a nueron
                {
                    Neuron n = MainWindow.theNeuronArray.GetNeuron(mouseDownNeuronIndex);
                    if (n != null)
                    {
                        if (n.Model == Neuron.modelType.Random || n.model == Neuron.modelType.Always)
                        {
                            if (n.LeakRate < 0)
                                n.LeakRate = -n.LeakRate;
                            else
                            {
                                n.CurrentCharge = 0;
                                n.LastCharge = 0;
                                n.LeakRate = -n.LeakRate;
                            }
                        }
                        else if (n.Model != Neuron.modelType.Color)
                        {
                            if (n.LastCharge < .99)
                            {
                                n.CurrentCharge = 1;
                                n.LastCharge = 1;
                            }
                            else
                            {
                                n.CurrentCharge = 0;
                                n.LastCharge = 0;
                            }
                        }
                        else
                        {
                            if (n.LastChargeInt == 0)
                            {
                                n.LastChargeInt = 0xffffff;
                            }
                            else
                            {
                                n.LastChargeInt = 0;
                            }
                        }
                        n.Update();
                        e.Handled = true;
                        Update();
                    }
                }
                else //we were dragging a new synapse
                {
                    if (mouseDownNeuronIndex > -1)
                    {
                        Point p1 = e.GetPosition(theCanvas);
                        LimitMousePostion(ref p1);
                        int index = dp.NeuronFromPoint(p1);
                        MainWindow.theNeuronArray.SetUndoPoint();
                        MainWindow.arrayView.AddShowSynapses(mouseDownNeuronIndex);
                        MainWindow.theNeuronArray.GetNeuron(mouseDownNeuronIndex).AddSynapseWithUndo(index, lastSynapseWeight, lastSynapseModel);
                    }
                    synapseShape = null;
                    mouseDownNeuronIndex = -1;
                    e.Handled = true;
                    Update();
                }
            }

            //finish a pan
            if (theCanvas.Cursor == Cursors.Hand)
            {
                FinishPan();
                e.Handled = true;
            }

            mouseDownNeuronIndex = -1;
            Mouse.Capture(null);
        }

        private void FinishSelection()
        {
            if (dragRectangle != null)
            {
                try
                {
                    //get the neuron pointers from the drag rectangle and save in the selection array
                    int w = 1 + (lastSelectedNeuron - firstSelectedNeuron) / Rows;
                    int h = 1 + (lastSelectedNeuron - firstSelectedNeuron) % Rows;
                    //Debug.Write(firstSelectedNeuron + ", " + lastSelectedNeuron);
                    SelectionRectangle rr = new SelectionRectangle(firstSelectedNeuron, w, h);
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
            float width = dp.NeuronDisplaySize * MainWindow.theNeuronArray.arraySize / dp.NeuronRows - 1;
            float height = dp.NeuronDisplaySize * dp.NeuronRows - 1;
            if (p1.X > dp.DisplayOffset.X + width) p1.X = dp.DisplayOffset.X + width;
            if (p1.Y > dp.DisplayOffset.Y + height) p1.Y = dp.DisplayOffset.Y + height;

        }

        //whatever the first & last selected neurons, this sorts out the upper-left and lower right
        //that way, you can swipe a selection in any direction and it works
        private void SetFirstLastSelectedNeurons(int newPosition)
        {
            int y1 = mouseDownNeuronIndex % Rows;
            int x1 = mouseDownNeuronIndex / Rows;
            int y2 = newPosition % Rows;
            int x2 = newPosition / Rows;
            firstSelectedNeuron = Math.Min(x1, x2) * Rows + Math.Min(y1, y2);
            lastSelectedNeuron = Math.Max(x1, x2) * Rows + Math.Max(y1, y2);
        }
        public void theCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            shapeType theShapeType = SetMouseCursorShape(e, out FrameworkElement theShape);
            if (MainWindow.Busy()) return;

            Point pt = e.GetPosition((UIElement)sender);

            if (mouseRepeatTimer != null)
            {
                if (mouseRepeatTimer.IsEnabled && mouseRepeatTimer.Interval == new TimeSpan(0, 0, 0, 0, 100)) return;
                mouseRepeatTimer.Stop();
            }

            if (MainWindow.IsArrayEmpty())
                return;

            if (e.RightButton == MouseButtonState.Pressed) return;

            Point currentPosition = e.GetPosition(theCanvas);
            Point savePositiion = currentPosition;
            LimitMousePostion(ref currentPosition);
            int currentNeuron = dp.NeuronFromPoint(currentPosition);

            if (currentPosition == savePositiion)
            {
                int y1 = currentNeuron % Rows;
                int x1 = currentNeuron / Rows;
                string mouseStatus = "ID: " + currentNeuron + "  Row: " + y1 + " Col: " + x1;
                MainWindow.thisWindow.SetStatus(1, mouseStatus, 0);
            }
            else
            {
                MainWindow.thisWindow.SetStatus(1, "Mouse outside neuron array", 1);
            }

            //are we dragging a synapse? rubber-band it
            if (e.LeftButton == MouseButtonState.Pressed &&
                (theCanvas.Cursor == Cursors.Arrow || theCanvas.Cursor == Cursors.UpArrow)
                && dragging)
            {
                if (mouseDownNeuronIndex > -1)
                {
                    if (synapseShape != null || (mouseDownNeuronIndex != currentNeuron))
                    {
                        if (synapseShape != null)
                            theCanvas.Children.Remove(synapseShape);
                        Shape l = SynapseView.GetSynapseShape
                            (dp.pointFromNeuron(mouseDownNeuronIndex),
                            dp.pointFromNeuron(currentNeuron),
                            this,
                            lastSynapseModel
                            );
                        l.Stroke = l.Fill = new SolidColorBrush(Utils.RainbowColorFromValue(lastSynapseWeight));
                        theCanvas.Children.Add(l);
                        synapseShape = l;
                    }
                }
            }
            else if (e.LeftButton != MouseButtonState.Pressed) //we may have missed a mouse-up event...clear out the rubber-banding
            {
                synapseShape = null;
                mouseDownNeuronIndex = -1;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                //handle the creation/updating of a dragging selection rectangle
                if ((theShapeType == shapeType.None || theShapeType == shapeType.Selection) && 
                    dragRectangle != null)
                {
                    //Get the first & last selected neurons
                    SetFirstLastSelectedNeurons(currentNeuron);

                    //update graphic rectangle 
                    Point p1 = dp.pointFromNeuron(firstSelectedNeuron);
                    Point p2 = dp.pointFromNeuron(lastSelectedNeuron);
                    dragRectangle.Width = p2.X - p1.X + dp.NeuronDisplaySize;
                    dragRectangle.Height = p2.Y - p1.Y + dp.NeuronDisplaySize;
                    Canvas.SetLeft(dragRectangle, p1.X);
                    Canvas.SetTop(dragRectangle, p1.Y);
                    if (!theCanvas.Children.Contains(dragRectangle))
                        theCanvas.Children.Add(dragRectangle);
                }

                //handle moving an existing  selection
                if (theShapeType == shapeType.Selection && 
                    theCanvas.Cursor == Cursors.ScrollAll)
                {
                    if (currentNeuron != mouseDownNeuronIndex)
                    {
                        if (theSelection.selectedRectangles.Count > 0)
                        {
                            MainWindow.theNeuronArray.AddSelectionUndo();
                            int offset = currentNeuron - mouseDownNeuronIndex;
                            targetNeuronIndex = theSelection.selectedRectangles[0].FirstSelectedNeuron + offset;
                            MoveNeurons(true);
                        }
                    }
                    mouseDownNeuronIndex = currentNeuron;
                }

                //handle moving of a module
                if (theShapeType == shapeType.Module &&
                    theCanvas.Cursor == Cursors.ScrollAll)
                {
                    if (currentNeuron != firstSelectedNeuron)
                    {
                        int index = MainWindow.theNeuronArray.modules.IndexOf(theCurrentModule);
                        MainWindow.theNeuronArray.AddModuleUndo(index, theCurrentModule);
                        int newFirst = theCurrentModule.FirstNeuron + currentNeuron - firstSelectedNeuron;
                        int newLast = theCurrentModule.LastNeuron + currentNeuron - firstSelectedNeuron;
                        theCurrentModule.GetAbsNeuronLocation(newFirst, out int xf, out int yf);
                        theCurrentModule.GetAbsNeuronLocation(newLast, out int xl, out int yl);

                        if (newFirst >= 0 && newLast < MainWindow.theNeuronArray.arraySize &&
                                xf <= xl && yf <= yl)
                        {
                            //move all the neurons
                            int delta = currentNeuron - firstSelectedNeuron;
                            List<int> neuronsToMove = new List<int>();
                            foreach (Neuron n in theCurrentModule.Neurons1) neuronsToMove.Add(n.id);
                            if (!IsDestinationClear(neuronsToMove, delta))
                            {
                                MessageBoxResult result1 = MessageBox.Show("Some destination neurons are in use and will be overwritten, continue?", "Continue", MessageBoxButton.YesNo);
                                if (result1 != MessageBoxResult.Yes)
                                {
                                    return;
                                }
                            }
                            if (delta > 0) //move all the nerons...opposite order depending on the direction of the move
                            {
                                for (int i = theCurrentModule.NeuronCount - 1; i >= 0; i--)
                                {
                                    Neuron src = theCurrentModule.GetNeuronAt(i);
                                    Neuron dest = MainWindow.theNeuronArray.GetNeuron(src.Id + delta);
                                    MainWindow.thisWindow.theNeuronArrayView.MoveOneNeuron(src, dest);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < theCurrentModule.NeuronCount; i++)
                                {
                                    Neuron src = theCurrentModule.GetNeuronAt(i);
                                    Neuron dest = MainWindow.theNeuronArray.GetNeuron(src.Id + delta);
                                    MainWindow.thisWindow.theNeuronArrayView.MoveOneNeuron(src, dest);
                                }
                            }
                            //move the box
                            theCurrentModule.FirstNeuron += currentNeuron - firstSelectedNeuron;
                            SortAreas();
                            Update();
                            firstSelectedNeuron = currentNeuron;
                        }
                        else
                        {
                            MessageBox.Show("Module would be outside neuron array boundary.");
                        }
                    }
                }
                //handle sizing of a module
                if (theShapeType == shapeType.Module &&  theCurrentModule != null &&
                    theCanvas.Cursor != Cursors.ScrollAll)
                {
                    //TODO: Add rearrangement of neurons
                    //TODO: Add clone of neurons to handle ALL properties
                    dragging = true;
                    theCurrentModule.GetBounds(out int X1, out int Y1, out int X2, out int Y2);
                    theCurrentModule.GetAbsNeuronLocation(firstSelectedNeuron, out int Xf, out int Yf);
                    theCurrentModule.GetAbsNeuronLocation(currentNeuron, out int Xc, out int Yc);
                    theCurrentModule.GetAbsNeuronLocation(theCurrentModule.LastNeuron, out int Xl, out int Yl);
                    int minHeight = theCurrentModule.TheModule.MinHeight;
                    int minWidth = theCurrentModule.TheModule.MinWidth;
                    int index = MainWindow.theNeuronArray.modules.IndexOf(theCurrentModule);

                    //move the top?
                    if (theCanvas.Cursor == Cursors.ScrollN || theCanvas.Cursor == Cursors.ScrollNE || theCanvas.Cursor == Cursors.ScrollNW)
                    {

                        if (Yc != Yf)
                        {
                            int newTop = Y1 + Yc - Yf;
                            if (newTop <= Y2)
                            {
                                MainWindow.theNeuronArray.AddModuleUndo(index, theCurrentModule);
                                theCurrentModule.Height -= Yc - Yf;
                                if (theCurrentModule.Height < minHeight)
                                    theCurrentModule.Height = minHeight;
                                else
                                {
                                    theCurrentModule.FirstNeuron += Yc - Yf;
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
                                MainWindow.theNeuronArray.AddModuleUndo(index, theCurrentModule);
                                theCurrentModule.Width -= Xc - Xf;
                                if (theCurrentModule.Width < minWidth)
                                    theCurrentModule.Width = minWidth;
                                else
                                {
                                    theCurrentModule.FirstNeuron += (Xc - Xf) * MainWindow.theNeuronArray.rows;
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
                                MainWindow.theNeuronArray.AddModuleUndo(index, theCurrentModule);
                                theCurrentModule.Width += Xc - Xf;
                                if (theCurrentModule.Width < minWidth)
                                    theCurrentModule.Width = minWidth;
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
                                MainWindow.theNeuronArray.AddModuleUndo(index, theCurrentModule);
                                theCurrentModule.Height += Yc - Yf;
                                if (theCurrentModule.Height < minHeight)
                                    theCurrentModule.Height = minHeight;
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
        }

        private bool SetScrollCursor(Point currentPosition, Rectangle r, double left, double top)
        {
            if (currentPosition.X > left && currentPosition.X < left + r.Width &&
                currentPosition.Y > top && currentPosition.Y < top + r.Height)
            {
                double edgeToler = dp.NeuronDisplaySize / 2.5;
                if (edgeToler < 8) edgeToler = 8;
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
    }
}
