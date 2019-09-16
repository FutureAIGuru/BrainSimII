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
using System.Diagnostics;
using System.Windows.Media.Imaging;

namespace BrainSimulator
{
    public  class NeuronView : DependencyObject
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

        public static UIElement GetNeuronView(int i, NeuronArrayView theNeuronArrayViewI)
        {
            theNeuronArrayView = theNeuronArrayViewI;

            Neuron n = MainWindow.theNeuronArray.neuronArray[i];
            Point p = dp.pointFromNeuron(i);

            if (p.X < -dp.NeuronDisplaySize) return null;
            if (p.Y < -dp.NeuronDisplaySize) return null;
            if (p.X > theCanvas.ActualWidth + dp.NeuronDisplaySize) return null;
            if (p.Y > theCanvas.ActualHeight + dp.NeuronDisplaySize) return null;

            // figure out which color to use
            float value = n.LastCharge;
            Color c = Colors.White;
            if ((n.Model == Neuron.modelType.Std || n.Model == Neuron.modelType.OneTime) && value > .99)
                c = Colors.Orange;
            else if ((n.Model == Neuron.modelType.Std || n.Model == Neuron.modelType.OneTime) && value != -1)
                c = MapRainbowColor(value, 1, 0);
            else if (n.Model == Neuron.modelType.Color)
                c = Utils.FromArgb((int)n.LastChargeInt);
            SolidColorBrush s1 = new SolidColorBrush(c);
            //   if (n.Label != "" || !n.InUse()) s1.Opacity = .50;
            if (!n.InUse() && n.Model == Neuron.modelType.Std) s1.Opacity = .50;

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

            r.MouseDown += theNeuronArrayView.theCanvas_MouseDown;
            r.MouseUp += theNeuronArrayView.theCanvas_MouseUp;
            r.MouseWheel += theNeuronArrayView.theCanvas_MouseWheel;
            
            float offset = (1 - ellipseSize) / 2f;
            Canvas.SetLeft(r, p.X + dp.NeuronDisplaySize * offset);
            Canvas.SetTop(r, p.Y + dp.NeuronDisplaySize * offset);
            if (dp.ShowNeuronArrowCursor())
            {
                r.MouseEnter += R_MouseEnter;
                r.MouseLeave += R_MouseLeave;
            }


            if (n.Label != "")
            {
                Label l = new Label();
                l.Content = n.Label;
                l.FontSize = dp.NeuronDisplaySize * .3;
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
                theCanvas.Children.Add(l);
            }
            return r;
        }

        private static void R_MouseLeave(object sender, MouseEventArgs e)
        {
            if (theCanvas.Cursor != Cursors.Hand && !theNeuronArrayView.dragging && e.LeftButton != MouseButtonState.Pressed)
                theCanvas.Cursor = Cursors.Cross;
        }

        private static void R_MouseEnter(object sender, MouseEventArgs e)
        {
            if (theCanvas.Cursor != Cursors.Hand && !theNeuronArrayView.dragging)
                theCanvas.Cursor = Cursors.UpArrow;
        }

