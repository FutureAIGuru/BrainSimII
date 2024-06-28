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

        public static UIElement GetNeuronView(Neuron n, NeuronArrayView theNeuronArrayViewI, out TextBlock tb)
        {
            tb = null;
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
