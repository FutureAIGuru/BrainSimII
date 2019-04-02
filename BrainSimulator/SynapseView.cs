using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Controls;

namespace BrainSimulator
{
    public class myMenuItem : MenuItem
    {
        public int Source, Target;
        public float Weight;
    }
    public class myTextBox : TextBox
    {
        public int Source, Target;
        public float Weight;
    }
    public static class SynapseView
    {
        public static DisplayParams dp;
        static int selectedSynapseSource = -1;
        static int selectedSynapseTarget = -1;
        static NeuronArrayView theNeuronArrayView = null;
        public static Canvas theCanvas;
        public static NeuronArrayView.MouseMode theMouseMode = NeuronArrayView.MouseMode.pan;

        static bool PtOnScreen(Point p)
        {
            if (p.X < -dp.NeuronDisplaySize) return false;
            if (p.Y < -dp.NeuronDisplaySize) return false;
            if (p.X > theCanvas.ActualWidth + dp.NeuronDisplaySize) return false;
            if (p.Y > theCanvas.ActualHeight + dp.NeuronDisplaySize) return false;
            return true;
        }

        public static Shape SynapseDisplay(int i, Point p1, Synapse s, NeuronArrayView theNeuronArrayView1)
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

            if (dp.NeuronDisplaySize > 45 && theMouseMode == NeuronArrayView.MouseMode.synapse)
            {
                l.ToolTip = i + " " + s.TargetNeuron + " " + s.Weight.ToString("f2");

                ContextMenu cm = new ContextMenu();

                myMenuItem mi = new myMenuItem();
                mi.Header = "Delete";
                mi.Source = i;
                mi.Target = s.TargetNeuron;
                mi.Weight = s.Weight;
                mi.Click += DeleteSynapse_Click;
                cm.Items.Add(mi);

                mi = new myMenuItem();
                mi.Header = "Step & Repeat";
                mi.Source = i;
                mi.Target = s.TargetNeuron;
                mi.Weight = s.Weight;
                mi.Click += StepAndRepeatSynapse_Click;
                cm.Items.Add(mi);

                mi = new myMenuItem();
                mi.Header = "1";
                mi.Source = i;
                mi.Target = s.TargetNeuron;
                mi.Weight = s.Weight;
                mi.Click += ANDSynapse_Click;
                cm.Items.Add(mi);

                mi = new myMenuItem();
                mi.Header = ".5";
                mi.Source = i;
                mi.Target = s.TargetNeuron;
                mi.Weight = s.Weight;
                mi.Click += ANDSynapse_Click;
                cm.Items.Add(mi);

                mi = new myMenuItem();
                mi.Header = ".33";
                mi.Source = i;
                mi.Target = s.TargetNeuron;
                mi.Weight = s.Weight;
                mi.Click += ANDSynapse_Click;
                cm.Items.Add(mi);

                mi = new myMenuItem();
                mi.Header = ".25";
                mi.Source = i;
                mi.Target = s.TargetNeuron;
                mi.Weight = s.Weight;
                mi.Click += ANDSynapse_Click;
                cm.Items.Add(mi);

                mi = new myMenuItem();
                mi.Header = "-1";
                mi.Source = i;
                mi.Target = s.TargetNeuron;
                mi.Weight = s.Weight;
                mi.Click += ANDSynapse_Click;
                cm.Items.Add(mi);
                mi = new myMenuItem();

                mi.Header = "Weight:";
                mi.IsEnabled = false;
                mi.Source = i;
                mi.Target = s.TargetNeuron;
                mi.Weight = s.Weight;
                mi.Click += ANDSynapse_Click;
                cm.Items.Add(mi);
                myTextBox tb = new myTextBox();
                tb.Text = s.Weight.ToString("F4");
                tb.Source = i;
                tb.Target = s.TargetNeuron;
                tb.Weight = s.Weight;
                tb.Width = 200;
                tb.TextChanged += Tb_TextChanged;
                cm.Items.Add(tb);

                l.ContextMenu = cm;
                cm.Closed += Cm_Closed;
            }
            return l;
        }

        private static void Cm_Closed(object sender, RoutedEventArgs e)
        {
            MainWindow.Update();
        }

        private static void Tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            myTextBox tb = (myTextBox)sender;
            //find out which neuron this context menu is from
            ContextMenu cm = (ContextMenu)tb.Parent;
            float newWeight = -20;
            float.TryParse(tb.Text, out newWeight);
            if (newWeight == -20) return;
            theNeuronArrayView.lastSynapseWeight = newWeight;
            Neuron n = MainWindow.theNeuronArray.neuronArray[tb.Source];
            n.AddSynapse(tb.Target, newWeight, MainWindow.theNeuronArray);
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
            myMenuItem mi = (myMenuItem)sender;
            theNeuronArrayView.StepAndRepeat(mi.Source, mi.Target, mi.Weight);
        }

        public static void ANDSynapse_Click(object sender, RoutedEventArgs e)
        {
            myMenuItem mi = (myMenuItem)sender;
            float weight = 0;
            if ((string)mi.Header == "1")
                weight = 1.0f;
            if ((string)mi.Header == ".5")
                weight = 0.5f;
            if ((string)mi.Header == ".33")
                weight = 0.33f;
            if ((string)mi.Header == ".25")
                weight = 0.25f;
            if ((string)mi.Header == "-1")
                weight = -1f;
            MainWindow.theNeuronArray.neuronArray[mi.Source].AddSynapse(mi.Target, weight, MainWindow.theNeuronArray);
            theNeuronArrayView.lastSynapseWeight = weight;
            MainWindow.Update();
        }

        public static void DeleteSynapse_Click(object sender, RoutedEventArgs e)
        {
            myMenuItem mi = (myMenuItem)sender;
            string[] s = mi.Name.Split('_');
            MainWindow.theNeuronArray.neuronArray[mi.Source].DeleteSynapse(mi.Target);
            MainWindow.Update();
        }


        public static Shape GetSynapseShape(Point p1, Point p2, NeuronArrayView theNeuronDisplayView)
        {
            //returns a line from the source to the destination 
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
                if (dp.NeuronDisplaySize > 45)
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
            if (dp.NeuronDisplaySize > 15)
                s.StrokeThickness = dp.NeuronDisplaySize / 15;
            s.MouseDown += theNeuronDisplayView.theCanvas_MouseDown;
            s.MouseUp += theNeuronDisplayView.theCanvas_MouseUp;
            return s;
        }

        public static Shape DrawLinkArrow(Point p1, Point p2) //helper to put an arrow in a synapse line
        {
            GeometryGroup lineGroup = new GeometryGroup();
            double theta = Math.Atan2((p2.Y - p1.Y), (p2.X - p1.X)) * 180 / Math.PI;

            PathGeometry pathGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();
            //            Point p = new Point(p1.X + ((p2.X - p1.X) / 1.35), p1.Y + ((p2.Y - p1.Y) / 1.35));
            Vector v = p2 - p1;
            v = v / v.Length * (dp.NeuronDisplaySize / 2);
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
