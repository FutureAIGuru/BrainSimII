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
using System.Windows.Media.Effects;


namespace BrainSimulator
{
    public partial class NeuronView
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

        /*****************************************************
         * Testing of filling disk neuron
         * ***********************************************/
        public class FillableDisc : Canvas
        {
            private Ellipse borderCircle;
            private Rectangle fillRect;
            private Grid fillContainer;
            private double size;

            public FillableDisc(double diameter, int neuronID)
            {
                size = diameter;
                Width = Height = diameter;
                InitializeDisc(neuronID);
            }

            private void InitializeDisc(int neuronID)
            {
                // Border circle
                borderCircle = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Stroke = Brushes.White,
                    StrokeThickness = 1,
                    Fill = Brushes.Gray, 
                };
                borderCircle.SetValue(NeuronIDProperty, neuronID);
                borderCircle.SetValue(NeuronArrayView.ShapeType, NeuronArrayView.shapeType.Neuron);
                Children.Add(borderCircle);

                // Fill container with clipping
                fillRect = new Rectangle
                {
                    Width = size,
                    Fill = Brushes.DodgerBlue,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Height = 0 // Start empty
                };
                fillRect.SetValue(NeuronIDProperty, neuronID);
                fillRect.SetValue(NeuronArrayView.ShapeType, NeuronArrayView.shapeType.Neuron);

                fillContainer = new Grid
                {
                    Width = size,
                    Height = size,
                    Clip = new EllipseGeometry(new Point(size / 2, size / 2), size / 2, size / 2)
                };

                fillContainer.Children.Add(fillRect);
                Children.Add(fillContainer);
            }

            /// <summary>
            /// Sets the fill level from 0.0 (empty) to 1.0 (full).
            /// </summary>
            public void SetValue(double value)
            {
                value = Math.Clamp(value, 0, 1);
                fillRect.Height = value * size;
                if (value == 1)
                    fillRect.Fill = Brushes.White;
                else
                    fillRect.Fill = Brushes.DodgerBlue;

                // Force alignment from bottom (WPF default is top)
                fillRect.VerticalAlignment = VerticalAlignment.Bottom;
            }
        }
        /*****************************************************
         * Testing of filling disk neuron
         * ***********************************************/

        public static UIElement GetNeuronView(Neuron n, NeuronArrayView theNeuronArrayViewI, out TextBlock tb)
        {
            tb = null;
            theNeuronArrayView = theNeuronArrayViewI;

            Point p = dp.pointFromNeuron(n.id);

            if (dp.ShowNeuronCircles())
            {
                var fillableCircle = new FillableDisc(dp.NeuronDisplaySize * ellipseSize, n.id);
                fillableCircle.SetValue(n.currentCharge);
                float offset1 = (1 - ellipseSize) / 2f;
                Canvas.SetLeft(fillableCircle, p.X + dp.NeuronDisplaySize * offset1);
                Canvas.SetTop(fillableCircle, p.Y + dp.NeuronDisplaySize * offset1);
                fillableCircle.Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 315,      // angle of the shadow in degrees
                    ShadowDepth = dp.NeuronDisplaySize * ellipseSize * 0.1,      // distance of the shadow
                    Opacity = 0.5,        // transparency of the shadow
                    BlurRadius = 10       // softness of the shadow edge
                };
                if (n.Label != "" || n.model != Neuron.modelType.IF)
                {
                    tb = new TextBlock();
                    //l.Content = n.Label;
                    tb.FontSize = dp.NeuronDisplaySize * .25;
                    tb.FontWeight = FontWeights.Bold;
                    tb.Foreground = Brushes.White;

                    string theLabel = GetNeuronLabel(n);
                    string theToolTip = n.ToolTip;
                    if (theToolTip != "")
                    {
                        fillableCircle.ToolTip = new ToolTip { Content = theToolTip };
                        tb.ToolTip = new ToolTip { Content = theToolTip };
                    }
                    tb.Text = theLabel;
                    tb.HorizontalAlignment = HorizontalAlignment.Center;
                    tb.SetValue(NeuronIDProperty, n.id);
                    tb.SetValue(NeuronArrayView.ShapeType, NeuronArrayView.shapeType.Neuron);
                    tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    Size textSize = tb.DesiredSize;

                    Canvas.SetLeft(tb, p.X + (dp.NeuronDisplaySize - textSize.Width) / 2);
                    Canvas.SetTop(tb, p.Y + dp.NeuronDisplaySize * offset1);
                    Canvas.SetZIndex(tb, 100);
                }
                return fillableCircle;
            }
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
                r.Width = dp.NeuronDisplaySize-2;
                r.Height = dp.NeuronDisplaySize-2;
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

            if (n.Label != "" || n.model != Neuron.modelType.IF)
            {
                tb = new TextBlock();
                //l.Content = n.Label;
                tb.FontSize = dp.NeuronDisplaySize * .25;
                tb.Foreground = Brushes.White;
                Canvas.SetLeft(tb, p.X + dp.NeuronDisplaySize * offset);
                Canvas.SetTop(tb, p.Y + dp.NeuronDisplaySize * offset);
                Canvas.SetZIndex(tb, 100);

                string theLabel = GetNeuronLabel(n);
                string theToolTip = n.ToolTip;
                if (theToolTip != "")
                {
                    r.ToolTip = new ToolTip { Content = theToolTip };
                    tb.ToolTip = new ToolTip { Content = theToolTip };
                }
                tb.Text = theLabel;
                tb.SetValue(NeuronIDProperty, n.id);
                tb.SetValue(NeuronArrayView.ShapeType, NeuronArrayView.shapeType.Neuron);
            }
            r.SetValue(NeuronIDProperty,n.id);
            r.SetValue(NeuronArrayView.ShapeType, NeuronArrayView.shapeType.Neuron);
            return r;
        }

        public static string GetNeuronLabel(Neuron n)
        {
            string retVal = "";
            if (!dp.ShowNeuronLabels()) return retVal;

            retVal = n.Label;
            if (retVal.Length > 0 && retVal[0] == '_')
                retVal = retVal.Remove(0, 1);
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
            if (!n.inUse && n.Model == Neuron.modelType.IF && n.Label =="")
                s1.Opacity = .50;
            if ((n.leakRate < 0) || float.IsNegativeInfinity(1.0f / n.leakRate))
                s1 = new SolidColorBrush(Colors.LightSalmon);
            return s1;
        }
    }
}
