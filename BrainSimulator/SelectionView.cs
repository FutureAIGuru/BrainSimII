using BrainSimulator.Modules;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BrainSimulator
{
    public partial class SelectionView
    {
        public static readonly DependencyProperty SelectionNumberProperty =
    DependencyProperty.Register("SelectionNumber", typeof(int), typeof(MenuItem));

        public static void CreateSelectionContextMenu(int i, ContextMenu cm = null) //for a selection
        {
            cmCancelled = false;
            if (cm == null)
                cm = new ContextMenu();
            StackPanel sp;
            cm.SetValue(SelectionNumberProperty, i);
            cm.PreviewKeyDown += Cm_PreviewKeyDown;

            MenuItem mi = new MenuItem();
            mi = new MenuItem();
            mi.Header = "Cut";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);
            mi = new MenuItem();
            mi.Header = "Copy";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);

            mi = new MenuItem();
            mi.Header = "Delete";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);


            mi = new MenuItem();
            mi.Header = "Clear Selection";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);
            mi = new MenuItem();
            mi.Header = "Mutual Suppression";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);

            sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new Label { Content = "Random Synapses (Count): ", Padding = new Thickness(0) });
            sp.Children.Add(new TextBox { Text = "10", Width = 60, Name = "RandomSynapseCount", Padding = new Thickness(0) });
            mi = new MenuItem { Header = sp };
            mi.Click += Mi_Click;
            cm.Items.Add(mi);

            mi = new MenuItem();
            mi.Header = "Reset Hebbian Weights";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);

            sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new Label { Content = "Convert to Module: ", VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(0) });
            cm.Items.Add(new MenuItem { Header = sp, StaysOpenOnClick = true });

            ComboBox cb = new ComboBox();
            //get list of available modules...these are assignable to a "ModuleBase" 
            var modules = Utils.GetArrayOfModuleTypes();

            foreach (var v in modules)
            {
                //get the tooltip
                Type t = Type.GetType("BrainSimulator.Modules." + v.Name);
                // ModuleBase theModule = (Modules.ModuleBase)Activator.CreateInstance(t);
                string toolTip = t.Name;

                //make the combobox entry
                string theName = toolTip.Replace("Module", "");
                cb.Items.Add(new Label { Content = theName, ToolTip = toolTip, Margin = new Thickness(0), Padding = new Thickness(0) });
            }
            cb.Width = 80;
            cb.Name = "AreaType";
            cb.SelectionChanged += Cb_SelectionChanged;
            sp.Children.Add(new MenuItem { Header = cb, StaysOpenOnClick = true });

            sp = new StackPanel { Orientation = Orientation.Horizontal };
            Button b0 = new Button { Content = "OK", Width = 100, Height = 25, Margin = new Thickness(10) };
            b0.Click += B0_Click;
            sp.Children.Add(b0);
            b0 = new Button { Content = "Cancel", Width = 100, Height = 25, Margin = new Thickness(10) };
            b0.Click += B0_Click;
            sp.Children.Add(b0);

            cm.Items.Add(new MenuItem { Header = sp, StaysOpenOnClick = true });

            cm.Closed += Cm_Closed;
        }

        private static void Cm_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ContextMenu cm = sender as ContextMenu;
            if (e.Key == Key.Enter)
            {
                Cm_Closed(sender, e);
            }
        }

        private static void Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb)
                if (cb.Parent is StackPanel sp)
                    if (sp.Parent is MenuItem mi)
                        if (mi.Parent is ContextMenu cm)
                            cm.IsOpen = false;
        }

        static bool cmCancelled = false;
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


        static bool deleted = false;
        private static void Cm_Closed(object sender, RoutedEventArgs e)
        {
            if ((Keyboard.GetKeyStates(Key.Escape) & KeyStates.Down) > 0)
            {
                MainWindow.Update();
                return;
            }
            if (deleted)
            {
                deleted = false;
            }
            else if (sender is ContextMenu cm)
            {
                if (!cm.IsOpen) return;
                cm.IsOpen = false;
                if (cmCancelled) return;

                int i = (int)cm.GetValue(SelectionNumberProperty);
                string label = "";
                string commandLine = "";
                Color color = Colors.LightBlue;
                int width = 1, height = 1;

                Control cc = Utils.FindByName(cm, "AreaType");
                if (cc is ComboBox cb && cb.SelectedValue != null)
                {
                    string moduleType = (string)((Label)cb.SelectedValue).Content;
                    commandLine = "Module" + moduleType;
                    if (commandLine == "") return;//something went wrong
                    label = moduleType;
                }

                if (label == "" && commandLine == "") return;

                //check to see if the module will fit in the array
                Type t = Type.GetType("BrainSimulator.Modules." + commandLine);
                Modules.ModuleBase tempModule = (Modules.ModuleBase)Activator.CreateInstance(t);
                SelectionRectangle nsr = MainWindow.arrayView.theSelection.selectedRectangles[i];
                MainWindow.theNeuronArray.GetNeuronLocation(nsr.FirstSelectedNeuron, out int col, out int row);
                if (row + tempModule.MinHeight > MainWindow.theNeuronArray.rows ||
                    col + tempModule.MinWidth > MainWindow.theNeuronArray.Cols)
                {
                    MessageBox.Show("Minimum size would exceed neuron array boundary.");
                    return;
                }


                //convert a selection rectangle to a module
                MainWindow.theNeuronArray.SetUndoPoint();

                //clear out the underlying neurons
                //TODO: take care of this hack
                if (commandLine != "ModuleNull")
                    MainWindow.arrayView.DeleteSelection(true);
                MainWindow.theNeuronArray.AddModuleUndo(MainWindow.theNeuronArray.modules.Count, null);
                width = MainWindow.arrayView.theSelection.selectedRectangles[i].Width;
                height = MainWindow.arrayView.theSelection.selectedRectangles[i].Height;

                if (width < tempModule.MinWidth) width = tempModule.MinWidth;
                if (width > tempModule.MaxWidth) width = tempModule.MaxWidth;
                if (height < tempModule.MinHeight) height = tempModule.MinHeight;
                if (height > tempModule.MaxHeight) height = tempModule.MaxHeight;

                CreateModule(label, commandLine, color, nsr.FirstSelectedNeuron, width, height);
                MainWindow.arrayView.theSelection.selectedRectangles.RemoveAt(i);
            }
            MainWindow.Update();
        }

        public static void CreateModule(string label, string commandLine, Color color, int firstNeuron, int width, int height)
        {

            ModuleView mv = new ModuleView(firstNeuron, width, height, label, commandLine, Utils.ColorToInt(color));
            if (mv.Width < mv.TheModule.MinWidth) mv.Width = mv.TheModule.MinWidth;
            if (mv.Height < mv.TheModule.MinHeight) mv.Height = mv.TheModule.MinHeight;
            MainWindow.theNeuronArray.modules.Add(mv);
            MainWindow.SuspendEngine();
            mv.TheModule.SetModuleView();
            mv.TheModule.Initialize();
            MainWindow.ResumeEngine();
            return;
        }

        private static void Mi_Click(object sender, RoutedEventArgs e)
        {
            //Handle delete  & initialize commands
            if (sender is MenuItem mi)
            {
                if (mi.Header is StackPanel sp && sp.Children[0] is Label l && l.Content.ToString().StartsWith("Random"))
                {
                    if (sp.Children[1] is TextBox tb0)
                    {
                        if (int.TryParse(tb0.Text, out int count))
                        {
                            MainWindow.arrayView.CreateRandomSynapses(count);
                            MainWindow.theNeuronArray.ShowSynapses = true;
                            MainWindow.thisWindow.SetShowSynapsesCheckBox(true);
                            MainWindow.Update();
                        }
                    }
                    return;
                }
                if ((string)mi.Header == "Cut")
                {
                    MainWindow.arrayView.CutNeurons();
                    MainWindow.Update();
                }
                if ((string)mi.Header == "Copy")
                {
                    MainWindow.arrayView.CopyNeurons();
                }
                if ((string)mi.Header == "Clear Selection")
                {
                    MainWindow.arrayView.ClearSelection();
                    MainWindow.Update();
                }
                if ((string)mi.Header == "Mutual Suppression")
                {
                    MainWindow.arrayView.MutualSuppression();
                    MainWindow.theNeuronArray.ShowSynapses = true;
                    MainWindow.thisWindow.SetShowSynapsesCheckBox(true);
                    MainWindow.Update();
                }
                if ((string)mi.Header == "Delete")
                {
                    int i = (int)mi.Parent.GetValue(SelectionNumberProperty);
                    MainWindow.arrayView.DeleteSelection();
                }
                if ((string)mi.Header == "Reset Hebbian Weights")
                {
                    MainWindow.theNeuronArray.SetUndoPoint();
                    foreach (SelectionRectangle sr in MainWindow.arrayView.theSelection.selectedRectangles)
                    {
                        foreach (int Id in sr.NeuronInRectangle())
                        {
                            Neuron n = MainWindow.theNeuronArray.GetNeuron(Id);
                            foreach (Synapse s in n.Synapses)
                            {
                                if (s.model != Synapse.modelType.Fixed)
                                {
                                    //TODO: Add some UI for this:
                                    //s.model = Synapse.modelType.Hebbian2;
                                    n.AddSynapseWithUndo(s.targetNeuron, 0, s.model);
                                    s.Weight = 0;
                                }
                            }
                        }
                    }
                    MainWindow.Update();
                }
            }
        }
    }
}
