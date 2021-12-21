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
        //mouse autorepeat timer
        DispatcherTimer mouseRepeatTimer = null;



        //NEURON

        private void OpenNeuronContextMenu(FrameworkElement theShape)
        {
            if (theShape is Label l)
            {
                l.ContextMenu = new ContextMenu();
                Neuron n1 = MainWindow.theNeuronArray.GetNeuron(mouseDownNeuronIndex);
                NeuronView.CreateContextMenu(mouseDownNeuronIndex, n1, l.ContextMenu);
                l.ContextMenu.IsOpen = true;
            }
            else
            {
                theShape.ContextMenu = new ContextMenu();
                Neuron n1 = MainWindow.theNeuronArray.GetNeuron(mouseDownNeuronIndex);
                NeuronView.CreateContextMenu(mouseDownNeuronIndex, n1, theShape.ContextMenu);
                targetNeuronIndex = mouseDownNeuronIndex;
                theShape.ContextMenu.IsOpen = true;
            }
        }

        //mouse down in a neuron...it's firing OR the beginning of a synapse drag
        private Neuron NeuronMouseDown(Neuron n, int clickCount)
        {
            if (clickCount == 2)
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
            currentOperation = CurrentOperation.draggingSynapse;
            targetNeuronIndex = mouseDownNeuronIndex;
            return n;
        }
        private void NeuronMouseUp(MouseButtonEventArgs e)
        {
            if (mouseDownNeuronIndex == -1)
            {
                return;
            }
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



        //this is an autorepeat on a neuron firing
        private void MouseRepeatTimer_Tick(object sender, EventArgs e)
        {
            if (mouseDownNeuronIndex < 0) return;
            Neuron n = MainWindow.theNeuronArray.GetNeuron(mouseDownNeuronIndex);
            n.SetValue(1);
            mouseRepeatTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
        }




        //SYNAPSE
        private static void OpenSynapseContextMenu(FrameworkElement theShape)
        {
            int source = (int)theShape.GetValue(SynapseView.SourceIDProperty);
            int target = (int)theShape.GetValue(SynapseView.TargetIDProperty);
            float weight = (float)theShape.GetValue(SynapseView.WeightValProperty);
            Neuron n1 = MainWindow.theNeuronArray.GetCompleteNeuron(source);
            n1 = MainWindow.theNeuronArray.AddSynapses(n1);
            if (n1.FindSynapse(target) is Synapse s1)
            {
                theShape.ContextMenu = new ContextMenu();
                SynapseView.CreateContextMenu(source, s1, theShape.ContextMenu);
            }
        }

        private void StartSynapseDragging(FrameworkElement theShape)
        {
            int source = (int)theShape.GetValue(SynapseView.SourceIDProperty);
            int target = (int)theShape.GetValue(SynapseView.TargetIDProperty);
            LastSynapseWeight = (float)theShape.GetValue(SynapseView.WeightValProperty);
            LastSynapseModel = (Synapse.modelType)theShape.GetValue(SynapseView.ModelProperty);
            Neuron n = MainWindow.theNeuronArray.GetNeuron(source);
            n.DeleteSynapse(target);
            mouseDownNeuronIndex = source;
            currentOperation = CurrentOperation.draggingSynapse;
            Canvas parentCanvas = (Canvas)theShape.Parent;
            parentCanvas.Children.Remove(theShape);
        }

        private void DragSynapse(int currentNeuron)
        {
            if (mouseDownNeuronIndex > -1 && (currentNeuron != mouseDownNeuronIndex || synapseShape != null))
            {
                if (synapseShape != null)
                    theCanvas.Children.Remove(synapseShape);
                Shape l = SynapseView.GetSynapseShape
                    (dp.pointFromNeuron(mouseDownNeuronIndex),
                    dp.pointFromNeuron(currentNeuron),
                    LastSynapseModel
                    );
                l.Stroke = new SolidColorBrush(Utils.RainbowColorFromValue(LastSynapseWeight));
                if (!(l is Ellipse))
                    l.Fill = l.Stroke;
                theCanvas.Children.Add(l);
                synapseShape = l;
            }
        }
        private void FinishDraggingSynapse(MouseButtonEventArgs e)
        {
            if (mouseDownNeuronIndex > -1)
            {
                Point p1 = e.GetPosition(theCanvas);
                LimitMousePostion(ref p1);
                int index = dp.NeuronFromPoint(p1);
                MainWindow.theNeuronArray.SetUndoPoint();
                MainWindow.arrayView.AddShowSynapses(mouseDownNeuronIndex);
                MainWindow.theNeuronArray.GetNeuron(mouseDownNeuronIndex).
                    AddSynapseWithUndo(index, LastSynapseWeight, LastSynapseModel);
            }
            synapseShape = null;
            mouseDownNeuronIndex = -1;
            e.Handled = true;
            Update();
        }


        //SELECTION
        private void OpenSelectionContextMenu(FrameworkElement theShape)
        {
            int i = (int)theShape.GetValue(ModuleView.AreaNumberProperty);
            Rectangle r = theSelection.selectedRectangles[i].GetRectangle(dp);
            theShape.ContextMenu = new ContextMenu();
            SelectionView.CreateSelectionContextMenu(i, theShape.ContextMenu);
            theShape.ContextMenu.IsOpen = true;
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

        private void StartMovingSelection(FrameworkElement theShape)
        {
            theShape.CaptureMouse();
            currentOperation = CurrentOperation.movingSelection;
        }
        private Point StartNewSelectionDrag()
        {
            //snap to neuron point
            Point currentPosition = dp.pointFromNeuron(mouseDownNeuronIndex);
            currentOperation = CurrentOperation.draggingNewSelection;
            if (dragRectangle != null)
            {
                theCanvas.Children.Remove(dragRectangle);
                dragRectangle = null;
            }

            MainWindow.theNeuronArray.SetUndoPoint();
            MainWindow.theNeuronArray.AddSelectionUndo();
            if (!MainWindow.ctrlPressed)
                theSelection.selectedRectangles.Clear();
            else
                Update();

            //currentPosition = CreateDragRectangle(currentPosition);
            return currentPosition;
        }

        private Point CreateDragRectangle(Point currentPosition)
        {
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
            return currentPosition;
        }

        private void DragNewSelection(int currentNeuron)
        {
            if (dragRectangle == null)
            {
                Point currentPosition = dp.pointFromNeuron(mouseDownNeuronIndex);
                CreateDragRectangle(currentPosition);
            }

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


        private void MoveSelection(int currentNeuron)
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




        //MODULE
        private static void OpenModuleContextMenu(FrameworkElement theShape)
        {
            int i = (int)theShape.GetValue(ModuleView.AreaNumberProperty);
            ModuleView nr = MainWindow.theNeuronArray.Modules[i];
            theShape.ContextMenu = new ContextMenu();
            ModuleView.CreateContextMenu(i, nr, theShape, theShape.ContextMenu);
            theShape.ContextMenu.IsOpen = true;
        }

        public static void StartInsertingModule(string moduleLabel)
        {
            StopInsertingModule();
            labelCursor = new Label
            {
                Content = moduleLabel,
                Background = new SolidColorBrush(Colors.White),
            };

            Type t = Type.GetType("BrainSimulator.Modules.Module" + moduleLabel);
            Modules.ModuleBase theModule = (Modules.ModuleBase)Activator.CreateInstance(t);
            rectCursor = new Rectangle
            {
                Fill = new SolidColorBrush(Colors.Wheat),
                Opacity = 0.3,
                Width = theModule.MinWidth * MainWindow.arrayView.dp.NeuronDisplaySize,
                Height = theModule.MinHeight * MainWindow.arrayView.dp.NeuronDisplaySize,
            };
            MainWindow.arrayView.theCanvas.Cursor = Cursors.None;
            MainWindow.arrayView.currentOperation = CurrentOperation.insertingModule;
        }
        public static void StopInsertingModule()
        {
            MainWindow.arrayView.theCanvas.Children.Remove(labelCursor);
            MainWindow.arrayView.theCanvas.Children.Remove(rectCursor);
            MainWindow.arrayView.currentOperation = CurrentOperation.idle;
            labelCursor = null;
            rectCursor = null;
        }

        private void DragInsertingModule(int currentNeuron)
        {
            if (labelCursor.Parent == null)
                theCanvas.Children.Add(labelCursor);
            if (rectCursor.Parent == null)
                theCanvas.Children.Add(rectCursor);

            string moduleLabel = labelCursor.Content.ToString();
            Type t = Type.GetType("BrainSimulator.Modules.Module" + moduleLabel);
            Modules.ModuleBase theModule = (Modules.ModuleBase)Activator.CreateInstance(t);

            MainWindow.theNeuronArray.GetNeuronLocation(currentNeuron, out int col, out int row);
            col = Math.Min(col, MainWindow.theNeuronArray.Cols - theModule.MinWidth);
            row = Math.Min(row, MainWindow.theNeuronArray.rows - theModule.MinHeight);
            currentNeuron = MainWindow.theNeuronArray.GetNeuronIndex(col, row);
            Point snappedPosition = dp.pointFromNeuron(currentNeuron);

            labelCursor.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetTop(labelCursor, snappedPosition.Y - labelCursor.DesiredSize.Height);
            Canvas.SetLeft(labelCursor, snappedPosition.X);
            Canvas.SetTop(rectCursor, snappedPosition.Y);
            Canvas.SetLeft(rectCursor, snappedPosition.X);
            currentOperation = CurrentOperation.insertingModule;
        }



        public static void InsertModule(int currentNeuron)
        {
            if (labelCursor == null) return;
            //TODO check to see if the underlying area is empty and warn if not
            MainWindow.theNeuronArray.SetUndoPoint();
            MainWindow.theNeuronArray.AddModuleUndo(MainWindow.theNeuronArray.modules.Count, null);
            string moduleName = labelCursor.Content.ToString();
            SelectionView.CreateModule(moduleName, "Module" + moduleName, Colors.LightBlue, currentNeuron, 0, 0);
            StopInsertingModule();
            MainWindow.Update();
            MainWindow.arrayView.currentOperation = CurrentOperation.idle;
        }

        int prevModuleMouseLocation = -1;
        private void StartaSizingModule(FrameworkElement theShape, int currentNeuron)
        {
            currentOperation = CurrentOperation.sizingModule;
            prevModuleMouseLocation = currentNeuron;
        }
        private void StartaMovingModule(FrameworkElement theShape, int currentNeuron)
        {
            currentOperation = CurrentOperation.movingModule;
            prevModuleMouseLocation = currentNeuron;
        }

        private void MoveModule(FrameworkElement theShape, int currentNeuron)
        {
            Debug.WriteLine("currentNeuron: " + currentNeuron + " prevModuleMouseLocation:" + prevModuleMouseLocation);
            lock (MainWindow.theNeuronArray.modules)
            {
                if (currentNeuron != prevModuleMouseLocation)
                {

                    //which module?
                    int index = (int)theShape.GetValue(ModuleView.AreaNumberProperty);
                    ModuleView theCurrentModule = MainWindow.theNeuronArray.modules[index];
                    MainWindow.theNeuronArray.AddModuleUndo(index, theCurrentModule);

                    int delta = currentNeuron - prevModuleMouseLocation;
                    int newFirst = theCurrentModule.FirstNeuron + delta;
                    int newLast = theCurrentModule.LastNeuron + delta;
                    MainWindow.theNeuronArray.GetNeuronLocation(newFirst, out int col0, out int row0);
                    MainWindow.theNeuronArray.GetNeuronLocation(newLast, out int col1, out int row1);


                    if (newFirst >= 0 && row1 > row0 && newLast < MainWindow.theNeuronArray.arraySize)
                    {
                        //move all the neurons
                        List<int> neuronsToMove = new List<int>();
                        foreach (Neuron n in theCurrentModule.Neurons)
                        {
                            neuronsToMove.Add(n.id);
                        }
                        if (!IsDestinationClear(neuronsToMove, delta))
                        {
                            MessageBoxResult result1 = MessageBox.Show("Some destination neurons are in use and will be overwritten, continue?", "Continue", MessageBoxButton.YesNo);
                            if (result1 != MessageBoxResult.Yes)
                            {
                                return;
                            }
                        }
                        if (delta > 0) //move all the nuerons...opposite order depending on the direction of the move
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
                        theCurrentModule.FirstNeuron += delta;
                        Update();
                        prevModuleMouseLocation = currentNeuron;
                    }
                    else
                    {
                        // MessageBox.Show("Module would be outside neuron array boundary.");
                    }
                }
            }
        }
        private void FinishMovingModule(FrameworkElement theShape)
        {
            currentOperation = CurrentOperation.idle;
        }

        private void ResizeModule(FrameworkElement theShape, int currentNeuron)
        {
            lock (MainWindow.theNeuronArray.modules)
            {
                //TODO: Add rearrangement of neurons
                //TODO: Add clone of neurons to handle ALL properties
                int index = (int)theShape.GetValue(ModuleView.AreaNumberProperty);
                ModuleView theCurrentModule = MainWindow.theNeuronArray.modules[index];

                //the current module bounding box
                theCurrentModule.GetBounds(out int X1, out int Y1, out int X2, out int Y2);
                //the previous mouse location
                theCurrentModule.GetAbsNeuronLocation(prevModuleMouseLocation, out int Xf, out int Yf);
                //the current mouse location
                theCurrentModule.GetAbsNeuronLocation(currentNeuron, out int Xc, out int Yc);
                int minHeight = theCurrentModule.TheModule.MinHeight;
                int minWidth = theCurrentModule.TheModule.MinWidth;
                int maxHeight = theCurrentModule.TheModule.MaxHeight;
                int maxWidth = theCurrentModule.TheModule.MaxWidth;

                //move the top?
                if (theCanvas.Cursor == Cursors.ScrollN || theCanvas.Cursor == Cursors.ScrollNE || theCanvas.Cursor == Cursors.ScrollNW)
                {
                    if (Yc != Yf)
                    {
                        int newTop = Y1 + Yc - Yf;
                        if (newTop <= Y2)
                        {
                            MainWindow.theNeuronArray.AddModuleUndo(index, theCurrentModule);
                            int newHeight = theCurrentModule.Height - (Yc - Yf);

                            if (newHeight < minHeight)
                                theCurrentModule.Height = minHeight;
                            else if (newHeight > maxHeight)
                                theCurrentModule.Height = maxHeight;
                            else
                            {
                                //force this move to be vertical only
                                int newLoc = theCurrentModule.GetAbsNeuronIndexAt(Xf, Yc);
                                MoveModule(theShape, newLoc);
                                theCurrentModule.Height = newHeight;
                            }
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
                            int newWidth = theCurrentModule.Width - (Xc - Xf);

                            if (newWidth < minWidth)
                                theCurrentModule.Width = minWidth;
                            else if (newWidth > maxWidth)
                                theCurrentModule.Width = maxWidth;
                            else
                            {
                                //force this move to be vertical only
                                int newLoc = theCurrentModule.GetAbsNeuronIndexAt(Xc, Yf);
                                MoveModule(theShape, newLoc);
                                theCurrentModule.Width = newWidth;
                            }
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
                        if (newRight >= X1 && newRight < MainWindow.theNeuronArray.Cols)
                        {
                            MainWindow.theNeuronArray.AddModuleUndo(index, theCurrentModule);
                            int newWidth = theCurrentModule.Width + Xc - Xf;

                            if (newWidth < minWidth)
                                theCurrentModule.Width = minWidth;
                            else if (newWidth > maxWidth)
                                theCurrentModule.Width = maxWidth;
                            else
                                theCurrentModule.Width = newWidth;
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
                        if (newBottom >= Y1  && newBottom < MainWindow.theNeuronArray.rows)
                        {
                            MainWindow.theNeuronArray.AddModuleUndo(index, theCurrentModule);

                            int newHeight = theCurrentModule.Height + Yc - Yf;

                            if (newHeight < minHeight)
                                theCurrentModule.Height = minHeight;
                            else if (newHeight > maxHeight)
                                theCurrentModule.Height = maxHeight;
                            else
                                theCurrentModule.Height = newHeight;
                            Update();
                        }
                    }
                }
            }
            prevModuleMouseLocation = currentNeuron;
        }


        //PAN
        //handled in NeuronArrayViewZoomPan

    }
}
