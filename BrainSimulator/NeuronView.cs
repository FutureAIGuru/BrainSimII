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


            if (n.Label != "" || n.model != Neuron.modelType.IF
                )
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
                l.Content = GetNeuronLabel(n);
            }
            return r;
        }

        public static string GetNeuronLabel(Neuron n)
        {
            string retVal = "";
            if (!dp.ShowNeuronLabels()) return retVal;

            retVal = n.label;
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
            if ((n.leakRate < 0) || float.IsNegativeInfinity(1.0f/n.leakRate))
                s1 = new SolidColorBrush(Colors.LightSalmon);
            return s1;
        }

        private static void R_MouseLeave(object sender, MouseEventArgs e)
        {
            //Debug.WriteLine("NeuronView MouseLeave");
            if (theCanvas.Cursor != Cursors.Hand && !theNeuronArrayView.dragging && e.LeftButton != MouseButtonState.Pressed)
                theCanvas.Cursor = Cursors.Cross;
        }

        private static void R_MouseEnter(object sender, MouseEventArgs e)
        {
            //Debug.WriteLine("NeuronView MouseEnter");
            if (theCanvas.Cursor != Cursors.Hand && !theNeuronArrayView.dragging && e.LeftButton != MouseButtonState.Pressed)
                theCanvas.Cursor = Cursors.UpArrow;
        }

        //for UI performance, the context menu is not attached to a neuron when the neuron is created but
        //is built on the fly when a neuron is right-clicked.  Hence the public-static
        static bool cmCancelled = false;
        static bool chargeChanged = false;
        static bool labelChanged = false;
        static bool modelChanged = false;
        static bool enabledChanged = false;
        static bool historyChanged = false;
        static bool synapsesChanged = false;
        static bool leakRateChanged = false;
        static bool axonDelayChanged = false;
        public static void CreateContextMenu(int i, Neuron n, ContextMenu cm)
        {
            cmCancelled = false;

            labelChanged = false;
            modelChanged = false;
            enabledChanged = false;
            historyChanged = false;
            synapsesChanged = false;
            chargeChanged = false;
            leakRateChanged = false;
            axonDelayChanged = false;

            n = MainWindow.theNeuronArray.AddSynapses(n);
            cm.SetValue(NeuronIDProperty, n.Id);
            cm.Closed += Cm_Closed;
            cm.Opened += Cm_Opened;
            cm.PreviewKeyDown += Cm_PreviewKeyDown;
            cmCancelled = false;

            //The neuron label
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
            sp.Children.Add(new Label { Content = "ID: " + n.id + "   Label: ", VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(0) });
            TextBox tb = Utils.ContextMenuTextBox(n.Label, "Label", 150);
            tb.TextChanged += Tb_TextChanged;
            sp.Children.Add(tb);
            sp.Children.Add(new Label { Content = "Warning:\rDuplicate Label", FontSize = 8, Name = "DupWarn", Visibility = Visibility.Hidden });
            cm.Items.Add(sp);

            //The neuron model
            sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
            sp.Children.Add(new Label { Content = "Model: ", Padding = new Thickness(0) });
            ComboBox cb = new ComboBox()
            { Width = 100, Name = "Model" };
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

            cm.Items.Add(new Separator());

            MenuItem mi = new MenuItem();
            CheckBox cbShowSynapses = new CheckBox
            {
                IsChecked = (n.leakRate > 0) || float.IsPositiveInfinity(1.0f / n.leakRate),
                Content = "Enabled",
                Name = "Enabled",
            };
            cbShowSynapses.Checked += CbCheckedChanged;
            cbShowSynapses.Unchecked += CbCheckedChanged;
            cm.Items.Add(cbShowSynapses);

            cbShowSynapses = new CheckBox
            {
                IsChecked = MainWindow.arrayView.IsShowingSnapses(n.id),
                Content = "Show Synapses",
                Name = "Synapses",
            };
            cbShowSynapses.Checked += CbCheckedChanged;
            cbShowSynapses.Unchecked += CbCheckedChanged;
            cm.Items.Add(cbShowSynapses);

            mi = new MenuItem();
            CheckBox cbHistory = new CheckBox
            {
                IsChecked = FiringHistory.NeuronIsInFiringHistory(n.id),
                Content = "Record Firing History",
                Name = "History",
            };
            cbHistory.Checked += CbCheckedChanged;
            cbHistory.Unchecked += CbCheckedChanged;
            cm.Items.Add(cbHistory);

            mi = new MenuItem();
            mi.Header = "Synapses Out";
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

            mi = new MenuItem { Header = "Paste Here" };
            if (MainWindow.myClipBoard == null) mi.IsEnabled = false;
            mi.Click += Mi_Click;
            cm.Items.Add(mi);
            mi = new MenuItem { Header = "Move Here" };
            if (MainWindow.arrayView.theSelection.selectedRectangles.Count == 0) mi.IsEnabled = false;
            mi.Click += Mi_Click;
            cm.Items.Add(mi);


            mi = new MenuItem();
            mi.Header = "Connect Multiple Synapses";
            mi.Click += Mi_Click;
            mi.Items.Add(new MenuItem() { Header = "From Selection to Here" });
            ((MenuItem)mi.Items[mi.Items.Count - 1]).Click += Mi_Click;
            mi.Items.Add(new MenuItem() { Header = "From Here to Selection" });
            ((MenuItem)mi.Items[mi.Items.Count - 1]).Click += Mi_Click;
            mi.Items.Add(new MenuItem() { Header = "Mutual Suppression" });
            ((MenuItem)mi.Items[mi.Items.Count - 1]).Click += Mi_Click;
            cm.Items.Add(mi);


            sp = new StackPanel { Orientation = Orientation.Horizontal };
            Button b0 = new Button { Content = "OK", Width = 100, Height = 25, Margin = new Thickness(10) };
            b0.Click += B0_Click;
            sp.Children.Add(b0);
            b0 = new Button { Content = "Cancel", Width = 100, Height = 25, Margin = new Thickness(10) };
            b0.Click += B0_Click;
            sp.Children.Add(b0);

            cm.Items.Add(sp);

            SetCustomCMItems(cm, n, n.model);

        }

        private static void B0_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                if (b.Parent is StackPanel sp)
                {
                    if (sp.Parent is ContextMenu cm)
                    {
                        if ((string)b.Content == "Cancel")
                            cmCancelled = true;
                        Cm_Closed(cm, e);
                    }
                }
            }
        }

        //This creates or updates the portion of the context menu content which depends on the model type
        private static void SetCustomCMItems(ContextMenu cm, Neuron n, Neuron.modelType newModel)
        {
            while (cm.Items[2].GetType().Name != "Separator")
                cm.Items.RemoveAt(2);

            //The charge value formatted based on the model
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
            sp.Children.Add(new Label { Content = "Charge: ", Padding = new Thickness(0) });
            if (newModel == Neuron.modelType.Color)
            {
                TextBox tb1 = Utils.ContextMenuTextBox(n.LastChargeInt.ToString("X8"), "CurrentCharge", 60);
                tb1.TextChanged += Tb1_ChargeTextChanged;
                sp.Children.Add(tb1);
            }
            else
            {
                string format = "f2";
                if (n.Model == Neuron.modelType.FloatValue) format = "f4";
                TextBox tb1 = Utils.ContextMenuTextBox(n.LastCharge.ToString(format), "CurrentCharge", 60);
                tb1.TextChanged += Tb1_ChargeTextChanged;
                sp.Children.Add(tb1);
            }
            cm.Items.Insert(2, sp);

            if (newModel == Neuron.modelType.LIF)
            {
                sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
                sp.Children.Add(new Label { Content = "Leak Rate: ", Padding = new Thickness(0) });
                TextBox tb0 = Utils.ContextMenuTextBox(Math.Abs(n.LeakRate).ToString("f2"), "LeakRate", 60);
                tb0.TextChanged += Tb0_LeakRateChanged;
                sp.Children.Add(tb0);
                cm.Items.Insert(3, sp);

                sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
                sp.Children.Add(new Label { Content = "Axon Delay: ", Padding = new Thickness(0) });
                TextBox tb1 = Utils.ContextMenuTextBox(n.axonDelay.ToString(), "AxonDelay", 60);
                tb1.TextChanged += Tb1_AxonDelayChanged;
                sp.Children.Add(tb1);
                cm.Items.Insert(4, sp);
            }
            else if (newModel == Neuron.modelType.Always)
            {
                sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
                sp.Children.Add(new Label { Content = "Delay: ", Padding = new Thickness(0) });
                TextBox tb0 = Utils.ContextMenuTextBox(n.axonDelay.ToString(), "AxonDelay", 60);
                tb0.TextChanged += Tb1_AxonDelayChanged;
                sp.Children.Add(tb0);
                cm.Items.Insert(3, sp);
            }
            else if (newModel == Neuron.modelType.Random)
            {
                sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
                sp.Children.Add(new Label { Content = "Mean: ", Padding = new Thickness(0) });
                TextBox tb1 = Utils.ContextMenuTextBox(n.axonDelay.ToString(), "AxonDelay", 60);
                tb1.TextChanged += Tb1_AxonDelayChanged;
                sp.Children.Add(tb1);
                cm.Items.Insert(3, sp);

                sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
                sp.Children.Add(new Label { Content = "Std Dev: ", Padding = new Thickness(0) });
                TextBox tb0 = Utils.ContextMenuTextBox(Math.Abs(n.LeakRate).ToString("f2"), "LeakRate", 60);
                tb0.TextChanged += Tb0_LeakRateChanged;
                sp.Children.Add(tb0);
                cm.Items.Insert(4, sp);
            }
            else if (newModel == Neuron.modelType.Burst)
            {
                if (n.axonDelay < 1) n.axonDelay = 5;
                sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
                sp.Children.Add(new Label { Content = "Count: ", Padding = new Thickness(0) });
                TextBox tb1 = Utils.ContextMenuTextBox(n.axonDelay.ToString(), "AxonDelay", 60);
                tb1.TextChanged += Tb1_AxonDelayChanged;
                sp.Children.Add(tb1);
                cm.Items.Insert(3, sp);

                if (n.leakRate < 1) n.leakRate = 1;
                sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
                sp.Children.Add(new Label { Content = "Rate: ", Padding = new Thickness(0) });
                TextBox tb0 = Utils.ContextMenuTextBox(Math.Abs(n.LeakRate).ToString("f0"), "LeakRate", 60);
                tb0.TextChanged += Tb0_LeakRateChanged;
                sp.Children.Add(tb0);
                cm.Items.Insert(4, sp);
            }

        }

        private static void Tb1_AxonDelayChanged(object sender, TextChangedEventArgs e)
        {
            axonDelayChanged = true;
        }

        private static void Tb0_LeakRateChanged(object sender, TextChangedEventArgs e)
        {
            leakRateChanged = true;
        }

        private static void Cm_Opened(object sender, RoutedEventArgs e)
        {
            //when the context menu opens, focus on the label and position text cursor to end
            if (sender is ContextMenu cm)
            {
                Control cc = Utils.FindByName(cm, "Label");
                if (cc is TextBox tb)
                {
                    tb.Focus();
                    tb.Select(tb.Text.Length, 0);
                }
            }
        }

        private static void Cm_Closed(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu cm)
            {
                cm.IsOpen = false;
                if (cmCancelled)
                {
                    MainWindow.Update();
                    return;
                }
                MainWindow.theNeuronArray.SetUndoPoint();
                int neuronID = (int)cm.GetValue(NeuronIDProperty);
                Neuron n = MainWindow.theNeuronArray.GetNeuron(neuronID);
                n.AddUndoInfo();

                Control cc = Utils.FindByName(cm, "Label");
                if (cc is TextBox tb)
                {
                    string newLabel = tb.Text;
                    if (int.TryParse(newLabel, out int dummy))
                        newLabel = "_" + newLabel;
                    if (labelChanged)
                    {
                        MainWindow.theNeuronArray.SetUndoPoint();
                        n.Label = newLabel;
                        SetValueInSelectedNeurons(n, "label");
                    }
                }
                cc = Utils.FindByName(cm, "Model");
                if (cc is ComboBox cb0)
                {
                    ListBoxItem lbi = (ListBoxItem)cb0.SelectedItem;
                    Neuron.modelType nm = (Neuron.modelType)System.Enum.Parse(typeof(Neuron.modelType), lbi.Content.ToString());
                    if (modelChanged)
                    {
                        n.model = nm;
                        SetValueInSelectedNeurons(n, "model");
                    }
                }
                cc = Utils.FindByName(cm, "CurrentCharge");
                if (cc is TextBox tb1)
                {
                    if (n.model == Neuron.modelType.Color)
                    {
                        try
                        {
                            uint newCharge = Convert.ToUInt32(tb1.Text, 16);
                            if (chargeChanged)
                            {
                                n.SetValueInt((int)newCharge);
                                n.lastCharge = newCharge;
                                SetValueInSelectedNeurons(n, "currentCharge");
                            }
                        }
                        catch { };
                    }
                    else
                    {
                        float.TryParse(tb1.Text, out float newCharge);
                        if (chargeChanged)
                        {
                            n.SetValue(newCharge);
                            n.lastCharge = newCharge;
                            SetValueInSelectedNeurons(n, "currentCharge");
                        }
                    }
                }
                cc = Utils.FindByName(cm, "LeakRate");
                if (cc is TextBox tb2)
                {
                    float.TryParse(tb2.Text, out float leakRate);
                    if (leakRateChanged)
                    {
                        n.LeakRate = leakRate;
                        SetValueInSelectedNeurons(n, "leakRate");
                    }
                }
                else
                    n.leakRate = 0;
                cc = Utils.FindByName(cm, "AxonDelay");
                if (cc is TextBox tb3)
                {
                    int.TryParse(tb3.Text, out int axonDelay);
                    if (axonDelayChanged)
                    {
                        n.axonDelay = axonDelay;
                        SetValueInSelectedNeurons(n, "axonDelay");
                    }
                }
                cc = Utils.FindByName(cm, "Synapses");
                if (cc is CheckBox cb2)
                {
                    if (synapsesChanged)
                    {
                        if (cb2.IsChecked == true)
                        {
                            MainWindow.arrayView.AddShowSynapses(n.id);
                        }
                        else
                            MainWindow.arrayView.RemoveShowSynapses(n.id);
                        SetValueInSelectedNeurons(n, "synapses");
                    }
                }

                cc = Utils.FindByName(cm, "Enabled");
                if (cc is CheckBox cb1)
                {
                    if (enabledChanged)
                    {
                        if (cb1.IsChecked == true)
                            n.leakRate = Math.Abs(n.leakRate);
                        else
                            n.leakRate = Math.Abs(n.leakRate) * -1.0f;

                        SetValueInSelectedNeurons(n, "enable");
                    }
                }

                cc = Utils.FindByName(cm, "History");
                if (cc is CheckBox cb3)
                {
                    if (historyChanged)
                    {
                        if (cb3.IsChecked == true)
                        {
                            FiringHistory.AddNeuronToHistoryWindow(n.id);
                            OpenHistoryWindow();
                        }
                        else
                            FiringHistory.RemoveNeuronFromHistoryWindow(n.id);
                        SetValueInSelectedNeurons(n, "history");
                    }
                }
                n.Update();
            }
            MainWindow.Update();
        }


        private static void CbCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                if (cb.Name == "Enabled")
                    enabledChanged = true;
                if (cb.Name == "History")
                    historyChanged = true;
                if (cb.Name == "Synapses")
                    synapsesChanged = true;
            }
        }
        private static void Tb1_ChargeTextChanged(object sender, TextChangedEventArgs e)
        {
            chargeChanged = true;
        }



        //this checks the name against existing names and warns on duplicates
        private static void Tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            labelChanged = true;
            if (sender is TextBox tb)
            {
                string neuronLabel = tb.Text;
                Neuron n = MainWindow.theNeuronArray.GetNeuron(neuronLabel);
                if (n == null || neuronLabel == "")
                {
                    tb.Background = new SolidColorBrush(Colors.White);
                    if (tb.Parent is StackPanel sp)
                    {
                        ((Label)sp.Children[2]).Visibility = Visibility.Hidden;
                    }
                }
                else
                {
                    tb.Background = new SolidColorBrush(Colors.Pink);
                    if (tb.Parent is StackPanel sp)
                    {
                        ((Label)sp.Children[2]).Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private static void Cm_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                cmCancelled = true;
        }

        public static void OpenHistoryWindow()
        {
            if (MainWindow.fwWindow == null || !MainWindow.fwWindow.IsVisible)
                MainWindow.fwWindow = new FiringHistoryWindow();
            MainWindow.fwWindow.Show();
        }

        //change the model and update the context menu
        private static void Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            modelChanged = true;
            ComboBox cb = sender as ComboBox;
            StackPanel sp = cb.Parent as StackPanel;
            ContextMenu cm = sp.Parent as ContextMenu;
            int neuronID = (int)cm.GetValue(NeuronIDProperty);
            ListBoxItem lbi = (ListBoxItem)cb.SelectedItem;
            Neuron.modelType nm = (Neuron.modelType)System.Enum.Parse(typeof(Neuron.modelType), lbi.Content.ToString());

            Neuron n = MainWindow.theNeuronArray.GetNeuron(neuronID);
            SetCustomCMItems(cm, n, nm);
        }
        private static void SetValueInSelectedNeurons(Neuron n, string property)
        {
            bool neuronInSelection = NeuronInSelection(n.id);
            if (neuronInSelection)
            {
                List<int> theNeurons = theNeuronArrayView.theSelection.EnumSelectedNeurons();
                //special case for label because they are auto-incremented, 
                //clear all the labels first to avoid collisions
                if (property == "label")
                {
                    for (int i = 0; i < theNeurons.Count; i++)
                    {
                        Neuron n1 = MainWindow.theNeuronArray.GetNeuron(theNeurons[i]);
                        if (n1.id != n.id)
                        {
                            n1.label = "";
                            n1.Update();
                        }
                    }
                }
                for (int i = 0; i < theNeurons.Count; i++)
                {
                    Neuron n1 = MainWindow.theNeuronArray.GetNeuron(theNeurons[i]);
                    n1.AddUndoInfo();
                    switch (property)
                    {
                        case "currentCharge":
                            if (n.model == Neuron.modelType.Color)
                                n1.SetValueInt(n.LastChargeInt);
                            else
                            {
                                n1.currentCharge = n.currentCharge;
                                n1.lastCharge = n.currentCharge;
                            }
                            break;
                        case "leakRate": n1.leakRate = n.leakRate; break;
                        case "axonDelay": n1.axonDelay = n.axonDelay; break;
                        case "model": n1.model = n.model; break;
                        case "enable": n1.leakRate = n.leakRate; break;
                        case "history":
                            if (FiringHistory.NeuronIsInFiringHistory(n.id))
                            {
                                FiringHistory.AddNeuronToHistoryWindow(n1.id);
                                OpenHistoryWindow();
                            }
                            else
                                FiringHistory.RemoveNeuronFromHistoryWindow(n1.id);
                            break;
                        case "synapses":
                            if (MainWindow.arrayView.IsShowingSnapses(n.id))
                            {
                                MainWindow.arrayView.AddShowSynapses(n1.id);
                            }
                            else
                                MainWindow.arrayView.RemoveShowSynapses(n1.id);
                            break;
                        case "label":
                            if (n.label == "")
                                n1.label = "";
                            else if (n.id != n1.id)
                            {
                                string newLabel = n.label;
                                while (MainWindow.theNeuronArray.GetNeuron(newLabel) != null)
                                {
                                    int num = 0;
                                    int digitCount = 0;
                                    while (Char.IsDigit(newLabel[newLabel.Length - 1]))
                                    {
                                        int.TryParse(newLabel[newLabel.Length - 1].ToString(), out int digit);
                                        num = num + (int)Math.Pow(10, digitCount) * digit;
                                        digitCount++;
                                        newLabel = newLabel.Substring(0, newLabel.Length - 1);
                                    }
                                    num++;
                                    newLabel = newLabel + num.ToString();
                                }
                                n1.label = newLabel;
                            }
                            break;
                    }
                    n1.Update();
                }
            }
        }

        private static bool NeuronInSelection(int id)
        {
            bool neuronInSelection = false;
            foreach (NeuronSelectionRectangle sr in theNeuronArrayView.theSelection.selectedRectangles)
            {
                if (sr.NeuronIsInSelection(id))
                {
                    neuronInSelection = true;
                    break;
                }
            }
            return neuronInSelection;
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
            if ((string)mi.Header == "From Selection to Here")
            {
                theNeuronArrayView.targetNeuronIndex = i;
                theNeuronArrayView.ConnectToHere();
            }
            if ((string)mi.Header == "From Here to Selection")
            {
                theNeuronArrayView.targetNeuronIndex = i;
                theNeuronArrayView.ConnectFromHere();
            }
            if ((string)mi.Header == "Mutual Suppression")
            {
                theNeuronArrayView.targetNeuronIndex = i;
                theNeuronArrayView.MutualSuppression();
            }
        }
    }
}
