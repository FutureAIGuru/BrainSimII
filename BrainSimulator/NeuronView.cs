//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Diagnostics;


namespace BrainSimulator
{
    public partial class NeuronView : DependencyObject
    {
        public static DisplayParams dp;
        public static Canvas theCanvas;     //reflection of canvas in neuronarrayview
        static NeuronArrayView theNeuronArrayView;

        public static readonly DependencyProperty NeuronIDProperty =
                DependencyProperty.Register("NeuronID", typeof(int), typeof(MenuItem));
        public int NeuronID
        {
            get { return (int)GetValue(NeuronIDProperty); }
            set { SetValue(NeuronIDProperty, value); }
        }
        private static float ellipseSize = 0.7f;

        public static UIElement GetNeuronView(Neuron n, NeuronArrayView theNeuronArrayViewI, out Label l)
        {
            l = null;
            theNeuronArrayView = theNeuronArrayViewI;

            Point p = dp.pointFromNeuron(n.id);

            SolidColorBrush s1 = GetNeuronColor(n);

            Shape r = null;
            if (dp.ShowNeuronCircles())
            {
                r = new Ellipse();
                r.Width = dp.NeuronDisplaySize * ellipseSize;
                r.Height = dp.NeuronDisplaySize * ellipseSize;
            }
            else
            {
                r = new Rectangle();
                r.Width = dp.NeuronDisplaySize;
                r.Height = dp.NeuronDisplaySize;
            }
            r.Fill = s1;
            if (dp.ShowNeuronOutlines())
            {
                r.Stroke = Brushes.Black;
                r.StrokeThickness = 1;
            }

            float offset = (1 - ellipseSize) / 2f;
            Canvas.SetLeft(r, p.X + dp.NeuronDisplaySize * offset);
            Canvas.SetTop(r, p.Y + dp.NeuronDisplaySize * offset);
            if (dp.ShowNeuronArrowCursor())
            {
                r.MouseDown += theNeuronArrayView.theCanvas_MouseDown;
                r.MouseUp += theNeuronArrayView.theCanvas_MouseUp;
                r.MouseWheel += theNeuronArrayView.theCanvas_MouseWheel;

                r.MouseEnter += R_MouseEnter;
                r.MouseLeave += R_MouseLeave;
            }


            if (n.Label != "" || n.model != Neuron.modelType.IF)
            {
                l = new Label();
                //l.Content = n.Label;
                l.FontSize = dp.NeuronDisplaySize * .25;
                l.Foreground = Brushes.White;
                Canvas.SetLeft(l, p.X + dp.NeuronDisplaySize * offset);
                Canvas.SetTop(l, p.Y + dp.NeuronDisplaySize * offset);
                Canvas.SetZIndex(l, 100);

                if (dp.ShowNeuronArrowCursor())
                {
                    l.MouseEnter += R_MouseEnter;
                    l.MouseLeave += R_MouseLeave;
                }
                l.MouseDown += theNeuronArrayView.theCanvas_MouseDown;
                l.MouseUp += theNeuronArrayView.theCanvas_MouseUp;
                l.MouseMove += theNeuronArrayView.theCanvas_MouseMove;
                string theLabel = GetNeuronLabel(n);
                string theToolTip = n.ToolTip;
                if (theToolTip != "")
                {
                    r.ToolTip = new ToolTip { Content = theToolTip };
                    l.ToolTip = new ToolTip { Content = theToolTip };
                }
                l.Content = theLabel;
            }
            return r;
        }

        public static string GetNeuronLabel(Neuron n)
        {
            string retVal = "";
            if (!dp.ShowNeuronLabels()) return retVal;

            retVal = n.Label;
            if (n.model == Neuron.modelType.LIF)
            {
                if (n.leakRate == 0)
                    retVal += "\rD=" + n.axonDelay.ToString();
                else if (n.leakRate < 1)
                    retVal += "\rL=" + n.leakRate.ToString("f2").Substring(1);
                else
                    retVal += "\rL=" + n.leakRate.ToString("f1");
            }
            if (n.model == Neuron.modelType.Burst)
                retVal += "\rB=" + n.axonDelay.ToString();
            if (n.model == Neuron.modelType.Random)
                retVal += "\rR=" + n.axonDelay.ToString();
            if (n.model == Neuron.modelType.Always)
                retVal += "\rA=" + n.axonDelay.ToString();
            return retVal;
        }

        public static SolidColorBrush GetNeuronColor(Neuron n)
        {
            // figure out which color to use
            if (n.model == Neuron.modelType.Color)
            {
                SolidColorBrush brush = new SolidColorBrush(Utils.IntToColor(n.LastChargeInt));
                return brush;
            }
            float value = n.LastCharge;
            Color c = Utils.RainbowColorFromValue(value);
            SolidColorBrush s1 = new SolidColorBrush(c);
            if (!n.inUse && n.Model == Neuron.modelType.IF)
                s1.Opacity = .50;
            if ((n.leakRate < 0) || float.IsNegativeInfinity(1.0f / n.leakRate))
                s1 = new SolidColorBrush(Colors.LightSalmon);
            return s1;
        }

        private static void R_MouseLeave(object sender, MouseEventArgs e)
        {
            if (MainWindow.IsProgressBarVisible()) return;

            if (theCanvas.Cursor == Cursors.Wait) return;

            //Debug.WriteLine("NeuronView MouseLeave");
            if (theCanvas.Cursor != Cursors.Hand && !theNeuronArrayView.dragging && e.LeftButton != MouseButtonState.Pressed)
                theCanvas.Cursor = Cursors.Cross;
            if (sender is FrameworkElement s1)
            {
                if (s1.ToolTip != null)
                {
                    ToolTip x = (ToolTip)s1.ToolTip;
                    x.IsOpen = false;
                }
            }
        }

        private static void R_MouseEnter(object sender, MouseEventArgs e)
        {
            //Debug.WriteLine("NeuronView MouseEnter");
            if (MainWindow.IsProgressBarVisible()) return;

            if (theCanvas.Cursor == Cursors.Wait) return;

            if (theCanvas.Cursor != Cursors.Hand && !theNeuronArrayView.dragging && e.LeftButton != MouseButtonState.Pressed)
                theCanvas.Cursor = Cursors.UpArrow;

            if (sender is FrameworkElement s1)
            {
                if (s1.ToolTip != null)
                {
                    ToolTip x = (ToolTip)s1.ToolTip;
                    if (!x.IsOpen)
                        x.IsOpen = true;
                }
            }
        }
    }
}