        //for UI performance, the context menu is not attached to a neuron when the neuron is created but
        //is built on the fly when a neuron is right-clicked.  Hence the public-static
        public static void CreateContextMenu(int i, Neuron n, ContextMenu cm)
        {
            cm.SetValue(NeuronIDProperty, n.Id);
            MenuItem mi = new MenuItem();
            mi = new MenuItem();
            mi.Header = "Current Charge: " + n.LastCharge.ToString("f2");
            cm.Items.Add(mi);
            mi = new MenuItem();
            mi.Header = "Neuron ID: " + n.Id;
            cm.Items.Add(mi);
            cm.Items.Add(new Separator());
            mi = new MenuItem();
            mi.Header = "Always Fire";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);
            mi = new MenuItem();
            mi.Header = "Paste Here";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);
            mi = new MenuItem();
            mi.Header = "Clipboard";
            mi.Click += Mi_Click;
            mi.Items.Add(new MenuItem() { Header = "Copy Selection" });
            ((MenuItem)mi.Items[mi.Items.Count - 1]).Click += Mi_Click;
            mi.Items.Add(new MenuItem() { Header = "Paste Here" });
            ((MenuItem)mi.Items[mi.Items.Count - 1]).Click += Mi_Click;
            mi.Items.Add(new MenuItem() { Header = "Move Here" });
            ((MenuItem)mi.Items[mi.Items.Count - 1]).Click += Mi_Click;
            mi.Items.Add(new MenuItem() { Header = "Connect to Here" });
            ((MenuItem)mi.Items[mi.Items.Count - 1]).Click += Mi_Click;
            mi.Items.Add(new MenuItem() { Header = "Connect from Here" });
            ((MenuItem)mi.Items[mi.Items.Count - 1]).Click += Mi_Click;
            mi.Items.Add(new MenuItem() { Header = "Select as Target" });
            ((MenuItem)mi.Items[mi.Items.Count - 1]).Click += Mi_Click;
            cm.Items.Add(mi);
            mi = new MenuItem();
            mi.Header = "Label:";
            mi.IsEnabled = false;
            cm.Items.Add(mi);
            TextBox tb = new TextBox { Text = n.Label, Width = 200 };
            tb.TextChanged += Tb_TextChanged;
            cm.Items.Add(tb);
            cm.Closed += Cm_Closed;
            mi = new MenuItem();
            mi.Header = "Model:";
            mi.IsEnabled = false;
            cm.Items.Add(mi);
            ComboBox cb = new ComboBox()
            { Width = 100 };
            foreach (Neuron.modelType model in (Neuron.modelType[])Enum.GetValues(typeof(Neuron.modelType)))
            { cb.Items.Add(model.ToString()); }
            cb.SelectedValue = n.Model.ToString();
            cb.SelectionChanged += Cb_SelectionChanged;
            cm.Items.Add(cb);
        }

        //change the model for all selected neurons if this one is in the selection
        private static void Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            ContextMenu cm = cb.Parent as ContextMenu;
            int neuronID = (int)cm.GetValue(NeuronIDProperty);
            Neuron.modelType nm = (Neuron.modelType)System.Enum.Parse(typeof(Neuron.modelType), cb.SelectedItem.ToString());
            Neuron n = MainWindow.theNeuronArray.neuronArray[neuronID];

            n.Model = nm;
            bool neuronInSelection = false;
            foreach (NeuronSelectionRectangle sr in theNeuronArrayView.theSelection.selectedRectangles)
            {
                if (sr.NeuronIsInSelection(neuronID))
                {
                    neuronInSelection = true;
                    break;
                }
            }
            if (neuronInSelection)
            {
                theNeuronArrayView.theSelection.EnumSelectedNeurons();
                for (Neuron n1 = theNeuronArrayView.theSelection.GetSelectedNeuron(); n1 != null; n1 = theNeuronArrayView.theSelection.GetSelectedNeuron())
                    n1.Model = nm;
                MainWindow.Update();
            }
        }

        private static void Mi_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            //find out which neuron this context menu is from
            ContextMenu cm = mi.Parent as ContextMenu;
            if (cm == null)
            {
                MenuItem mi2 = mi.Parent as MenuItem;
                cm = mi2.Parent as ContextMenu;
            }
            int i = (int)cm.GetValue(NeuronIDProperty);
            Neuron n = MainWindow.theNeuronArray.neuronArray[i];
            if ((string)mi.Header == "Always Fire")
            {
                if (n.FindSynapse(i) == null)
                    n.AddSynapse(i, 1, MainWindow.theNeuronArray);
                else
                    n.DeleteSynapse(i);
            }
            if ((string)mi.Header == "Paste Here")
            {
                theNeuronArrayView.targetNeuronIndex = i;
                theNeuronArrayView.PasteNeurons();
                theNeuronArrayView.targetNeuronIndex = -1;
            }
            if ((string)mi.Header == "Move Here")
            {
                theNeuronArrayView.targetNeuronIndex = i;
                theNeuronArrayView.MoveNeurons();
            }
            if ((string)mi.Header == "Copy Selection")
            {
                theNeuronArrayView.CopyNeurons();
            }
            if ((string)mi.Header == "Connect to Here")
            {
                theNeuronArrayView.targetNeuronIndex = i;
                theNeuronArrayView.ConnectToHere();
            }
            if ((string)mi.Header == "Connect from Here")
            {
                theNeuronArrayView.targetNeuronIndex = i;
                theNeuronArrayView.ConnectFromHere();
            }
            if ((string)mi.Header == "Select as Target")
            {
                theNeuronArrayView.targetNeuronIndex = i;
            }
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
            if (cm == null) return;
            int i = (int) cm.GetValue(NeuronIDProperty);
            Neuron n = MainWindow.theNeuronArray.neuronArray[i];
            n.Label = tb.Text;
        }


        //helper to make rainbow colors
        // Map a value to a rainbow color.
        private static Color MapRainbowColor(
                float value, float red_value, float blue_value)
        {
            // Convert into a value between 0 and neuronDisplaySize23.
            int int_value = (int)(1023 * (value - red_value) /
                (blue_value - red_value));

            // Map different color bands.
            if (int_value < 256)
            {
                // Red to yellow. (255, 0, 0) to (255, 255, 0).
                return Color.FromRgb(255, (byte)int_value, 0);
            }
            else if (int_value < 512)
            {
                // Yellow to green. (255, 255, 0) to (0, 255, 0).
                int_value -= 256;
                return Color.FromRgb((byte)(255 - int_value), 255, 0);
            }
            else if (int_value < 768)
            {
                // Green to aqua. (0, 255, 0) to (0, 255, 255).
                int_value -= 512;
                return Color.FromRgb(0, 255, (byte)int_value);
            }
            else
            {
                // Aqua to blue. (0, 255, 255) to (0, 0, 255).
                int_value -= 768;
                return Color.FromRgb(0, (byte)(255 - int_value), 255);
            }
        }
    }
}
