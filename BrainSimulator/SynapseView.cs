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
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;

namespace BrainSimulator
{

    public class SynapseView : DependencyObject
    {
        public static DisplayParams dp;
        static int selectedSynapseSource = -1;
        static int selectedSynapseTarget = -1;
        static NeuronArrayView theNeuronArrayView = null;
        public static Canvas theCanvas;

        public static readonly DependencyProperty SourceIDProperty =
        DependencyProperty.Register("SourceID", typeof(int), typeof(MenuItem));
        public int SourceID
        {
            get { return (int)GetValue(SourceIDProperty); }
            set { SetValue(SourceIDProperty, value); }
        }
        public static readonly DependencyProperty TargetIDProperty =
                DependencyProperty.Register("TargetID", typeof(int), typeof(MenuItem));
        public int TargetID
        {
            get { return (int)GetValue(TargetIDProperty); }
            set { SetValue(TargetIDProperty, value); }
        }
        public static readonly DependencyProperty WeightValProperty =
                DependencyProperty.Register("WeightVal", typeof(float), typeof(MenuItem));
        public float WeightVal
        {
            get { return (int)GetValue(WeightValProperty); }
            set { SetValue(WeightValProperty, value); }
        }



        static bool PtOnScreen(Point p)
        {
            if (p.X < -dp.NeuronDisplaySize) return false;
            if (p.Y < -dp.NeuronDisplaySize) return false;
            if (p.X > theCanvas.ActualWidth + dp.NeuronDisplaySize) return false;
            if (p.Y > theCanvas.ActualHeight + dp.NeuronDisplaySize) return false;
            return true;
        }

        public static Shape GetSynapseView(int i, Point p1, Synapse s, NeuronArrayView theNeuronArrayView1)
        {
            theNeuronArrayView = theNeuronArrayView1;
            Point p2 = dp.pointFromNeuron(s.TargetNeuron);
            if (!PtOnScreen(p1) && !PtOnScreen(p2)) return null;

            Shape l = GetSynapseShape(p1, p2, theNeuronArrayView);
            l.Stroke = Brushes.Red;
            if (s.Weight < 0.9)
                l.Stroke = Brushes.Yellow;
            if (s.Weight < 0.1)
                l.Stroke = Brushes.Green;
            if (s.Weight < -0.1)
                l.Stroke = Brushes.Blue;
            if (s.Weight < -0.9)
                l.Stroke = Brushes.Black;
            l.SetValue(SourceIDProperty, i);
            l.SetValue(TargetIDProperty, s.TargetNeuron);
            l.SetValue(WeightValProperty, s.Weight);

            return l;
        }

        //these aren't added to synapses for performance but are built on the fly if the user right-clicks
        public static void CreateContextMenu(int i, Synapse s, ContextMenu cm)
        {
            cm.SetValue(SourceIDProperty, i);
            cm.SetValue(TargetIDProperty, s.TargetNeuron);
            cm.SetValue(WeightValProperty, s.Weight);
            MenuItem mi = new MenuItem();
            mi.Header = "Delete";
            mi.Click += DeleteSynapse_Click;
            cm.Items.Add(mi);

            mi = new MenuItem();
            mi.Header = "Step & Repeat";
            mi.Click += StepAndRepeatSynapse_Click;
            cm.Items.Add(mi);

            mi = new MenuItem();
            mi.Header = "1";
            mi.Click += ANDSynapse_Click;
            cm.Items.Add(mi);

            mi = new MenuItem();
            mi.Header = ".9";
            mi.Click += ANDSynapse_Click;
            cm.Items.Add(mi);

            mi = new MenuItem();
            mi.Header = ".5";
            mi.Click += ANDSynapse_Click;
            cm.Items.Add(mi);

            mi = new MenuItem();
            mi.Header = ".34";
            mi.Click += ANDSynapse_Click;
            cm.Items.Add(mi);

            mi = new MenuItem();
            mi.Header = ".25";
            mi.Click += ANDSynapse_Click;
            cm.Items.Add(mi);

            mi = new MenuItem();
            mi.Header = "-1";
            mi.Click += ANDSynapse_Click;
            cm.Items.Add(mi);
            mi = new MenuItem();

            mi.Header = "Weight:";
            mi.IsEnabled = false;
            mi.Click += ANDSynapse_Click;
            cm.Items.Add(mi);
            TextBox tb = new TextBox();
            tb.Text = s.Weight.ToString("F4");
            tb.Width = 200;
            tb.TextChanged += Tb_TextChanged;
            cm.Items.Add(tb);
            CheckBox cbHebbian = new CheckBox
            {
                IsChecked = s.IsHebbian,
                Content = "Hebbian Learning",
                Name = "Hebbian",
            };
            cbHebbian.Checked += cbHebbianChecked;
            cbHebbian.Unchecked += cbHebbianChecked;
            cm.Items.Add(cbHebbian);
        }

