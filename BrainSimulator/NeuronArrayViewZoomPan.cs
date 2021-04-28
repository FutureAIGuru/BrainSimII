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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace BrainSimulator
{
    public partial class NeuronArrayView
    {
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

        public double ActualHeight()
        {
            return theCanvas.ActualHeight;
        }
        public double ActualWidth()
        {
            return theCanvas.ActualWidth;
        }


        //SCROLLBAR FUNCTIONS
        //set up the scrollbars to match the coordinates of the display
        bool scrolling = false;
        private void UpdateScrollbars()
        {
            //without this the scrollbar thumbbars flicker
            if (scrolling) return; //to get rid of this, we'll need to take the display translation transformation into account

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
            if (e.ScrollEventType == System.Windows.Controls.Primitives.ScrollEventType.EndScroll)
            {
                scrollBarRepeatTimer.Stop();
                FinishPan();
                return;
            }
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
            scrollBarRepeatTimer.Start();
        }

        private void ScrollBarV_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            if (e.ScrollEventType == System.Windows.Controls.Primitives.ScrollEventType.EndScroll)
            {
                scrollBarRepeatTimer.Stop();
                FinishPan();
                return;
            }
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
            scrollBarRepeatTimer.Start();
        }

        private void ScrollBarRepeatTimer_Tick(object sender, EventArgs e)
        {
            if (!scrollBarRepeatTimer.IsEnabled) return; //there are spurious extra events after the stop
            scrollBarRepeatTimer.Stop();
            FinishPan();
        }


        //PAN

        public void PanToNeuron(int nID)
        {
            Point p1 = dp.pointFromNeuron(nID);
            p1.X -= 30;
            p1.Y -= 30;
            dp.DisplayOffset -= (Vector)p1;
            Update();
        }

        private void StartPan(Point currentPositionOnGrid)
        {
            scrolling = true;
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
            theCanvas.RenderTransform = Transform.Identity;// new TranslateTransform(0, 0);
            dp.DisplayOffset += CanvasOffset;
            CanvasOffset = new Vector(0, 0);
            Update();
            scrolling = false;
        }

        //ZOOM
        float GetNextZoomLevel(float change,float currentValue) //change is the number of steps
        {
            var currentN = Math.Log10(currentValue);
            currentN += change / 20.0;
            currentValue = (float)Math.Pow(10, currentN);
            double size = Math.Min(ActualHeight() / (double)MainWindow.theNeuronArray.rows, ActualWidth() / (double)MainWindow.theNeuronArray.Cols);
            if (currentValue < size/2) 
                currentValue = (float)size/2;
            return currentValue;
        }
        public void Zoom(int change)
        {
            dp.DisplayOffset = (Point)(((Vector)dp.DisplayOffset) * (dp.NeuronDisplaySize + change) / dp.NeuronDisplaySize);
            dp.NeuronDisplaySize = GetNextZoomLevel((float)change,dp.NeuronDisplaySize);

            Update();
        }

        public void theCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //if (!MainWindow.shiftPressed) return;
            if (sender != theCanvas) return;
            //zoom in-out the display
            float oldNeuronDisplaySize = dp.NeuronDisplaySize;
            dp.NeuronDisplaySize = GetNextZoomLevel(e.Delta / 120f,dp.NeuronDisplaySize);
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

            MainWindow.UpdateDisplayLabel(dp.NeuronDisplaySize);
            Debug.WriteLine("Wheel-Zoom: " + scale + " " + dp.NeuronDisplaySize + " " + oldNeuronDisplaySize);
        }

        //get here if the repeat timer expires so do an Update
        private void Dt_Tick(object sender, EventArgs e)
        {
            zoomRepeatTimer.Stop();
            scale = 1;
            theCanvas.RenderTransform = Transform.Identity;// new ScaleTransform(1, 1);
            Update();
        }

        //sets neuron 0 to the upper left of the neuron array display
        public void Origin()
        {
            dp.DisplayOffset = new Point(0, 0);
            dp.NeuronDisplaySize = 25;
        }

    }
}
