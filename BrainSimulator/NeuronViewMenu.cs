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

    //TODO make this into a dialog
    public partial class NeuronView : DependencyObject
    {

        //for UI performance, the context menu is not attached to a neuron when the neuron is created but
        //is built on the fly when a neuron is right-clicked.  Hence the public-static
        static bool cmCancelled = false;
        static bool chargeChanged = false;
        static bool labelChanged = false;
        static bool toolTipChanged = false;
        static bool modelChanged = false;
        static bool enabledChanged = false;
        static bool historyChanged = false;
        static bool synapsesChanged = false;
        static bool leakRateChanged = false;
        static bool axonDelayChanged = false;
        public static ContextMenu CreateContextMenu(int i, Neuron n, ContextMenu cm)
        {
            cmCancelled = false;

            labelChanged = false;
            toolTipChanged = false;
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
            cm.StaysOpen = true;
            cm.Width = 300;

            //The neuron label
            MenuItem mi1 = new MenuItem { Header = "ID: " + n.id, Padding = new Thickness(0) };
            cm.Items.Add(mi1);

            //apply to all in selection
            CheckBox cbApplyToSelection = new CheckBox
            {
                IsChecked = true,
                Content = "Apply changes to all neurons in selection",
                Name = "ApplyToSelection",
            };
            cbApplyToSelection.Checked += CbCheckedChanged;
            cbApplyToSelection.Unchecked += CbCheckedChanged;
            cm.Items.Add(new MenuItem { StaysOpenOnClick = true, Header = cbApplyToSelection });
            if (MainWindow.arrayView.theSelection.selectedRectangles.Count > 0)
            {
                cbApplyToSelection.IsEnabled = true;
                cbApplyToSelection.IsChecked = NeuronInSelection(n.id);
            }
            else
            {
                cbApplyToSelection.IsChecked = false;
                cbApplyToSelection.IsEnabled = false;
            }

            //label
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, };
            sp.Children.Add(new Label { Content = "Label: ", Padding = new Thickness(0), VerticalAlignment = VerticalAlignment.Center, }); ;
            TextBox tb = Utils.ContextMenuTextBox(n.Label, "Label", 170);
            tb.TextChanged += Tb_TextChanged;
            sp.Children.Add(tb);
            sp.Children.Add(new Label { Content = "Warning: Duplicate Label", FontSize = 8, Name = "DupWarn", Visibility = Visibility.Hidden });
            mi1 = new MenuItem { StaysOpenOnClick = true, Header = sp };
            cm.Items.Add(mi1);

            //tooltip
            if (n.Label != "" || n.ToolTip != "") //add the tooltip textbox if needed
            {
                sp = new StackPanel { Orientation = Orientation.Horizontal };
                sp.Children.Add(new Label { Content = "Tooltip: ", VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(0) });
                tb = Utils.ContextMenuTextBox(n.ToolTip, "ToolTip", 150);
                tb.TextChanged += Tb_TextChanged;
                sp.Children.Add(tb);
                cm.Items.Add(sp);
            }

            //The neuron model
            sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
            sp.Children.Add(new Label { Content = "Model: ", Padding = new Thickness(0) });
            ComboBox cb = new ComboBox()
            { Width = 80, Name = "Model" };
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
            cm.Items.Add(new MenuItem { StaysOpenOnClick = true, Header = sp });

            cm.Items.Add(new Separator { Visibility = Visibility.Collapsed });
            cm.Items.Add(new Separator { Visibility = Visibility.Collapsed });


            MenuItem mi = new MenuItem();
            CheckBox cbEnableNeuron = new CheckBox
            {
                IsChecked = (n.leakRate > 0) || float.IsPositiveInfinity(1.0f / n.leakRate),
                Content = "Enabled",
                Name = "Enabled",
            };
            cbEnableNeuron.Checked += CbCheckedChanged;
            cbEnableNeuron.Unchecked += CbCheckedChanged;
            cm.Items.Add(new MenuItem { StaysOpenOnClick = true, Header = cbEnableNeuron });

            CheckBox cbShowSynapses = new CheckBox
            {
                IsChecked = MainWindow.arrayView.IsShowingSynapses(n.id),
                Content = "Show Synapses",
                Name = "Synapses",
            };
            cbShowSynapses.Checked += CbCheckedChanged;
            cbShowSynapses.Unchecked += CbCheckedChanged;
            cm.Items.Add(new MenuItem { StaysOpenOnClick = true, Header = cbShowSynapses });

            mi = new MenuItem();
            CheckBox cbHistory = new CheckBox
            {
                IsChecked = FiringHistory.NeuronIsInFiringHistory(n.id),
                Content = "Record Firing History",
                Name = "History",
            };
            cbHistory.Checked += CbCheckedChanged;
            cbHistory.Unchecked += CbCheckedChanged;
            cm.Items.Add(new MenuItem { StaysOpenOnClick = true, Header = cbHistory });

            mi = new MenuItem { Header = "Clear Synapses" };
            mi.Click += Mi_Click;
            cm.Items.Add(mi);

            cm.Items.Add(new Separator());
            cm.Items.Add(new Separator());

            mi = new MenuItem();
            mi.Header = "Synapses Out";
            mi.Width = 250;
            mi.HorizontalAlignment = HorizontalAlignment.Left;
            foreach (Synapse s in n.Synapses)
            {
                AddSynapseEntryToMenu(mi, s);
            }
            cm.Items.Add(mi);

            mi = new MenuItem();
            mi.Header = "Synapses In";
            mi.Width = 250;
            mi.HorizontalAlignment = HorizontalAlignment.Left;
            foreach (Synapse s in n.SynapsesFrom)
            {
                AddSynapseEntryToMenu(mi, s);
            }
            cm.Items.Add(mi);

            mi = new MenuItem { Header = "Paste Here" };
            if (!XmlFile.WindowsClipboardContainsNeuronArray()) mi.IsEnabled = false;
            mi.Click += Mi_Click;
            cm.Items.Add(mi);

            mi = new MenuItem { Header = "Move Here" };
            if (MainWindow.arrayView.theSelection.selectedRectangles.Count == 0) mi.IsEnabled = false;
            mi.Click += Mi_Click;
            cm.Items.Add(mi);


            mi = new MenuItem();
            mi.Header = "Connect Multiple Synapses";
            mi.Width = 250;
            mi.HorizontalAlignment = HorizontalAlignment.Left;
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

            cm.Items.Add(new MenuItem { Header = sp, StaysOpenOnClick = true });

            SetCustomCMItems(cm, n, n.model);

            return cm;
        }

        private static void AddSynapseEntryToMenu(MenuItem mi, Synapse s)
        {
            string targetLabel = MainWindow.theNeuronArray.GetNeuron(s.targetNeuron).Label;
            StackPanel sp0 = new StackPanel { Orientation = Orientation.Horizontal };
            TextBlock tbWeight = new TextBlock { Text = s.Weight.ToString("F3").PadLeft(9) };
            tbWeight.MouseEnter += SynapseEntry_MouseEnter;
            tbWeight.MouseLeave += SynapseEntry_MouseLeave;
            tbWeight.ToolTip = "Click to edit synapse";
            tbWeight.MouseDown += SynapseEntry_MouseDown;
            tbWeight.Name = "weight";

            TextBlock tbTarget = new TextBlock { Text = s.targetNeuron.ToString().PadLeft(8) + " " + targetLabel };
            sp0.Children.Add(tbWeight);
            sp0.Children.Add(tbTarget);
            tbTarget.MouseEnter += SynapseEntry_MouseEnter;
            tbTarget.MouseLeave += SynapseEntry_MouseLeave;
            tbTarget.ToolTip = "Click to go to neuron";
            tbTarget.MouseDown += SynapseEntry_MouseDown;
            tbTarget.Name = "neuron";
            mi.Items.Add(sp0);
        }

        //This creates or updates the portion of the context menu content which depends on the model type
        private static void SetCustomCMItems(ContextMenu cm, Neuron n, Neuron.modelType newModel)
        {
            //find first seperator;
            int insertPosition = 0;
            for (int i = 0; i < cm.Items.Count; i++)
            {
                if (cm.Items[i].GetType() == typeof(Separator))
                {
                    insertPosition = i + 1;
                    while (i + 1 < cm.Items.Count && cm.Items[i + 1].GetType() != typeof(Separator))
                        cm.Items.RemoveAt(i + 1);
                    break;
                }
            }

            //The charge value formatted based on the model
            if (newModel == Neuron.modelType.Color)
            {
                StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
                sp.Children.Add(new Label { Content = "Content: " });
                ComboBox cb0 = (Utils.CreateComboBox("CurrentCharge", n.LastChargeInt, colorValues, colorFormatString, 80, ComboBox_ContentChanged));
                sp.Children.Add(cb0);
                for (int i = 0; i < cb0.Items.Count; i++)
                {
                    string cc = cb0.Items[i].ToString();
                    cb0.Items[i] = new Label { Content = cc };
                    if (int.TryParse(cc, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int colorValue))
                    {
                        System.Drawing.Color col = System.Drawing.ColorTranslator.FromWin32(colorValue);
                        if (col.R * 0.2126 + col.G * 0.7152 + col.B * 0.0722 < 255 / 2)
                        {
                            //dark color
                            ((Label)cb0.Items[i]).Foreground = new SolidColorBrush(Utils.IntToColor(0xffffff));
                        }
                         ((Label)cb0.Items[i]).Background = new SolidColorBrush(Utils.IntToColor(colorValue));
                    }
                }
                cm.Items.Insert(insertPosition + 1, new MenuItem { Header = sp, StaysOpenOnClick = true });

            }
            else if (newModel == Neuron.modelType.FloatValue)
            {
                cm.Items.Insert(insertPosition,
                    Utils.CreateComboBoxMenuItem("CurrentCharge", n.lastCharge, currentChargeValues, floatValueFormatString, "Content: ", 80, ComboBox_ContentChanged));
            }
            else
            {
                cm.Items.Insert(insertPosition,
                    Utils.CreateComboBoxMenuItem("CurrentCharge", n.lastCharge, currentChargeValues, floatFormatString, "Charge: ", 80, ComboBox_ContentChanged));
            }

            if (newModel == Neuron.modelType.LIF)
            {
                cm.Items.Insert(insertPosition + 1,
                    Utils.CreateComboBoxMenuItem("LeakRate", Math.Abs(n.leakRate), leakRateValues, floatFormatString, "Leak Rate: ", 80, ComboBox_ContentChanged));
                cm.Items.Insert(insertPosition + 2,
                    Utils.CreateComboBoxMenuItem("AxonDelay", n.axonDelay, axonDelayValues, intFormatString, "AxonDelay: ", 80, ComboBox_ContentChanged));
            }
            else if (newModel == Neuron.modelType.Always)
            {
                StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
                sp.Children.Add(new Label { Content = "Period: " });
                ComboBox cb0 = (Utils.CreateComboBox("AxonDelay", n.axonDelay, alwaysDelayValues, intFormatString, 80, ComboBox_ContentChanged));
                sp.Children.Add(cb0);
                Slider sl = new Slider { Width = 100, Margin = new Thickness(10, 4, 0, 0), Value = 10, Maximum = 10 };
                sl.ValueChanged += Sl_ValueChanged;
                sp.Children.Add(sl);
                cm.Items.Insert(insertPosition + 1, new MenuItem { Header = sp, StaysOpenOnClick = true });
            }
            else if (newModel == Neuron.modelType.Random)
            {
                cm.Items.Insert(insertPosition + 1,
                    Utils.CreateComboBoxMenuItem("AxonDelay", n.axonDelay, meanValues, intFormatString, "Mean: ", 80, ComboBox_ContentChanged));
                cm.Items.Insert(insertPosition + 2,
                    Utils.CreateComboBoxMenuItem("LeakRate", Math.Abs(n.leakRate), stdDevValues, floatFormatString, "Std Dev: ", 80, ComboBox_ContentChanged));
            }
            else if (newModel == Neuron.modelType.Burst)
            {
                cm.Items.Insert(insertPosition + 1,
                    Utils.CreateComboBoxMenuItem("AxonDelay", n.axonDelay, alwaysDelayValues, intFormatString, "Count: ", 80, ComboBox_ContentChanged));
                cm.Items.Insert(insertPosition + 2,
                    Utils.CreateComboBoxMenuItem("LeakRate", Math.Abs(n.leakRate), axonDelayValues, intFormatString, "Rate: ", 80, ComboBox_ContentChanged));
            }
        }


        private static void SynapseEntry_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock tb0)
            {
                if (tb0.Parent is StackPanel sp)
                {
                    if (sp.Parent is MenuItem mi)
                    {
                        if (mi.Parent is ContextMenu cm)
                        {
                            if (tb0.Name == "weight")
                            {
                                int sourceID = (int)cm.GetValue(NeuronIDProperty);
                                if (sp.Children.Count > 1 && sp.Children[1] is TextBlock tb1)
                                {
                                    int.TryParse(tb1.Text.Substring(0, 8), out int targetID);
                                    if (mi.Header.ToString().Contains("In"))
                                    {
                                        int temp = targetID;
                                        targetID = sourceID;
                                        sourceID = temp;
                                    }
                                    ContextMenu newCm = new ContextMenu();
                                    Neuron n = MainWindow.theNeuronArray.GetNeuron(sourceID);
                                    Synapse s = n.FindSynapse(targetID);
                                    if (s != null)
                                    {
                                        SynapseView.CreateContextMenu(sourceID, s, newCm);
                                        newCm.IsOpen = true;
                                        e.Handled = true;
                                    }

                                }
                            }
                            if (tb0.Name == "neuron")
                            {
                                int.TryParse(tb0.Text.Substring(0, 8), out int targetID);
                                Neuron n1 = MainWindow.theNeuronArray.GetNeuron(targetID);
                                ContextMenu cm1 = NeuronView.CreateContextMenu(n1.id, n1, new ContextMenu() { IsOpen = true, });
                                MainWindow.arrayView.targetNeuronIndex = targetID;
                                Point loc = dp.pointFromNeuron(targetID);
                                if (loc.X < 0 || loc.X > theCanvas.ActualWidth - cm.ActualWidth ||
                                    loc.Y < 0 || loc.Y > theCanvas.ActualHeight - cm.ActualHeight)
                                {
                                    MainWindow.arrayView.PanToNeuron(targetID);
                                    loc = dp.pointFromNeuron(targetID);
                                }
                                loc.X += dp.NeuronDisplaySize / 2;
                                loc.Y += dp.NeuronDisplaySize / 2;
                                loc = MainWindow.arrayView.theCanvas.PointToScreen(loc);
                                cm1.PlacementRectangle = new Rect(loc.X, loc.Y, 0, 0);
                                cm1.Placement = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint;
                            }
                        }
                    }
                }
                e.Handled = true;
            }
        }

        private static void SynapseEntry_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is TextBlock tb0)
                tb0.Background = null;
        }

        private static void SynapseEntry_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is TextBlock tb0)
                tb0.Background = new SolidColorBrush(Colors.LightGreen);
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


        //get here if the user moved the slider for an Always firing neuron
        private static void Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sl = sender as Slider;
            StackPanel sp = sl.Parent as StackPanel;
            MenuItem mi = sp.Parent as MenuItem;
            ContextMenu cm = mi.Parent as ContextMenu;
            int neuronID = (int)cm.GetValue(NeuronIDProperty);
            Control cc = Utils.FindByName(cm, "AxonDelay");
            if (cc is ComboBox tb3)
            {
                int.TryParse(tb3.Text, out int axonDelay);
                int newDelay = (int)(axonDelay * (float)sl.Value / 10.0f);
                Neuron n = MainWindow.theNeuronArray.GetNeuron(neuronID);
                n.AxonDelay = newDelay;
                n.Update();
            }
        }


        static List<float> leakRateValues = new List<float>() { 0, 0.1f, 0.5f, 1.0f };
        static List<float> axonDelayValues = new List<float>() { 0, 1, 4, 10 };
        static List<float> meanValues = new List<float>() { 0, 1, 4, 10 };
        static List<float> stdDevValues = new List<float>() { 0, 1, 4, 10 };
        static List<float> currentChargeValues = new List<float>() { 0, 1, };
        static List<float> colorValues = new List<float>() { 0x00, 0xff0000, 0xff00, 0xff, 0xffff00, 0xff00ff, 0xffff, 0xffa500, 0xffffff };
        static List<float> alwaysDelayValues = new List<float>() { 0, 1, 2, 3 };

        const string intFormatString = "F0";
        const string floatFormatString = "F2";
        const string colorFormatString = "X8";
        const string floatValueFormatString = "F4";

        private static void ComboBox_ContentChanged(object sender, object e)
        {
            if (sender is ComboBox cb)
            {
                if (!cb.IsArrangeValid) return;
                if (cb.Name == "LeakRate")
                {
                    leakRateChanged = true;
                    Utils.ValidateInput(cb, -1, 1);
                }
                if (cb.Name == "AxonDelay")
                {
                    axonDelayChanged = true;
                    Utils.ValidateInput(cb, 0, int.MaxValue, "Int");
                }
                if (cb.Name == "CurrentCharge")
                {
                    chargeChanged = true;

                    //all this to get the updated neuron model to set up the correct validation
                    string validation = "";
                    StackPanel sp = cb.Parent as StackPanel;
                    MenuItem mi = sp.Parent as MenuItem;
                    ContextMenu cm = mi.Parent as ContextMenu;
                    ComboBox cb1 = (ComboBox)Utils.FindByName(cm, "Model");
                    ListBoxItem lbi = (ListBoxItem)cb1.SelectedItem;
                    Neuron.modelType nm = (Neuron.modelType)System.Enum.Parse(typeof(Neuron.modelType), lbi.Content.ToString());
                    if (nm == Neuron.modelType.Color) validation = "Hex";
                    Utils.ValidateInput(cb, 0, 1, validation);
                }
            }
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
                    tb.Select(0, tb.Text.Length);
                }
            }
        }

        private static void Cm_Closed(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu cm)
            {
                if (!cm.IsOpen) return;
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

                bool applyToAll = false;
                Control cc = Utils.FindByName(cm, "ApplyToSelection");
                if (cc is CheckBox cb)
                    if (cb.IsChecked == true) applyToAll = true;

                cc = Utils.FindByName(cm, "ToolTip");
                if (cc is TextBox tb1)
                {
                    string newLabel = tb1.Text;
                    if (toolTipChanged)
                    {
                        MainWindow.theNeuronArray.SetUndoPoint();
                        n.ToolTip = newLabel;
                        if (applyToAll)
                            SetValueInSelectedNeurons(n, "toolTip");
                    }
                }
                cc = Utils.FindByName(cm, "Label");
                if (cc is TextBox tb)
                {
                    string newLabel = tb.Text;
                    if (int.TryParse(newLabel, out int dummy))
                        newLabel = "_" + newLabel;
                    if (labelChanged)
                    {
                        MainWindow.theNeuronArray.SetUndoPoint();
                        n.Label = newLabel;
                        if (applyToAll)
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
                        if (applyToAll)
                            SetValueInSelectedNeurons(n, "model");
                    }
                }
                cc = Utils.FindByName(cm, "CurrentCharge");
                if (cc is ComboBox cbb1)
                {
                    if (n.model == Neuron.modelType.Color)
                    {
                        try
                        {
                            uint newCharge = Convert.ToUInt32(cbb1.Text, 16);
                            if (chargeChanged)
                            {
                                n.SetValueInt((int)newCharge);
                                n.lastCharge = newCharge;
                                if (applyToAll)
                                    SetValueInSelectedNeurons(n, "currentCharge");
                                Utils.AddToValues(newCharge, colorValues);
                            }
                        }
                        catch { };
                    }
                    else
                    {
                        float.TryParse(cbb1.Text, out float newCharge);
                        if (chargeChanged)
                        {
                            n.SetValue(newCharge);
                            n.lastCharge = newCharge;
                            if (applyToAll)
                                SetValueInSelectedNeurons(n, "currentCharge");
                            Utils.AddToValues(newCharge, currentChargeValues);
                        }
                    }
                }
                cc = Utils.FindByName(cm, "LeakRate");
                if (cc is ComboBox tb2)
                {
                    float.TryParse(tb2.Text, out float leakRate);
                    if (leakRateChanged)
                    {
                        n.LeakRate = leakRate;
                        if (applyToAll)
                            SetValueInSelectedNeurons(n, "leakRate");
                        if (n.model == Neuron.modelType.LIF)
                            Utils.AddToValues(leakRate, leakRateValues);
                        if (n.model == Neuron.modelType.Random)
                            Utils.AddToValues(leakRate, stdDevValues);
                        if (n.model == Neuron.modelType.Burst)
                            Utils.AddToValues(leakRate, axonDelayValues);
                    }
                }
                else
                    n.leakRate = 0;
                cc = Utils.FindByName(cm, "AxonDelay");
                if (cc is ComboBox tb3)
                {
                    int.TryParse(tb3.Text, out int axonDelay);
                    if (axonDelayChanged)
                    {
                        n.axonDelay = axonDelay;
                        if (applyToAll)
                            SetValueInSelectedNeurons(n, "axonDelay");
                        if (n.model == Neuron.modelType.Random)
                            Utils.AddToValues(axonDelay, meanValues);
                        else if (n.model == Neuron.modelType.Always)
                            Utils.AddToValues(axonDelay, alwaysDelayValues);
                        else if (n.model == Neuron.modelType.Burst)
                            Utils.AddToValues(axonDelay, alwaysDelayValues);
                        else
                            Utils.AddToValues(axonDelay, axonDelayValues);
                    }
                }
                cc = Utils.FindByName(cm, "Synapses");
                if (cc is CheckBox cb2)
                {
                    if (synapsesChanged)
                    {
                        n.ShowSynapses = (bool)cb2.IsChecked;
                        //if (cb2.IsChecked == true)
                        //{
                        //    MainWindow.arrayView.AddShowSynapses(n.id);
                        //}
                        //else
                        //    MainWindow.arrayView.RemoveShowSynapses(n.id);
                        if (applyToAll)
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

                        if (applyToAll)
                            SetValueInSelectedNeurons(n, "enable");
                    }
                }

                cc = Utils.FindByName(cm, "History");
                if (cc is CheckBox cb3)
                {
                    if (historyChanged)
                    {
                        n.RecordHistory = (bool)cb3.IsChecked;
                        if (applyToAll)
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


        //this checks the name against existing names and warns on duplicates
        private static void Tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && tb.Name == "Label")
            {
                labelChanged = true;
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
            if (sender is TextBox tb1 && tb1.Name == "ToolTip")
            {
                toolTipChanged = true;
            }
        }

        private static void Cm_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                cmCancelled = true;
            if (e.Key == Key.Enter)
            {
                if (sender is ContextMenu cm)
                {
                    Cm_Closed(cm, e);
                }
            }
            //This hack is here because textboxes don't like to lose focus if the mouse moves around the context menu
            //When this becomes a window, all this will go away
            if (e.Key == Key.Tab)
            {
                if (sender is ContextMenu cm)
                {
                    var focussedControl = FocusManager.GetFocusedElement(cm);
                    if (focussedControl is TextBox tb)
                    {
                        if (tb.Name == "Label")
                        {
                            Control tt = Utils.FindByName(cm, "ToolTip");
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
                        else if (tb.Name == "ToolTip")
                        {
                            Control cc = Utils.FindByName(cm, "Model");
                            cc.Focus();
                            e.Handled = true;

                        }
                    }
                }
            }
        }

        public static void OpenHistoryWindow()
        {
            if (MainWindow.Busy()) return;
            if (FiringHistory.history.Count == 0) return;
            if (MainWindow.fwWindow == null || !MainWindow.fwWindow.IsVisible)
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    MainWindow.fwWindow = new FiringHistoryWindow();
                    MainWindow.fwWindow.Show();
                });
        }

        //change the model and update the context menu
        private static void Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            modelChanged = true;
            ComboBox cb = sender as ComboBox;
            StackPanel sp = cb.Parent as StackPanel;
            MenuItem mi = sp.Parent as MenuItem;
            ContextMenu cm = mi.Parent as ContextMenu;
            int neuronID = (int)cm.GetValue(NeuronIDProperty);
            ListBoxItem lbi = (ListBoxItem)cb.SelectedItem;
            Neuron.modelType nm = (Neuron.modelType)System.Enum.Parse(typeof(Neuron.modelType), lbi.Content.ToString());

            Neuron n = MainWindow.theNeuronArray.GetNeuron(neuronID);
            SetCustomCMItems(cm, n, nm);
        }
        private static void SetValueInSelectedNeurons(Neuron n, string property)
        {
            bool neuronInSelection = true;
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
                            n1.Label = "";
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
                        case "clear": n1.ClearWithUndo(); break;
                        case "leakRate": n1.leakRate = n.leakRate; break;
                        case "axonDelay": n1.axonDelay = n.axonDelay; break;
                        case "model": n1.model = n.model; break;
                        case "enable": n1.leakRate = n.leakRate; break;
                        case "history":
                            n1.RecordHistory = n.RecordHistory;
                            break;
                        case "synapses":
                            n1.ShowSynapses = n.ShowSynapses;
                            //if (MainWindow.arrayView.IsShowingSynapses(n.id))
                            //{
                            //    MainWindow.arrayView.AddShowSynapses(n1.id);
                            //}
                            //else
                            //    MainWindow.arrayView.RemoveShowSynapses(n1.id);
                            break;
                        case "toolTip":
                            n1.ToolTip = n.ToolTip;
                            break;
                        case "label":
                            if (n.Label == "")
                                n1.Label = "";
                            else if (n1.id != n.id)
                            {
                                string newLabel = n.Label;
                                if (!Char.IsDigit(newLabel[newLabel.Length - 1])) newLabel += "0";
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
                                n1.Label = newLabel;
                            }
                            break;
                    }
                    n1.Update();
                }
            }
        }

        public static bool NeuronInSelection(int id)
        {
            bool neuronInSelection = false;
            foreach (SelectionRectangle sr in theNeuronArrayView.theSelection.selectedRectangles)
            {
                if (sr.NeuronIsInSelection(id))
                {
                    neuronInSelection = true;
                    break;
                }
            }
            return neuronInSelection;
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
            if ((string)mi.Header == "Clear Synapses")
            {
                MainWindow.theNeuronArray.SetUndoPoint();
                n.ClearWithUndo();
                Control cc = Utils.FindByName(cm, "ApplyToSelection");
                if (cc is CheckBox cb)
                    if (cb.IsChecked == true)
                        SetValueInSelectedNeurons(n, "clear");
                cmCancelled = true;
                MainWindow.Update();
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