        private static void cbHebbianChecked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            ContextMenu cm = cb.Parent as ContextMenu;
            cm.IsOpen = false;
            CheckBox cbHebbian = (CheckBox)Utils.FindByName(cm, "Hebbian");
            Synapse s = MainWindow.theNeuronArray.GetNeuron((int)cm.GetValue(SourceIDProperty)).
                FindSynapse((int)cm.GetValue(TargetIDProperty));
            if (s != null)
                s.IsHebbian = (bool)cb.IsChecked;
        }

        private static void Cm_Closed(object sender, RoutedEventArgs e)
        {
            MainWindow.Update();
        }

        private static void Tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            //find out which neuron this context menu is from
            ContextMenu cm = tb.Parent as ContextMenu;
            float newWeight = -20;
            float.TryParse(tb.Text, out newWeight);
            if (newWeight == -20) return;
            theNeuronArrayView.lastSynapseWeight = newWeight;
            Neuron n = MainWindow.theNeuronArray.GetNeuron((int)cm.GetValue(SourceIDProperty));
            n.AddSynapse((int)cm.GetValue(TargetIDProperty), newWeight, MainWindow.theNeuronArray,true);
        }

        public static void GetSynapseInfo(object sender)
        {
            Shape shape = (Shape)sender;
            shape.Stroke = System.Windows.Media.Brushes.Blue;
            if (shape.GetType() == typeof(Line))
            {
                Line l = (Line)shape;
                Point p1 = new Point(l.X1, l.Y1);
                Point p2 = new Point(l.X2, l.Y2);
                selectedSynapseSource = dp.NeuronFromPoint(p1);
                selectedSynapseTarget = dp.NeuronFromPoint(p2);
            }
            else if (shape.GetType() == typeof(Ellipse))
            {
                Point p1 = new Point(Canvas.GetLeft(shape), Canvas.GetTop(shape) + dp.NeuronDisplaySize / 2);
                selectedSynapseSource = selectedSynapseTarget = dp.NeuronFromPoint(p1);
            }
        }

        public static void StepAndRepeatSynapse_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            ContextMenu cm = mi.Parent as ContextMenu;
            theNeuronArrayView.StepAndRepeat((int)cm.GetValue(SourceIDProperty), (int)cm.GetValue(TargetIDProperty), (float)cm.GetValue(WeightValProperty));
        }

        public static void ANDSynapse_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            ContextMenu cm = mi.Parent as ContextMenu;
            float weight = 0;
            float.TryParse((string)mi.Header, out weight);
            MainWindow.theNeuronArray.GetNeuron((int)cm.GetValue(SourceIDProperty)).
                AddSynapse((int)cm.GetValue(TargetIDProperty), weight, MainWindow.theNeuronArray,true);
            theNeuronArrayView.lastSynapseWeight = weight;
            MainWindow.Update();
        }

        public static void DeleteSynapse_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            ContextMenu cm = mi.Parent as ContextMenu;
            MainWindow.theNeuronArray.GetNeuron((int)cm.GetValue(SourceIDProperty)).DeleteSynapse((int)cm.GetValue(TargetIDProperty));
            MainWindow.Update();
        }


        public static Shape GetSynapseShape(Point p1, Point p2, NeuronArrayView theNeuronDisplayView)
        {
            //returns a line from the source to the destination (with a link arrow at larger zooms
            //unless the source and destination are the same in which it returns an arc
            Shape s;
            if (p1 != p2)
            {
                Line l = new Line();
                l.X1 = p1.X + dp.NeuronDisplaySize / 2;
                l.X2 = p2.X + dp.NeuronDisplaySize / 2;
                l.Y1 = p1.Y + dp.NeuronDisplaySize / 2;
                l.Y2 = p2.Y + dp.NeuronDisplaySize / 2;
                s = l;
                if (dp.ShowSynapseArrows())
                {
                    Vector offset = new Vector(dp.NeuronDisplaySize / 2, dp.NeuronDisplaySize / 2);
                    s = DrawLinkArrow(p1 + offset, p2 + offset);
                }
            }
            else
            {
                s = new Ellipse();
                s.Height = s.Width = dp.NeuronDisplaySize * .6;
                Canvas.SetTop(s, p1.Y + dp.NeuronDisplaySize / 4);
                Canvas.SetLeft(s, p1.X + dp.NeuronDisplaySize / 2);
            }
            s.Stroke = Brushes.Red;
            s.StrokeThickness = 1;
            if (dp.ShowSynapseWideLines())
                s.StrokeThickness = dp.NeuronDisplaySize / 15;
            s.MouseDown += theNeuronDisplayView.theCanvas_MouseDown;
            s.MouseUp += theNeuronDisplayView.theCanvas_MouseUp;
            if (dp.ShowSynapseArrowCursor())
            {
                s.MouseEnter += S_MouseEnter;
                s.MouseLeave += S_MouseLeave;
            }
            return s;
        }

        private static void S_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (theCanvas.Cursor == Cursors.Arrow)// && theNeuronArrayView != null && !theNeuronArrayView.dragging)
                theCanvas.Cursor = Cursors.Cross;
        }

        private static void S_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.RightButton != MouseButtonState.Pressed && e.LeftButton != MouseButtonState.Pressed && theCanvas.Cursor != Cursors.Hand)
                theCanvas.Cursor = Cursors.Arrow;
        }

        public static Shape DrawLinkArrow(Point p1, Point p2) //helper to put an arrow in a synapse line
        {
            GeometryGroup lineGroup = new GeometryGroup();
            double theta = Math.Atan2((p2.Y - p1.Y), (p2.X - p1.X)) * 180 / Math.PI;

            PathGeometry pathGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();
            //            Point p = new Point(p1.X + ((p2.X - p1.X) / 1.35), p1.Y + ((p2.Y - p1.Y) / 1.35));
            Vector v = p2 - p1;
            //v = v / v.Length * (dp.NeuronDisplaySize / 2);
            v = v / 2;
            Point p = new Point();
            p = p2 - v;
            pathFigure.StartPoint = p;
            double arrowWidth = dp.NeuronDisplaySize / 15;
            double arrowLength = dp.NeuronDisplaySize / 10;
            Point lpoint = new Point(p.X + arrowWidth, p.Y + arrowLength);
            Point rpoint = new Point(p.X - arrowWidth, p.Y + arrowLength);
            LineSegment seg1 = new LineSegment();
            seg1.Point = lpoint;
            pathFigure.Segments.Add(seg1);

            LineSegment seg2 = new LineSegment();
            seg2.Point = rpoint;
            pathFigure.Segments.Add(seg2);

            LineSegment seg3 = new LineSegment();
            seg3.Point = p;
            pathFigure.Segments.Add(seg3);

            pathGeometry.Figures.Add(pathFigure);
            RotateTransform transform = new RotateTransform();
            transform.Angle = theta + 90;
            transform.CenterX = p.X;
            transform.CenterY = p.Y;
            pathGeometry.Transform = transform;
            lineGroup.Children.Add(pathGeometry);

            LineGeometry connectorGeometry = new LineGeometry();
            connectorGeometry.StartPoint = p1;
            connectorGeometry.EndPoint = p2;
            lineGroup.Children.Add(connectorGeometry);
            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
            path.Data = lineGroup;
            path.StrokeThickness = 2;
            path.Stroke = path.Fill = Brushes.Red;
            return path;
        }

    }
}
