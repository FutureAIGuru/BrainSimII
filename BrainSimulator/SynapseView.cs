﻿//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace BrainSimulator
{

    public class SynapseView
    {
        public static DisplayParams dp;
        static NeuronArrayView theNeuronArrayView = null;
        public static Canvas theCanvas;

        public static readonly DependencyProperty SourceIDProperty =
                DependencyProperty.Register("SourceID", typeof(int), typeof(Shape));
        public static readonly DependencyProperty TargetIDProperty =
                DependencyProperty.Register("TargetID", typeof(int), typeof(Shape));
        public static readonly DependencyProperty WeightValProperty =
                DependencyProperty.Register("WeightVal", typeof(float), typeof(Shape));
        public static readonly DependencyProperty ModelProperty =
                DependencyProperty.Register("ModelVal", typeof(Synapse.modelType), typeof(Shape));



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

            Shape l = GetSynapseShape(p1, p2, s.model);
            l.Stroke = new SolidColorBrush(Utils.RainbowColorFromValue(s.weight));
            //if (s.weight >= 0)
            //    l.StrokeThickness *= 4 * s.weight;
            //l.StrokeThickness = Math.Clamp(l.StrokeThickness, 2, dp.NeuronDisplaySize / 4);
            l.StrokeEndLineCap = PenLineCap.Round;
            l.StrokeStartLineCap = PenLineCap.Round;

            if (l is Ellipse E)
            { }
            else
                l.Fill = l.Stroke;
            l.SetValue(SourceIDProperty, i);
            l.SetValue(TargetIDProperty, s.TargetNeuron);
            l.SetValue(WeightValProperty, s.Weight);
            l.SetValue(ModelProperty, s.model);
            l.SetValue(NeuronArrayView.ShapeType, NeuronArrayView.shapeType.Synapse);

            return l;
        }
        public static Shape GetSynapseShape(Point p1, Point p2, Synapse.modelType model)
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
                    s = DrawLinkArrow(p1 + offset, p2 + offset, model != Synapse.modelType.Fixed);
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
            s.StrokeThickness = 4;
            if (dp.ShowSynapseWideLines())
            {
                s.StrokeThickness = Math.Min(4, dp.NeuronDisplaySize / 8);
            }

            return s;
        }

        public static UIElement  GetWeightBargraph(Point p1, Point p2, double value)
        {
            Canvas canvas = new Canvas();

            // Compute center point of the line
            double centerX = (p1.X + p2.X) / 2;
            double centerY = (p1.Y + p2.Y) / 2;

            // Parameters for the rectangle (bar)
            double barWidth = dp.NeuronDisplaySize * .25; // fixed width
            double barMaxHeight = dp.NeuronDisplaySize*.5; // max height for full value
            // Draw rectangle (centered on line midpoint)
            Rectangle border = new Rectangle
            {
                Width = barWidth,
                Height = barMaxHeight,
                Fill = Brushes.White,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            border.Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                Direction = 315,      // angle of the shadow in degrees
                ShadowDepth = dp.NeuronDisplaySize * 0.05,      // distance of the shadow
                Opacity = 0.5,        // transparency of the shadow
                BlurRadius = 10       // softness of the shadow edge
            };

            // Position the border so its center is at the midpoint
            Canvas.SetLeft(border, centerX - barWidth / 2);
            Canvas.SetTop(border, centerY - barMaxHeight / 2);
            canvas.Children.Add(border);

            // Height scaled to value
            double barHeight = barMaxHeight * value;
            if (barHeight < 0) barHeight = 0;

            // Draw rectangle (centered on line midpoint)
            Rectangle bar = new Rectangle
            {
                Width = barWidth,
                Height = barHeight,
                Fill = Brushes.SteelBlue,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            // Position rectangle (centered horizontally, aligned vertically from center)
            Canvas.SetLeft(bar, centerX - barWidth / 2);
            Canvas.SetTop(bar, centerY - barMaxHeight / 2 +  (barMaxHeight - barHeight));
            canvas.Children.Add(bar);

            // Add value text
            TextBlock text = new TextBlock
            {
                Text = $"{value:0.000}",
                Foreground = Brushes.Black,
                FontSize = dp.NeuronDisplaySize * .09,
                FontWeight = FontWeights.Bold
            };

            // Measure text size
            text.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            Size textSize = text.DesiredSize;

            // Position text centered on rectangle
            Canvas.SetLeft(text, centerX - textSize.Width / 2);
            Canvas.SetTop(text, centerY - textSize.Height / 2);
            canvas.Children.Add(text);
            return canvas;
        }


        public static Shape DrawLinkArrow(Point p1, Point p2, bool canLearn) //helper to put an arrow in a synapse line
        {
            GeometryGroup lineGroup = new GeometryGroup();
            double theta = Math.Atan2((p2.Y - p1.Y), (p2.X - p1.X)) * 180 / Math.PI;

            PathGeometry pathGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();
            Vector v = p2 - p1;
            double lengthFactor = ((dp.NeuronDisplaySize * 0.7f / 2f) + 3) / v.Length;
            //lengthfactor = 0.5; //how it used to be
            v = v * lengthFactor;

            Point p = new Point();
            p = p2 - v;
            pathFigure.StartPoint = p;
            double arrowWidth = dp.NeuronDisplaySize / 20;
            double arrowLength = dp.NeuronDisplaySize / 15;
            if (canLearn)
            {
                arrowWidth *= 2;
            }
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
            return path;
        }


        //these aren't added to synapses (for performance) but are built on the fly if the user right-clicks
        public static void CreateContextMenu(int i, Synapse s, ContextMenu cm)
        {
            cmCancelled = false;
            weightChanged = false;

            //set defaults for next synapse add
            theNeuronArrayView.LastSynapseModel = s.model;
            theNeuronArrayView.LastSynapseWeight = s.weight;


            cm.SetValue(SourceIDProperty, i);
            cm.SetValue(TargetIDProperty, s.TargetNeuron);
            cm.SetValue(WeightValProperty, s.Weight);
            cm.SetValue(ModelProperty, s.model);

            cm.Closed += Cm_Closed;
            cm.PreviewKeyDown += Cm_PreviewKeyDown;
            cm.Opened += Cm_Opened;

            Neuron nSource = MainWindow.theNeuronArray.GetNeuron(i);
            Neuron nTarget = MainWindow.theNeuronArray.GetNeuron(s.targetNeuron);
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
            sp.Children.Add(new Label { Content = "Source: ", Padding = new Thickness(0) });
            string source = nSource.id.ToString();
            if (nSource.label != "")
                source = nSource.Label;
            TextBox t0 = Utils.ContextMenuTextBox(source, "Source", 150);
            t0.TextChanged += TextChanged;
            sp.Children.Add(t0);
            cm.Items.Add(new MenuItem { Header = sp, StaysOpenOnClick = true });

            sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
            sp.Children.Add(new Label { Content = "Target: ", Padding = new Thickness(0) });
            string target = nTarget.id.ToString();
            if (nTarget.label != "")
                target = nTarget.Label;
            TextBox t1 = Utils.ContextMenuTextBox(target, "Target", 150);
            t1.TextChanged += TextChanged;
            sp.Children.Add(t1);
            cm.Items.Add(new MenuItem { Header = sp, StaysOpenOnClick = true });

            //The Synapse model
            sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
            sp.Children.Add(new Label { Content = "Model: ", Padding = new Thickness(0) });
            ComboBox cb = new ComboBox()
            {
                Width = 100,
                Name = "Model"
            };
            for (int index = 0; index < Enum.GetValues(typeof(Synapse.modelType)).Length; index++)
            {
                Synapse.modelType model = (Synapse.modelType)index;
                cb.Items.Add(new ListBoxItem()
                {
                    Content = model.ToString(),
                    ToolTip = Synapse.modelToolTip[index],
                    Width = 100,
                });
            }
            cb.SelectedIndex = (int)s.model;
            sp.Children.Add(cb);
            cm.Items.Add(new MenuItem { Header = sp, StaysOpenOnClick = true });

            cm.Items.Add(Utils.CreateComboBoxMenuItem("SynapseWeight", s.weight, synapseWeightValues, "F3", "Weight: ", 100, ComboBox_ContentChanged));

            MenuItem mi = new MenuItem();
            mi.Header = "Delete";
            mi.Click += DeleteSynapse_Click;
            cm.Items.Add(mi);

            sp = new StackPanel { Orientation = Orientation.Horizontal };
            Button b0 = new Button { Content = "OK", Width = 100, Height = 25, Margin = new Thickness(10), IsDefault = true };
            b0.Click += B0_Click;
            sp.Children.Add(b0);
            b0 = new Button { Content = "Cancel", Width = 100, Height = 25, Margin = new Thickness(10), IsCancel = true };
            b0.Click += B0_Click;
            sp.Children.Add(b0);

            cm.Items.Add(new MenuItem { Header = sp, StaysOpenOnClick = true });

        }

        private static void TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                //is numeric?
                if (int.TryParse(tb.Text, out int newID))
                {
                    if (newID < 0 || newID >= MainWindow.theNeuronArray.arraySize)
                        tb.Background = new SolidColorBrush(Colors.Pink);
                    else
                        tb.Background = new SolidColorBrush(Colors.LightGreen);
                }
                else //is non-numeric
                {
                    Neuron n = MainWindow.theNeuronArray.GetNeuron(tb.Text);
                    {
                        if (n == null)
                            tb.Background = new SolidColorBrush(Colors.Pink);
                        else
                            tb.Background = new SolidColorBrush(Colors.LightGreen);
                    }
                }
            }
        }

        static bool weightChanged = false;
        static List<float> synapseWeightValues = new List<float> { 1, .5f, .334f, .25f, .2f, 0, -1 };
        private static void ComboBox_ContentChanged(object sender, object e)
        {
            if (sender is ComboBox cb)
            {
                if (!cb.IsArrangeValid) return;
                if (cb.Name == "SynapseWeight")
                {
                    weightChanged = true;
                    Utils.ValidateInput(cb, -1, 1);
                }
            }
        }

        private static void Cm_Opened(object sender, RoutedEventArgs e)
        {
            //when the context menu opens, focus on the label and position text cursor to end
            if (sender is ContextMenu cm)
            {
                if (Utils.FindByName(cm, "SynapseWeight") is ComboBox cc)
                {
                    //this hack finds the textbox within a combobox
                    TextBox tb = (TextBox)cc.Template.FindName("PART_EditableTextBox", cc);
                    tb.Focus();
                    tb.Select(0, tb.Text.Length);
                }
            }
        }

        private static void Cm_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ContextMenu cm = sender as ContextMenu;
            if (e.Key == Key.Delete)
            {
                var focussedControl = FocusManager.GetFocusedElement(cm);
                if (focussedControl.GetType() != typeof(TextBox))
                {
                    MainWindow.theNeuronArray.GetNeuron((int)cm.GetValue(SourceIDProperty)).DeleteSynapse((int)cm.GetValue(TargetIDProperty));
                    MainWindow.Update();
                    cmCancelled = true;
                    cm.IsOpen = false;
                }
            }
            if (e.Key == Key.Enter)
            {
                cm.IsOpen = false;
            }
            //This hack is here because textboxes don't like to lose focus if the mouse moves aroundt the context menu
            //When this becomes a window, all this will go away
            if (e.Key == Key.Tab)
            {
                {
                    var focussedControl = FocusManager.GetFocusedElement(cm);
                    if (focussedControl is TextBox tb)
                    {
                        if (tb.Name == "Source")
                        {
                            Control tt = Utils.FindByName(cm, "Target");
                            if (tt is TextBox tbtt)
                            {
                                tbtt.Focus();
                                e.Handled = true;
                            }
                            else
                            {
                                Control cc = Utils.FindByName(cm, "Model");
                                cc.Focus();
                                e.Handled = true;
                            }
                        }
                        else if (tb.Name == "Target")
                        {
                            Control cc = Utils.FindByName(cm, "Model");
                            cc.Focus();
                            e.Handled = true;

                        }
                    }
                }
            }

        }

        static bool cmCancelled = false;
        private static void Cm_Closed(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu cm)
            {
                if (cmCancelled || (Keyboard.GetKeyStates(Key.Escape) & KeyStates.Down) > 0)
                {
                    cm.IsOpen = false;
                    MainWindow.Update();
                    return;
                }
                int sourceID = (int)cm.GetValue(SourceIDProperty);
                int targetID = (int)cm.GetValue(TargetIDProperty);
                Neuron nSource = MainWindow.theNeuronArray.GetNeuron(sourceID);
                Neuron nTarget = MainWindow.theNeuronArray.GetNeuron(targetID);
                int newSourceID = sourceID;
                int newTargetID = targetID;

                Control cc = Utils.FindByName(cm, "Target");
                if (cc is TextBox tb)
                {
                    string targetLabel = tb.Text;
                    if (nTarget.label != targetLabel)
                    {
                        if (!int.TryParse(tb.Text, out newTargetID))
                        {
                            newTargetID = targetID;
                            Neuron n = MainWindow.theNeuronArray.GetNeuron(targetLabel);
                            if (n != null)
                                newTargetID = n.id;
                        }
                    }
                }
                cc = Utils.FindByName(cm, "Source");
                if (cc is TextBox tb1)
                {
                    string sourceLabel = tb1.Text;
                    if (nSource.label != sourceLabel)
                    {
                        if (!int.TryParse(tb1.Text, out newSourceID))
                        {
                            newSourceID = sourceID;
                            Neuron n = MainWindow.theNeuronArray.GetNeuron(sourceLabel);
                            if (n != null)
                                newSourceID = n.id;
                        }
                    }
                }
                if (newSourceID < 0 || newSourceID >= MainWindow.theNeuronArray.arraySize ||
                    newTargetID < 0 || newTargetID >= MainWindow.theNeuronArray.arraySize
                    )
                {
                    MessageBox.Show("Neuron outside array boundary!");
                    return;
                }
                cc = Utils.FindByName(cm, "SynapseWeight");
                float newWeight = 1f;
                if (cc is ComboBox tb2)
                {
                    float.TryParse(tb2.Text, out newWeight);
                    if (weightChanged)
                    {
                        theNeuronArrayView.LastSynapseWeight = newWeight;
                        Utils.AddToValues(newWeight, synapseWeightValues);
                    }
                }
                cc = Utils.FindByName(cm, "Model");
                Synapse.modelType newModel = Synapse.modelType.Fixed;
                if (cc is ComboBox cb0)
                {
                    ListBoxItem lbi = (ListBoxItem)cb0.SelectedItem;
                    newModel = (Synapse.modelType)System.Enum.Parse(typeof(Synapse.modelType), lbi.Content.ToString());
                    theNeuronArrayView.LastSynapseModel = newModel;
                }

                if (newSourceID != sourceID || newTargetID != targetID)
                {
                    MainWindow.theNeuronArray.SetUndoPoint();
                    MainWindow.theNeuronArray.GetNeuron((int)cm.GetValue(SourceIDProperty)).DeleteSynapseWithUndo((int)cm.GetValue(TargetIDProperty));
                }
                Neuron nNewSource = MainWindow.theNeuronArray.GetNeuron(newSourceID);
                nNewSource.AddSynapseWithUndo(newTargetID, newWeight, newModel);
                cm.IsOpen = false;
                MainWindow.Update();
            }
        }
        private static void B0_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                if (b.Parent is StackPanel sp)
                {
                    if (sp.Parent is MenuItem mi)
                    {
                        if (mi.Parent is ContextMenu cm)
                        {
                            if ((string)b.Content == "Cancel")
                                cmCancelled = true;
                            Cm_Closed(cm, e);
                        }
                    }
                }
            }
        }


        public static void StepAndRepeatSynapse_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            ContextMenu cm = mi.Parent as ContextMenu;
            theNeuronArrayView.StepAndRepeat(
                (int)cm.GetValue(SourceIDProperty),
                (int)cm.GetValue(TargetIDProperty),
                (float)cm.GetValue(WeightValProperty),
                Synapse.modelType.Fixed); //TODO: handle hebbian/model
        }

        public static void DeleteSynapse_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.theNeuronArray.SetUndoPoint();
            MenuItem mi = (MenuItem)sender;
            ContextMenu cm = mi.Parent as ContextMenu;
            MainWindow.theNeuronArray.GetNeuron((int)cm.GetValue(SourceIDProperty)).DeleteSynapseWithUndo((int)cm.GetValue(TargetIDProperty));
            cm.IsOpen = false;
            cmCancelled = true;
            MainWindow.Update();
        }

    }
}
