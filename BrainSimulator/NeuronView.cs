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
    public static class NeuronView
    {
        public static DisplayParams dp;
        public static NeuronArrayView.MouseMode theMouseMode = NeuronArrayView.MouseMode.pan;
        public static Canvas theCanvas;     //reflection of canvas in neuronarrayview
        static NeuronArrayView theNeuronArrayView;

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
            if (n.Range != 2 && value > .99)
                c = Colors.Orange;
            else if (n.Range != 2 && value != -1)
                c = MapRainbowColor(value, 1, 0);
            else if (n.Range == 2)
                c = Utils.FromArgb((int)n.LastChargeInt);
            SolidColorBrush s1 = new SolidColorBrush(c);
            //   if (n.Label != "" || !n.InUse()) s1.Opacity = .50;
            if (!n.InUse()) s1.Opacity = .50;

            Shape r = null;
            if (dp.NeuronDisplaySize > 10)
            {
                r = new Ellipse();
                r.Width = dp.NeuronDisplaySize * .8;
                r.Height = dp.NeuronDisplaySize * .8;
            }
            else
            {
                r = new Rectangle();
                r.Width = dp.NeuronDisplaySize;
                r.Height = dp.NeuronDisplaySize;
            }
            r.Fill = s1;
            if (dp.NeuronDisplaySize > 15)
            {
                r.Stroke = Brushes.Black;
                r.StrokeThickness = 1;
            }

            //build the context menu
            ContextMenu cm = new ContextMenu();
            if (dp.NeuronDisplaySize > 25)// && theMouseMode == NeuronArrayView.MouseMode.neuron)
            {
                //this stashes the neuron number in the first (hidden) entry of the menu so if you click
                //on the context menu and an Update() has removed the existing entry, you can still do the right thing
                //Label l = new Label();l.Content = n.Id +":"+n.CurrentCharge + "," + n.LastCharge;
                //cm.Items.Add(l);
                MenuItem mi = new MenuItem();
                mi.Header = i.ToString();
                mi.Visibility = Visibility.Collapsed;
                cm.Items.Add(mi);
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
                mi.Header = "Copy Selection";
                mi.Click += Mi_Click;
                cm.Items.Add(mi);
                mi = new MenuItem();
                mi.Header = "Move Here";
                mi.Click += Mi_Click;
                cm.Items.Add(mi);
                mi = new MenuItem();
                mi.Header = "Connect to Here";
                mi.Click += Mi_Click;
                cm.Items.Add(mi);
                mi = new MenuItem();
                mi.Header = "Connect from Here";
                mi.Click += Mi_Click;
                cm.Items.Add(mi);
                mi = new MenuItem();
                mi.Header = "Select as Target";
                mi.Click += Mi_Click;
                cm.Items.Add(mi);
                mi = new MenuItem();
                mi.Header = "Label:";
                mi.IsEnabled = false;
                cm.Items.Add(mi);
                TextBox tb = new TextBox();
                tb.Text = n.Label;
                tb.Width = 200;
                tb.TextChanged += Tb_TextChanged;
                cm.Items.Add(tb);
                cm.Closed += Cm_Closed;
                r.ContextMenu = cm;
            }
            r.MouseDown += theNeuronArrayView.theCanvas_MouseDown;
            r.MouseUp += theNeuronArrayView.theCanvas_MouseUp;
            r.MouseWheel += theNeuronArrayView.theCanvas_MouseWheel;

            Canvas.SetLeft(r, p.X + dp.NeuronDisplaySize * .1);
            Canvas.SetTop(r, p.Y + dp.NeuronDisplaySize * .1);


            if (n.Label != "")
            {
                Label l = new Label();
                l.Content = n.Label;
                l.FontSize = dp.NeuronDisplaySize * .3;
                l.Foreground = Brushes.White;
                Canvas.SetLeft(l, p.X + dp.NeuronDisplaySize * .1);
                Canvas.SetTop(l, p.Y + dp.NeuronDisplaySize * .1);
                if (dp.NeuronDisplaySize > 8)
                    l.ContextMenu = cm;
                Canvas.SetZIndex(l, 100);
                theCanvas.Children.Add(l);
                //return l;
            }
            return r;
        }
        private static void Mi_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            //find out which neuron this context menu is from
            ContextMenu cm = (ContextMenu)mi.Parent;
            int i = -1;
            int.TryParse(((MenuItem)cm.Items[0]).Header.ToString(), out i);
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
            TextBox tb = (TextBox)sender;
            //find out which neuron this context menu is from
            ContextMenu cm = (ContextMenu)tb.Parent;
            int i = -1;
            int.TryParse(((MenuItem)cm.Items[0]).Header.ToString(), out i);
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
