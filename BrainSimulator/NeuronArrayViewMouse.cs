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



        //State
        private enum CurrentOperation { idle, draggingSynapse, draggingNewSelection, movingSelection, movingModule, sizingModule, panning };
        private CurrentOperation currentOperation = CurrentOperation.idle;



        //current default synapse params
        public float lastSynapseWeight = 1.0f;
        public Synapse.modelType lastSynapseModel = Synapse.modelType.Fixed;

        int mouseDownNeuronIndex = -1;
        static Shape synapseShape = null;  //the shape of the synapses being rubber-banded 
                                           //(so it can be added/removed easily from the canvas)

        FrameworkElement prevShape = null;

        //This sets the mouse cursor depending on the mouse location and current operation.
        private shapeType SetMouseCursorShape(MouseEventArgs e, out FrameworkElement theShape)
        {
            theShape = prevShape;

            //if dragging, don't change the cursor but return the hit
            if (currentOperation != CurrentOperation.idle)
            {
                if (currentOperation == CurrentOperation.draggingSynapse) return shapeType.Synapse;
                if (currentOperation == CurrentOperation.movingModule) return shapeType.Module;
                if (currentOperation == CurrentOperation.draggingNewSelection) return shapeType.Selection;
                if (currentOperation == CurrentOperation.movingSelection) return shapeType.Selection;
                if (currentOperation == CurrentOperation.sizingModule) return shapeType.Module;
            }

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
                //or its border
                //So you need to dig up the parent in order to get the original Label
                theShape = theShape0;
                if (theShape0 is TextBlock)
                {
                    ContentPresenter x = (ContentPresenter)VisualTreeHelper.GetParent(result.VisualHit);
                    theShape = (Label)x.TemplatedParent;
                }
                if (theShape0 is Border l)
                {
                    theShape = (Label)VisualTreeHelper.GetParent(result.VisualHit);
                }

                //Get the type of the hit...
                shapeType st = (shapeType)theShape.GetValue(ShapeType);

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
                        SetScrollCursor(e.GetPosition(this), theShape);
                        break;
                    case shapeType.Selection: //TODO add directional arrow code
                        theCanvas.Cursor = Cursors.ScrollAll;
                        break;
                }
                prevShape = theShape;
                return st;
            }
            //mouse is not over anything
            theCanvas.Cursor = Cursors.Cross;
            return shapeType.Canvas;
        }

        //set up the cursor if it's near the edge of a module
        private bool SetScrollCursor(Point currentPosition, FrameworkElement theShape)
        {
            int moduleIndex = (int)theShape.GetValue(ModuleView.AreaNumberProperty);
            //this addresses a problem if you delete a module and it's still on the screen for a moment
            if (moduleIndex >= MainWindow.theNeuronArray.Modules.Count) return false;

            Rectangle theRect = MainWindow.theNeuronArray.Modules[moduleIndex].GetRectangle(dp);
            double left = Canvas.GetLeft(theRect);
            double top = Canvas.GetTop(theRect);

            if (currentPosition.X >= left && currentPosition.X <= left + theRect.Width &&
                currentPosition.Y >= top && currentPosition.Y <= top + theRect.Height)
            {
                double edgeToler = dp.NeuronDisplaySize / 2.5;
                if (edgeToler < 8) edgeToler = 8;
                bool nearTop = currentPosition.Y - top < edgeToler;
                bool nearBottom = top + theRect.Height - currentPosition.Y < edgeToler;
                bool nearLeft = currentPosition.X - left < edgeToler;
                bool nearRight = left + theRect.Width - currentPosition.X < edgeToler;

                if (nearTop && nearLeft) theCanvas.Cursor = Cursors.ScrollNW;
                else if (nearTop && nearRight) theCanvas.Cursor = Cursors.ScrollNE;
                else if (nearBottom && nearLeft) theCanvas.Cursor = Cursors.ScrollSW;
                else if (nearBottom && nearRight) theCanvas.Cursor = Cursors.ScrollSE;
                else if (nearTop) theCanvas.Cursor = Cursors.ScrollN;
                else if (nearBottom) theCanvas.Cursor = Cursors.ScrollS;
                else if (nearLeft) theCanvas.Cursor = Cursors.ScrollW;
                else if (nearRight) theCanvas.Cursor = Cursors.ScrollE;
                else theCanvas.Cursor = Cursors.ScrollAll;
            }
            else
                theCanvas.Cursor = Cursors.ScrollAll;
            return true;
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
                else if (theShapeType == shapeType.Neuron)
                {
                    OpenNeuronContextMenu(theShape);
                }
                else if (theShapeType == shapeType.Synapse)
                {
                    OpenSynapseContextMenu(theShape);
                }
                else if (theShapeType == shapeType.Selection)
                {
                    OpenSelectionContextMenu(theShape);
                }
                else if (theShapeType == shapeType.Module)
                {
                    OpenModuleContextMenu(theShape);
                }
                return;
            } //end of right-button pressed

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (theShapeType == shapeType.Neuron)
                {
                    Neuron n = null;
                    if (mouseDownNeuronIndex >= 0 && mouseDownNeuronIndex < MainWindow.theNeuronArray.arraySize)
                        n = MainWindow.theNeuronArray.GetNeuron(mouseDownNeuronIndex) as Neuron;

                    int clickCount = e.ClickCount;
                    n = NeuronMouseDown(n, clickCount);
                }

                if (theShapeType == shapeType.Synapse)
                {
                    StartSynapseDragging(theShape);
                }

                //start a new selection rectangle drag
                if (theShapeType == shapeType.Canvas)
                {
                    currentPosition = StartNewSelectionDrag();
                }

                //either a module or a selection, we're dragging it
                if (theShapeType == shapeType.Module && theCanvas.Cursor == Cursors.ScrollAll)
                {
                    StartaMovingModule(theShape, mouseDownNeuronIndex);
                }
                if (theShapeType == shapeType.Module && theCanvas.Cursor != Cursors.ScrollAll)
                {
                    StartaSizingModule(theShape, mouseDownNeuronIndex);
                }

                if (theShapeType == shapeType.Selection)
                {
                    StartMovingSelection(theShape);
                }
                //starting pan
                if (theCanvas.Cursor == Cursors.Hand)
                {
                    StartPan(e.GetPosition((UIElement)theCanvas.Parent));
                    Mouse.Capture(theCanvas);
                }
            }//end of left-button down
        }

        public void theCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            shapeType theShapeType = SetMouseCursorShape(e, out FrameworkElement theShape);
            if (MainWindow.Busy()) return;

            if (mouseRepeatTimer != null)
            {
                if (mouseRepeatTimer.IsEnabled && mouseRepeatTimer.Interval == new TimeSpan(0, 0, 0, 0, 100)) return;
                mouseRepeatTimer.Stop();
            }

            if (MainWindow.IsArrayEmpty()) return;

            //You can't do any dragging with the right mouse button
            if (e.RightButton == MouseButtonState.Pressed) return;


            Point currentPosition = e.GetPosition(theCanvas);
            Point nonlimitedPosition = currentPosition;
            LimitMousePostion(ref currentPosition);
            int currentNeuron = dp.NeuronFromPoint(currentPosition);

            //update the status bar 
            if (currentPosition == nonlimitedPosition)
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


            if (e.LeftButton == MouseButtonState.Pressed)
            {
                string oString = (theShape == null) ? "null" : theShape.ToString();
                Debug.WriteLine("MouseMove  currentNeuron: " + currentNeuron
                    + " Shape type: " + theShapeType.ToString() + " theShape: " + oString);

                //are we dragging a synapse? rubber-band it
                if ((theCanvas.Cursor == Cursors.Arrow || theCanvas.Cursor == Cursors.UpArrow)
                    && currentOperation == CurrentOperation.draggingSynapse)
                {
                    DragSynapse(currentNeuron);
                }

                //handle the creation/updating of a dragging selection rectangle
                else if ((theShapeType == shapeType.None || theShapeType == shapeType.Selection) &&
                    currentOperation == CurrentOperation.draggingNewSelection)
                {
                    DragNewSelection(currentNeuron);
                }

                //handle moving an existing  selection
                else if (theShapeType == shapeType.Selection &&
                    theCanvas.Cursor == Cursors.ScrollAll)
                {
                    MoveSelection(currentNeuron);
                }

                //handle moving of a module
                else if (theShapeType == shapeType.Module && currentOperation == CurrentOperation.movingModule)
                {
                    MoveModule(theShape, currentNeuron);
                }

                //handle sizing of a module
                else if (theShapeType == shapeType.Module && currentOperation == CurrentOperation.sizingModule)
                {
                    ResizeModule(theShape, currentNeuron);
                }

                else if (theCanvas.Cursor == Cursors.Hand)
                {
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
            }//end of Left-button down
            else //no button pressed
                currentOperation = CurrentOperation.idle;
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

            //we were dragging a new selection, create it
            if (currentOperation == CurrentOperation.draggingNewSelection)
            {
                FinishSelection();
                Update();
            }
            else if (currentOperation == CurrentOperation.draggingSynapse && 
                synapseShape != null)
            {
                FinishDraggingSynapse(e);
            }

            //we clicked on a neuron...was it a fire or a synapse drag?
            if (theCanvas.Cursor == Cursors.UpArrow)
            {
                //if no synapseshape exists, then were just clicking in a nueron
                if (synapseShape == null)
                {
                    NeuronMouseUp(e);
                }
                else //we were dragging a new synapse
                {
                    FinishDraggingSynapse(e);
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
            currentOperation = CurrentOperation.idle;
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
    }
}
