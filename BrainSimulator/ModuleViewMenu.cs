using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using BrainSimulator.Modules;

namespace BrainSimulator
{
    public partial class ModuleView : DependencyObject
    {
        public static readonly DependencyProperty AreaNumberProperty =
    DependencyProperty.Register("AreaNumber", typeof(int), typeof(MenuItem));
        public int AreaNumber
        {
            get { return (int)GetValue(AreaNumberProperty); }
            set { SetValue(AreaNumberProperty, value); }
        }

        public static void CreateContextMenu(int i, ModuleView nr, Rectangle r) //for a selection
        {
            ContextMenu cm = new ContextMenu();
            StackPanel sp;
            cm.SetValue(AreaNumberProperty, i);
            MenuItem mi = new MenuItem();
            mi.Header = "Delete";
            mi.Click += Mi_Click;
            cm.Items.Add(mi);
            if (i < 0)
            {
                mi = new MenuItem();
                mi.Header = "Reset Hebbian Weights";
                mi.Click += Mi_Click;
                cm.Items.Add(mi);
            }
            if (i >= 0)
            {
                mi = new MenuItem();
                mi.Header = "Initialize";
                mi.Click += Mi_Click;
                cm.Items.Add(mi);
                mi = new MenuItem();
                if (nr.TheModule.ShortDescription != null || nr.TheModule.LongDescription != null)
                {
                    mi.Header = "Info...";
                    mi.Click += Mi_Click;
                    cm.Items.Add(mi);
                }
                sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 3, 3) };
                sp.Children.Add(new Label { Content = "Width: ", VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(0) });
                sp.Children.Add(new TextBox { Text = nr.Width.ToString(), Width = 60, Name = "AreaWidth", VerticalAlignment = VerticalAlignment.Center });
                sp.Children.Add(new Label { Content = "Height: " });
                sp.Children.Add(new TextBox { Text = nr.Height.ToString(), Width = 60, Name = "AreaHeight", VerticalAlignment = VerticalAlignment.Center });
                cm.Items.Add(sp);

                sp = new StackPanel { Orientation = Orientation.Horizontal };
                sp.Children.Add(new Label { Content = "Name: ", Padding = new Thickness(0) });
                sp.Children.Add(new TextBox { Text = nr.Label, Width = 140, Name = "AreaName", Padding = new Thickness(0) });
                cm.Items.Add(sp);
            }

            sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new Label { Content = "Type: ", VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(0) });
            cm.Items.Add(sp);

            ComboBox cb = new ComboBox();
            //get list of available NEW modules...these are assignable to a "ModuleBase" 
            var listOfBs = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                            from assemblyType in domainAssembly.GetTypes()
                            where typeof(ModuleBase).IsAssignableFrom(assemblyType)
                            orderby assemblyType.Name
                            select assemblyType
                            ).ToArray();
            foreach (var v in listOfBs)
            {
                if (v.Name != "ModuleBase")
                {
                    Type t = Type.GetType(v.FullName);
                    if (v.Name != "ModuleBase")
                    {
                        ComboBoxItem cbi = new ComboBoxItem
                        {
                            Content = v.Name,
                        };
                        cb.Items.Add(v.Name);
                    }
                }
            }
            if (nr.TheModule != null)
            {
                string cm1 = nr.TheModule.GetType().Name.ToString();
                if (cm1 != "")
                    cb.SelectedValue = cm1;
            }
            cb.Width = 180;
            cb.Name = "AreaType";
            cb.SelectionChanged += Cb_SelectionChanged;
            sp.Children.Add(cb);


            TextBox tb2 = new TextBox();
            tb2.Text = "";
            tb2.Name = "CommandParams";
            if (nr.CommandLine.IndexOf(" ") > 0) tb2.Text = nr.CommandLine.Substring(nr.CommandLine.IndexOf(" ") + 1);
            tb2.Width = 200;
            cm.Items.Add(tb2);

            if (i >= 0)
            {            //color picker
                Color c = Utils.IntToColor(nr.Color);
                cb = new ComboBox();
                cb.Width = 200;
                cb.Name = "AreaColor";
                PropertyInfo[] x1 = typeof(Colors).GetProperties();
                int sel = -1;
                for (int i1 = 0; i1 < x1.Length; i1++)
                {
                    Color cc = (Color)ColorConverter.ConvertFromString(x1[i1].Name);
                    if (cc == c)

                    {
                        sel = i1;
                        break;
                    }
                }
                if (nr.Color == 0) sel = 3;
                foreach (PropertyInfo s in x1)
                {
                    ComboBoxItem cbi = new ComboBoxItem()
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(s.Name)),
                        Content = s.Name
                    };
                    Rectangle r1 = new Rectangle()
                    {
                        Width = 20,
                        Height = 20,
                        Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(s.Name)),
                        Margin = new Thickness(0, 0, 140, 0),
                    };
                    Grid g = new Grid();
                    g.Children.Add(r1);
                    g.Children.Add(new Label() { Content = s.Name, Margin = new Thickness(25, 0, 0, 0) });
                    cbi.Content = g;
                    cbi.Width = 200;
                    cb.Items.Add(cbi);
                }
                cb.SelectedIndex = sel;
                cm.Items.Add(cb);
            }
            if (i >= 0 && MainWindow.theNeuronArray.Modules[i].TheModule != null)
            {
                var t = MainWindow.theNeuronArray.Modules[i].TheModule.GetType();
                Type t1 = Type.GetType(t.ToString() + "Dlg");
                while (t1 == null && t.BaseType.Name != "ModuleBase")
                {
                    t = t.BaseType;
                    t1 = Type.GetType(t.ToString() + "Dlg");
                }
                if (t1 != null)
                {
                    cm.Items.Add(new MenuItem { Header = "Show Dialog" });
                    ((MenuItem)cm.Items[cm.Items.Count - 1]).Click += Mi_Click;
                }
            }
            r.ContextMenu = cm;
            cm.Closed += Cm_Closed;
        }

        private static void Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb)
                if (cb.Parent is StackPanel sp)
                    if (sp.Parent is ContextMenu cm)
                        cm.IsOpen = false;
        }

        static bool deleted = false;
        private static void Cm_Closed(object sender, RoutedEventArgs e)
        {
            if ((Keyboard.GetKeyStates(Key.Escape) & KeyStates.Down) > 0)
                return;
            if (deleted)
            {
                deleted = false;
            }
            else if (sender is ContextMenu cm)
            {
                int i = (int)cm.GetValue(AreaNumberProperty);
                string label = "";
                string commandLine = "";
                Color color = Colors.Wheat;
                int width = 1, height = 1;

                Control cc = Utils.FindByName(cm, "AreaName");
                if (cc is TextBox tb)
                    label = tb.Text;
                cc = Utils.FindByName(cm, "AreaWidth");
                if (cc is TextBox tb1)
                    int.TryParse(tb1.Text, out width);
                cc = Utils.FindByName(cm, "AreaHeight");
                if (cc is TextBox tb2)
                    int.TryParse(tb2.Text, out height);
                cc = Utils.FindByName(cm, "AreaType");
                if (cc is ComboBox cb && cb.SelectedValue != null)
                {
                    commandLine = (string)cb.SelectedValue;
                }
                if (commandLine == "") return;//something went wrong
                cc = Utils.FindByName(cm, "CommandParams");
                if (cc is TextBox tb3)
                    commandLine += " " + tb3.Text;
                if ((label == "new" || label == "") && commandLine != "")
                    label = commandLine;
                cc = Utils.FindByName(cm, "AreaColor");
                if (cc is ComboBox cb1)
                    color = ((SolidColorBrush)((ComboBoxItem)cb1.SelectedValue).Background).Color;
                if (label == " " && commandLine == " ") return;
                if (i >= 0)
                {
                    ModuleView theModuleView = MainWindow.theNeuronArray.modules[i];
                    //update the existing module
                    theModuleView.Label = label;
                    theModuleView.CommandLine = commandLine;
                    theModuleView.Color = Utils.ColorToInt(color);

                    //did we change the module type?
                    string[] Params = commandLine.Split(' ');
                    Type t1x = Type.GetType("BrainSimulator.Modules." + Params[0]);
                    if (t1x != null && (MainWindow.theNeuronArray.modules[i].TheModule == null || MainWindow.theNeuronArray.modules[i].TheModule.GetType() != t1x))
                    {
                        MainWindow.theNeuronArray.modules[i].TheModule = (ModuleBase)Activator.CreateInstance(t1x);
                        MainWindow.theNeuronArray.modules[i].label = Params[0];
                    }

                    MainWindow.theNeuronArray.GetNeuronLocation(MainWindow.theNeuronArray.modules[i].firstNeuron, out int col, out int row);
                    if (width < theModuleView.TheModule.MinWidth) width = theModuleView.TheModule.MinWidth;
                    if (height < theModuleView.TheModule.MinHeight) height = theModuleView.TheModule.MinHeight;

                    bool dimsChanged = false;

                    if (width + col >= MainWindow.theNeuronArray.Cols)
                    {
                        width = MainWindow.theNeuronArray.Cols - col;
                        dimsChanged = true;
                    }
                    if (height + row >= MainWindow.theNeuronArray.rows)
                    {
                        height = MainWindow.theNeuronArray.rows - row;
                        dimsChanged = true;
                    }
                    if (dimsChanged)
                        MessageBox.Show("Dimensions reduced to stay within neuron array bondary.", "Warning", MessageBoxButton.OK);
                    theModuleView.Width = width;
                    theModuleView.Height = height;

                }
                else
                {
                    //convert a selection rectangle to a module
                    i = -i - 1;
                    width = MainWindow.arrayView.theSelection.selectedRectangles[i].Width;
                    height = MainWindow.arrayView.theSelection.selectedRectangles[i].Height;
                    NeuronSelectionRectangle nsr = MainWindow.arrayView.theSelection.selectedRectangles[i];
                    MainWindow.arrayView.theSelection.selectedRectangles.RemoveAt(i);
                    CreateModule(label, commandLine, color, nsr.FirstSelectedNeuron, width, height);
                }
            }
            MainWindow.Update();
        }

        public static void CreateModule(string label, string commandLine, Color color, int firstNeuron, int width, int height)
        {
            ModuleView na = new ModuleView(firstNeuron, width, height, label, commandLine, Utils.ColorToInt(color));
            if (na.Width < na.theModule.MinWidth) na.Width = na.theModule.MinWidth;
            if (na.Height < na.theModule.MinHeight) na.Height = na.theModule.MinHeight;
            MainWindow.theNeuronArray.modules.Add(na);
            string[] Params = commandLine.Split(' ');
            if (na.TheModule != null)
            {
                //MainWindow.theNeuronArray.areas[i].TheModule.Initialize(); //doesn't work with camera module
            }
            else
            {
                Type t1x = Type.GetType("BrainSimulator.Modules." + Params[0]);
                if (t1x != null && (na.TheModule == null || na.TheModule.GetType() != t1x))
                {
                    na.TheModule = (ModuleBase)Activator.CreateInstance(t1x);
                    //  MainWindow.theNeuronArray.areas[i].TheModule.Initialize();
                }
            }
        }

        private static void Mi_Click(object sender, RoutedEventArgs e)
        {
            //Handle delete  & initialize commands
            if (sender is MenuItem mi)
            {
                if ((string)mi.Header == "Delete")
                {
                    int i = (int)mi.Parent.GetValue(AreaNumberProperty);
                    if (i < 0)
                    {
                        i = -i - 1;
                        MainWindow.arrayView.theSelection.selectedRectangles.RemoveAt(i);
                        deleted = true;
                    }
                    else
                    {
                        ModuleView mv = MainWindow.theNeuronArray.Modules[i];
                        foreach (Neuron n in mv.Neurons())
                        {
                            n.Reset();
                            n.DeleteAllSynapes();
                        }
                        MainWindow.theNeuronArray.Modules.RemoveAt(i);
                        deleted = true;
                    }
                }
                if ((string)mi.Header == "Initialize")
                {
                    int i = (int)mi.Parent.GetValue(AreaNumberProperty);
                    if (i < 0)
                    {
                    }
                    else
                    {
                        MainWindow.theNeuronArray.Modules[i].TheModule.Initialize();
                    }
                }
                if ((string)mi.Header == "Show Dialog")
                {
                    int i = (int)mi.Parent.GetValue(AreaNumberProperty);
                    if (i < 0)
                    {
                    }
                    else
                    {
                        MainWindow.theNeuronArray.Modules[i].TheModule.ShowDialog();
                    }
                }
                if ((string)mi.Header == "Info...")
                {
                    int i = (int)mi.Parent.GetValue(AreaNumberProperty);
                    if (i < 0)
                    {
                    }
                    else
                    {
                        ModuleView m = MainWindow.theNeuronArray.Modules[i];
                        ModuleBase m1 = m.TheModule;
                        ModuleDescription md = new ModuleDescription(m1.ShortDescription, m1.LongDescription);
                        md.ShowDialog();
                    }
                }
                if ((string)mi.Header == "Reset Hebbian Weights")
                {
                    foreach (NeuronSelectionRectangle sr in MainWindow.arrayView.theSelection.selectedRectangles)
                    {
                        foreach (int Id in sr.NeuronInRectangle())
                        {
                            Neuron n = MainWindow.theNeuronArray.GetNeuron(Id);
                            foreach (Synapse s in n.Synapses)
                            {
                                if (s.IsHebbian)
                                    s.Weight = 0;
                            }
                        }
                    }
                    MainWindow.Update();
                }
            }
        }
    }
}
