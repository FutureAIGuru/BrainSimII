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

namespace BrainSimulator
{
    public class NeuronView : DependencyObject
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
                l = new Label();
                l.Content = n.Label;
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
                //theCanvas.Children.Add(l);
            }
            return r;
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
            if (!n.inUse && n.Model == Neuron.modelType.Std)
                s1.Opacity = .50;
            return s1;
        }

        private static void R_MouseLeave(object sender, MouseEventArgs e)
        {
            if (theCanvas.Cursor != Cursors.Hand && !theNeuronArrayView.dragging && e.LeftButton != MouseButtonState.Pressed)
                theCanvas.Cursor = Cursors.Cross;
        }

        private static void R_MouseEnter(object sender, MouseEventArgs e)
        {
            if (theCanvas.Cursor != Cursors.Hand && !theNeuronArrayView.dragging && e.LeftButton != MouseButtonState.Pressed)
                theCanvas.Cursor = Cursors.UpArrow;
        }

        //for UI performance, the context menu is not attached to a neuron when the neuron is created but
        //is built on the fly when a neuron is right-clicked.  Hence the public-static
        static bool cmCancelled = false;
        public static void CreateContextMenu(int i, Neuron n, ContextMenu cm)
        {
            n = MainWindow.theNeuronArray.AddSynapses(n);
            cm.SetValue(NeuronIDProperty, n.Id);
            cm.Closed += Cm_Closed;
            cmCancelled = false;
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
            sp.Children.Add(new Label { Content = "Charge: ", Padding = new Thickness(0) });
            if (n.Model != Neuron.modelType.FloatValue)
                sp.Children.Add(new TextBox { Text = n.LastCharge.ToString("f2"), Width = 60, Name = "CurrentCharge", VerticalAlignment = VerticalAlignment.Center });
            else
                sp.Children.Add(new TextBox { Text = n.CurrentCharge.ToString("f2"), Width = 60, Name = "CurrentCharge", VerticalAlignment = VerticalAlignment.Center });
            cm.Items.Add(sp);
            if (n.Model == Neuron.modelType.LIF || n.model == Neuron.modelType.Random)
            {
                sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
                sp.Children.Add(new Label { Content = "Leak Rate: ", Padding = new Thickness(0) });
                sp.Children.Add(new TextBox { Text = n.LeakRate.ToString("f4"), Width = 60, Name = "LeakRate", VerticalAlignment = VerticalAlignment.Center });
                cm.Items.Add(sp);
            }

            MenuItem mi = new MenuItem();
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
            sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
            sp.Children.Add(new Label { Content = "Label: ", Padding = new Thickness(0) });
            sp.Children.Add(new TextBox { Text = n.Label, Width = 150, Name = "Label", VerticalAlignment = VerticalAlignment.Center });
            cm.Items.Add(sp);

            sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
            sp.Children.Add(new Label { Content = "Model: ", Padding = new Thickness(0) });
            ComboBox cb = new ComboBox()
            { Width = 100 };
            for (int index = 0; index < Enum.GetValues(typeof(Neuron.modelType)).Length; index++)
            {
                Neuron.modelType model = (Neuron.modelType)index;
                cb.Items.Add(new ListBoxItem()
                {
                    Content = model.ToString(),
                    ToolTip = Neuron.modelToolTip[index],
                    Width = 100,
                });
            }
            cb.SelectedIndex = (int)n.Model;
            cb.SelectionChanged += Cb_SelectionChanged;
            sp.Children.Add(cb);
            cm.Items.Add(sp);
            CheckBox cbHistory = new CheckBox
            {
                IsChecked = FiringHistory.NeuronIsInFiringHistory(n.id),
                Content = "Record Firing History",
                Name = "History",
            };
            cbHistory.Checked += CbHistory_Checked;
            cbHistory.Unchecked += CbHistory_Checked;
            cm.Items.Add(cbHistory);

            mi = new MenuItem();
            mi.Header = "Synapses";
            foreach (Synapse s in n.Synapses)
            {
                string targetLabel = MainWindow.theNeuronArray.GetNeuron(s.targetNeuron).Label;
                MenuItem synapseMenuItem = new MenuItem() { Header = s.targetNeuron.ToString().PadLeft(8) + s.Weight.ToString("F3").PadLeft(9) + " " + targetLabel, FontFamily = new FontFamily("Courier New") };
                synapseMenuItem.Click += Mi_Click;
                synapseMenuItem.PreviewMouseRightButtonDown += SynapseMenuItem_PreviewMouseRightButtonDown;
                synapseMenuItem.ToolTip = "L-click -> target neuron, R-Click -> edit synapse";
                mi.Items.Add(synapseMenuItem);
            }
            cm.Items.Add(mi);

            mi = new MenuItem();
            mi.Header = "Synapses In";
            foreach (Synapse s in n.SynapsesFrom)
            {
                string targetLabel = MainWindow.theNeuronArray.GetNeuron(s.targetNeuron).Label;
                MenuItem synapseMenuItem = new MenuItem() { Header = s.targetNeuron.ToString().PadLeft(8) + " " + s.Weight.ToString("F3").PadLeft(9) + " " + targetLabel, FontFamily = new FontFamily("Courier New") };
                synapseMenuItem.Click += Mi_Click;
                synapseMenuItem.PreviewMouseRightButtonDown += SynapseFromMenuItem_PreviewMouseRightButtonDown1;
                synapseMenuItem.ToolTip = "L-click -> source neuron, R-Click -> edit synapse";
                mi.Items.Add(synapseMenuItem); ;
            }
            cm.Items.Add(mi);
        }

        private static void CbHistory_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            ContextMenu cm = cb.Parent as ContextMenu;
            cm.IsOpen = false;
        }

        private static void Cm_Closed(object sender, RoutedEventArgs e)
        {
            if ((Keyboard.GetKeyStates(Key.Escape) & KeyStates.Down) > 0)
            {
                return;
            }
            if (cmCancelled)
            {
                cmCancelled = false;
                return;
            }
            if (sender is ContextMenu cm)
            {

                int neuronID = (int)cm.GetValue(NeuronIDProperty);
                Neuron n = MainWindow.theNeuronArray.GetNeuron(neuronID);
                Control cc = Utils.FindByName(cm, "Label");
                if (cc is TextBox tb)
                    n.Label = tb.Text;
                cc = Utils.FindByName(cm, "CurrentCharge");
                if (cc is TextBox tb1)
                {
                    float.TryParse(tb1.Text, out float newCharge);
                    n.SetValue(newCharge);
                }
                cc = Utils.FindByName(cm, "LeakRate");
                if (cc is TextBox tb2)
                {
                    float.TryParse(tb2.Text, out float leakRate);
                    if (n.LeakRate != leakRate)
                    {
                        n.LeakRate = leakRate;
                        SetModelAndLeakrate(n);
                    }
                    n.LeakRate = leakRate;
                }
                cc = Utils.FindByName(cm, "History");
                if (cc is CheckBox cb1)
                {
                    bool KeepHistory = (bool)cb1.IsChecked;
                    if (!KeepHistory)
                    {
                        FiringHistory.RemoveNeuronFromHistoryWindow(n.Id);
                        if (FiringHistory.history.Count == 0 && MainWindow.fwWindow != null && MainWindow.fwWindow.IsVisible)
                        {
                            MainWindow.fwWindow.Close();
                            MainWindow.fwWindow = null;
                        }
                    }
                    else  //make sure a window is open
                    {
                        FiringHistory.AddNeuronToHistoryWindow(n.id);
                        OpenHistoryWindow();
                    }
                    //if there is a selection, set all the keepHistory values to match
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
                        {
                            if (KeepHistory)
                                FiringHistory.AddNeuronToHistoryWindow(n1.id);
                            else
                                FiringHistory.RemoveNeuronFromHistoryWindow(n1.id);
                        }
                        //n1.KeepHistory = (bool)cb1.IsChecked;
                    }
                }
            }
            MainWindow.Update();
        }

        public static void OpenHistoryWindow()
        {
            if (MainWindow.fwWindow == null || !MainWindow.fwWindow.IsVisible)
                MainWindow.fwWindow = new FiringHistoryWindow();
            MainWindow.fwWindow.Show();
        }

        //change the model for all selected neurons if this one is in the selection
        private static void Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            StackPanel sp = cb.Parent as StackPanel;
            ContextMenu cm = sp.Parent as ContextMenu;
            int neuronID = (int)cm.GetValue(NeuronIDProperty);
            ListBoxItem lbi = (ListBoxItem)cb.SelectedItem;
            Neuron.modelType nm = (Neuron.modelType)System.Enum.Parse(typeof(Neuron.modelType), lbi.Content.ToString());
            Neuron n = MainWindow.theNeuronArray.GetNeuron(neuronID);
            n.Model = nm;
            SetModelAndLeakrate(n);
            cm.IsOpen = false;
        }

        static Random rand = new Random();
        private static void SetModelAndLeakrate(Neuron n)
        {
            bool neuronInSelection = false;
            foreach (NeuronSelectionRectangle sr in theNeuronArrayView.theSelection.selectedRectangles)
            {
                if (sr.NeuronIsInSelection(n.Id))
                {
                    neuronInSelection = true;
                    break;
                }
            }
            if (neuronInSelection)
            {
                theNeuronArrayView.theSelection.EnumSelectedNeurons();
                for (Neuron n1 = theNeuronArrayView.theSelection.GetSelectedNeuron(); n1 != null; n1 = theNeuronArrayView.theSelection.GetSelectedNeuron())
                {
                    if (n.Model == Neuron.modelType.Random)
                    {
                        n1.currentCharge = (float)rand.NextDouble();
                    }
                    n1.Model = n.Model;
                    n1.LeakRate = n.LeakRate;
                }
                MainWindow.Update();
            }
        }

        private static void SynapseFromMenuItem_PreviewMouseRightButtonDown1(object sender, MouseButtonEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            MenuItem mi2 = mi.Parent as MenuItem;
            ContextMenu cm = mi2.Parent as ContextMenu;
            int targetID = (int)cm.GetValue(NeuronIDProperty);
            ContextMenu newCm = new ContextMenu();
            int.TryParse(mi.Header.ToString().Substring(0, 8), out int sourceID);
            Neuron n = MainWindow.theNeuronArray.GetNeuron(sourceID);
            Synapse s = n.FindSynapse(targetID);
            if (s != null)
            {
                SynapseView.CreateContextMenu(sourceID, s, newCm);
                newCm.IsOpen = true;
                e.Handled = true;
            }
        }

        private static void SynapseMenuItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            MenuItem mi2 = mi.Parent as MenuItem;
            ContextMenu cm = mi2.Parent as ContextMenu;
            int sourceID = (int)cm.GetValue(NeuronIDProperty);
            ContextMenu newCm = new ContextMenu();
            int.TryParse(mi.Header.ToString().Substring(0, 8), out int targetID);
            Neuron n = MainWindow.theNeuronArray.GetNeuron(sourceID);
            Synapse s = n.FindSynapse(targetID);
            if (s != null)
            {
                SynapseView.CreateContextMenu(sourceID, s, newCm);
                newCm.IsOpen = true;
                e.Handled = true;
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
                if (mi2.Header.ToString().IndexOf("Synapses") == 0)
                {
                    int.TryParse(mi.Header.ToString().Substring(0, 8), out int newID);
                    Neuron n1 = MainWindow.theNeuronArray.GetNeuron(newID);
                    NeuronView.CreateContextMenu(n1.id, n1, new ContextMenu() { IsOpen = true, });
                    return;
                }
                cm = mi2.Parent as ContextMenu;
            }
            int i = (int)cm.GetValue(NeuronIDProperty);
            Neuron n = MainWindow.theNeuronArray.GetNeuron(i);
            if ((string)mi.Header == "Always Fire")
            {
                if (n.FindSynapse(i) == null)
                    n.AddSynapse(i, 1, MainWindow.theNeuronArray, true);
                else
                    n.DeleteSynapse(i);
            }
            if ((string)mi.Header == "Paste Here")
            {
                theNeuronArrayView.targetNeuronIndex = i;
                theNeuronArrayView.PasteNeurons();
                theNeuronArrayView.targetNeuronIndex = -1;
                cmCancelled = true;
            }
            if ((string)mi.Header == "Move Here")
            {
                theNeuronArrayView.targetNeuronIndex = i;
                theNeuronArrayView.MoveNeurons();
                cmCancelled = true;
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


        private static void Tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            //find out which neuron this context menu is from
            ContextMenu cm = tb.Parent as ContextMenu;
            if (cm == null) return;
            int i = (int)cm.GetValue(NeuronIDProperty);
            Neuron n = MainWindow.theNeuronArray.GetNeuron(i);
            n.Label = tb.Text;
        }


    }
}
