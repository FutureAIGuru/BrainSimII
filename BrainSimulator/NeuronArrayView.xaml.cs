using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Threading;
using System.Collections;
using System.Reflection;


namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for NeuronArrayView.xaml
    /// </summary>
    /// 

    public partial class NeuronArrayView : UserControl
    {

        public enum MouseMode { pan, neuron, synapse, select };
        MouseMode theMouseMode = MouseMode.pan;

        private DisplayParams dp = new DisplayParams(); //refactor these back to private

        Point lastPosition = new Point(0, 0); //temp position used for calculating pan positions
        Point lastPosition1 = new Point(0, 0); //temp position used for calculating pan positions

        public NeuronArray myClipBoard = null; //refactor back to private
        public int targetNeuronIndex = -1;

        //needed for handling selections of areas of neurons
        Rectangle dragRectangle = null;
        int firstSelectedNeuron = -1;
        int lastSelectedNeuron = -1;

        //keeps track of the multiple selection areas
        public NeuronSelection theSelection = new NeuronSelection();

        //temporary for panning the canvas
        Vector CanvasOffset = new Vector(0, 0);

        //these are used to handle scaling with the mouse wheel
        DispatcherTimer dt = new DispatcherTimer();
        float scale = 1;

        //this helper keeps track of the neurons on the screen so they can change color without repainting
        private List<NeuronOnScreen> neuronsOnScreen = new List<NeuronOnScreen>();
        public class NeuronOnScreen
        {
            public int neuronIndex; public UIElement graphic; public float prevValue;
            public NeuronOnScreen(int index, UIElement e, float value)
            {
                neuronIndex = index; graphic = e; prevValue = value;
            }
        };

        public MouseMode TheMouseMode
        {
            get { return theMouseMode; }
            set
            {
                theMouseMode = value;
                NeuronView.theMouseMode = theMouseMode;
                SynapseView.theMouseMode = theMouseMode;
                Update();
            }
        }

        public NeuronArrayView()
        {
            InitializeComponent();
            dt.Tick += Dt_Tick;
        }

        public static readonly DependencyProperty AreaNumberProperty =
            DependencyProperty.Register("AreaNumber", typeof(int), typeof(MenuItem));
        public int AreaNumber
        {
            get { return (int)GetValue(AreaNumberProperty); }
            set { SetValue(AreaNumberProperty, value); }
        }
        //refresh the display of the neuron network
        public void Update()
        {
            if (MainWindow.theNeuronArray == null) return;
            Debug.WriteLine("Update " + MainWindow.theNeuronArray.Generation);
            dp.NeuronRows = MainWindow.theNeuronArray.rows;
            theCanvas.Children.Clear();
            neuronsOnScreen.Clear();

            //draw any areas in the array
            {
                for (int i = 0; i < MainWindow.theNeuronArray.Areas.Count; i++)
                {
                    NeuronArea nr = MainWindow.theNeuronArray.Areas[i];
                    NeuronSelectionRectangle nsr = new NeuronSelectionRectangle(MainWindow.theNeuronArray.rows, nr.FirstNeuron, nr.LastNeuron);
                    Rectangle r = nsr.GetRectangle(dp);
                    r.Fill = new SolidColorBrush(Utils.FromArgb(nr.Color));
                    theCanvas.Children.Add(r);
                    TextBlock tb = new TextBlock();
                    tb.Text = nr.Label;
                    tb.Background = new SolidColorBrush(Colors.White);
                    Canvas.SetLeft(tb, Canvas.GetLeft(r));
                    Canvas.SetTop(tb, Canvas.GetTop(r));
                    if (theMouseMode == MouseMode.select)
                        Canvas.SetZIndex(tb, 100);
                    theCanvas.Children.Add(tb);
                    CreateContextMenu(i, nr.Label, nr.CommandLine, r, nr.Color);
                }
            }
            //draw any selection rectangle(s)
            for (int i = 0; i < theSelection.selectedRectangle.Length; i++)
                if (theSelection.selectedRectangle[i] != null)
                {
                    Rectangle r = theSelection.selectedRectangle[i].GetRectangle(dp);
                    r.Fill = new SolidColorBrush(Colors.Pink);
                    theCanvas.Children.Add(r);
                    CreateContextMenu(-i - 1, "new", "", r, 0);
                }

            //highlight the "target" neuron
            if (targetNeuronIndex != -1)
            {
                Rectangle r = new Rectangle();
                Point p1 = dp.pointFromNeuron(targetNeuronIndex);
                r.Width = r.Height = dp.NeuronDisplaySize;
                Canvas.SetTop(r, p1.Y);
                Canvas.SetLeft(r, p1.X);
                r.Fill = new SolidColorBrush(Colors.LightBlue);
                theCanvas.Children.Add(r);
            }

            //this kludge puts the synapses in front of the neurons in Synapse mode only
            //draw the neurons
            if (theMouseMode == MouseMode.synapse)
                if (dp.NeuronDisplaySize > 5)
                {
                    for (int i = 0; i < MainWindow.theNeuronArray.neuronArray.Length; i++)
                    {
                        UIElement l = NeuronView.GetNeuronView(i, this);
                        if (l != null)
                        {
                            int element = theCanvas.Children.Add(l);
                            if (l is Ellipse || l is Rectangle)
                                neuronsOnScreen.Add(new NeuronOnScreen(i, l, 0));
                            else if (l is Label)
                                neuronsOnScreen.Add(new NeuronOnScreen(i, l, 0));
                        }
                    }
                }
            //draw the synapses
            if (dp.NeuronDisplaySize > 35)
                for (int i = 0; i < MainWindow.theNeuronArray.neuronArray.Length; i++)
                {
                    Point p1 = dp.pointFromNeuron(i);
                    Neuron n = MainWindow.theNeuronArray.neuronArray[i];
                    foreach (Synapse s in n.Synapses)
                    {
                        Shape l = SynapseView.SynapseDisplay(i, p1, s, this);
                        if (l != null)
                            theCanvas.Children.Add(l);
                    }
                }

            //draw the neurons
            if (theMouseMode != MouseMode.synapse)
                if (dp.NeuronDisplaySize > 5)
                {
                    for (int i = 0; i < MainWindow.theNeuronArray.neuronArray.Length; i++)
                    {
                        UIElement l = NeuronView.GetNeuronView(i, this);
                        if (l != null)
                        {
                            int element = theCanvas.Children.Add(l);
                            //if (l is Ellipse e && e.Fill.Opacity != 0)
                            if (l is Ellipse || l is Rectangle)
                                neuronsOnScreen.Add(new NeuronOnScreen(i, l, 0));
                            else if (l is Label)
                                neuronsOnScreen.Add(new NeuronOnScreen(i, l, 0));
                        }
                    }
                }
            Debug.WriteLine("Update Done");
        }

        private void CreateContextMenu(int i, string Label, string CommandLine, Rectangle r, int theColor)
        {
            if (TheMouseMode == MouseMode.select)
            {
                r.MouseDown += theCanvas_MouseDown;
                ContextMenu cm = new ContextMenu();
                cm.SetValue(AreaNumberProperty, i);
                MenuItem mi = new MenuItem();
                mi.Header = "Delete";
                mi.Click += Mi_Click;
                cm.Items.Add(mi);
                MenuItem mi1 = new MenuItem();
                mi1.Header = "Name:";
                mi1.IsEnabled = false;
                mi1.Click += Mi_Click;
                cm.Items.Add(mi1);
                TextBox tb1 = new TextBox();
                tb1.Text = Label;
                tb1.Width = 200;
                cm.Items.Add(tb1);
                MenuItem mi2 = new MenuItem();
                mi2.Header = "Command Line:";
                mi2.IsEnabled = false;
                cm.Items.Add(mi2);
                ComboBox cb = new ComboBox();
                Type theType = MainWindow.theNeuronArray.GetType();
                MethodInfo[] Methods = theType.GetMethods(); //this will list the available functions (with some effort)

                Array.Sort(Methods, delegate (MethodInfo x, MethodInfo y) { return x.Name.CompareTo(y.Name); }); ;
                foreach (MethodInfo m in Methods)
                {
                    ParameterInfo[] p = m.GetParameters();
                    if (p.Length == 1 && p[0].Name == "na")
                    {
                        cb.Items.Add(m.Name);
                    }
                }
                string cm1 = CommandLine;
                if (cm1.IndexOf(" ") > 0) cm1 = cm1.Substring(0, cm1.IndexOf(" "));
                cb.SelectedValue = cm1;
                cb.Width = 200;
                cm.Items.Add(cb);

                TextBox tb2 = new TextBox();
                tb2.Text = "";
                if (CommandLine.IndexOf(" ") > 0) tb2.Text = CommandLine.Substring(CommandLine.IndexOf(" ") + 1);
                tb2.Width = 200;
                cm.Items.Add(tb2);

                //color picker
                Color c = Utils.FromArgb(theColor);
                cb = new ComboBox();
                cb.Width = 200;
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

                r.ContextMenu = cm;
                cm.Closed += Cm_Closed;
            }
        }

        bool deleted = false;
        private void Cm_Closed(object sender, RoutedEventArgs e)
        {
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
                if (cm.Items[2] is TextBox t)
                    label = t.Text;
                if (cm.Items[4] is ComboBox cb)
                    commandLine = (string)cb.SelectedValue;
                if (cm.Items[5] is TextBox t1)
                    commandLine += " " + t1.Text;
                if (label == "new" && commandLine != "")
                    label = commandLine;
                if (cm.Items[6] is ComboBox cb1)
                    color = ((SolidColorBrush)((ComboBoxItem)cb1.SelectedValue).Background).Color;

                if (i >= 0)
                {
                    MainWindow.theNeuronArray.areas[i].Label = label;
                    MainWindow.theNeuronArray.areas[i].CommandLine = commandLine;
                    MainWindow.theNeuronArray.areas[i].Color = Utils.ToArgb(color);
                }
                else
                {
                    i = -i - 1;
                    NeuronSelectionRectangle nsr = theSelection.selectedRectangle[i];
                    theSelection.selectedRectangle[i] = null;
                    NeuronArea na = new NeuronArea(nsr.FirstSelectedNeuron, nsr.LastSelectedNeuron, label, commandLine, Utils.ToArgb(color));
                    MainWindow.theNeuronArray.areas.Add(na);
                }
            }
            Update();
        }

        private void Mi_Click(object sender, RoutedEventArgs e)
        {
            //Handle delete command
            if (sender is MenuItem mi)
            {
                int i = (int)mi.Parent.GetValue(AreaNumberProperty);
                if (i < 0)
                {
                    i = -i - 1;
                    theSelection.selectedRectangle[i] = null;
                    deleted = true;
                }
                else
                {
                    MainWindow.theNeuronArray.Areas.RemoveAt(i);
                    deleted = true;
                }
            }
        }

        //just update the colors of the neurons based on their current charge
        public void UpdateNeuronColors()
        {
            for (int i = 0; i < neuronsOnScreen.Count; i++)
            {
                NeuronOnScreen a = neuronsOnScreen[i];
                if (a.graphic is Shape e)
                {
                    Neuron n = MainWindow.theNeuronArray.neuronArray[a.neuronIndex];
                    if (n.LastCharge != a.prevValue)
                    {
                        if (NeuronView.GetNeuronView(a.neuronIndex, this) is Shape l)
                        {
                            e.Fill = l.Fill;
                            neuronsOnScreen[i].prevValue = n.LastCharge;
                        }
                    }
                }
            }
            MainWindow.UpdateDisplayLabel(dp.NeuronDisplaySize, (int)MainWindow.theNeuronArray.lastFireCount);
        }


        public void CancelSynapseRubberband()
        {
            mouseDownNeuronIndex = -1;
            if (synapseShape != null)
                theCanvas.Children.Remove(synapseShape);
            synapseShape = null;
        }

        int mouseDownNeuronIndex = -1;
        static Shape synapseShape = null;  //the shape of the synapses being rubber-banded 
                                           //(so it can be added/removed easily from the canvas)

        int MouseInArea = -1;
        public void theCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (MainWindow.theNeuronArray == null) return;
            Debug.WriteLine("theCanvas_MouseDown" + MainWindow.theNeuronArray.Generation);
            Point currentPosition = e.GetPosition(theCanvas);
            LimitMousePostion(ref currentPosition);
            mouseDownNeuronIndex = dp.NeuronFromPoint(currentPosition);

            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (sender is Shape s && s.ContextMenu != null)
                {
                    s.ContextMenu.IsOpen = true;
                    e.Handled = true;
                }
                else
                {
                    //labels don't seem to get context-menu hits so we'll emulate them
                    NeuronOnScreen nOnScreen = neuronsOnScreen.Find(x => x.neuronIndex == mouseDownNeuronIndex);
                    if (nOnScreen != null && nOnScreen.graphic is Label ll && ll.ContextMenu != null)
                    {
                        ll.ContextMenu.IsOpen = true;
                        e.Handled = true;
                    }
                    else if (TheMouseMode == MouseMode.select)
                    {//this copes with the problem of getting a hit to the rectangle when the mouse was clicked
                        //on a neuron in front of the rectangle ... doesn't work with selection areas (only modules)
                        if (MouseInArea != -1)
                        {
                            Rectangle r = theCanvas.Children[MouseInArea*2] as Rectangle;
                            if (r != null)
                            { if (r.ContextMenu != null) r.ContextMenu.IsOpen=true; }
                        }
                        //need to add case of mouse in selection
                    }
                }
                return;
            }
            Neuron n = null;
            if (mouseDownNeuronIndex >= 0 && mouseDownNeuronIndex < MainWindow.theNeuronArray.neuronArray.Length)
                n = MainWindow.theNeuronArray.neuronArray[mouseDownNeuronIndex] as Neuron;

            //this duplicates the code of pan mode
            if (Mouse.MiddleButton == MouseButtonState.Pressed)
            {
                lastPosition1 = e.GetPosition((UIElement)theCanvas.Parent);
                Mouse.Capture(theCanvas);
            }
            else switch (TheMouseMode)
                {
                    case MouseMode.select:
                        if (theCanvas.Cursor == Cursors.Cross)
                        {
                            Debug.WriteLine("dragStart" + MainWindow.theNeuronArray.Generation);
                            if (dragRectangle != null)
                            {
                                theCanvas.Children.Remove(dragRectangle);
                            }

                            if (!MainWindow.crtlPressed)
                            {
                                theSelection.ClearSelection();
                            }
                            else Update();

                            //snap to neuron point
                            currentPosition = dp.pointFromNeuron(mouseDownNeuronIndex);

                            //build the draggable selection rectangle
                            dragRectangle = new Rectangle();
                            dragRectangle.Width = dragRectangle.Height = dp.NeuronDisplaySize;
                            dragRectangle.Stroke = new SolidColorBrush(Colors.Red);
                            dragRectangle.Fill = new SolidColorBrush(Colors.Red);
                            dragRectangle.Fill.Opacity = 0.5;
                            Canvas.SetLeft(dragRectangle, currentPosition.X);
                            Canvas.SetTop(dragRectangle, currentPosition.Y);
                            theCanvas.Children.Add(dragRectangle);
                            firstSelectedNeuron = mouseDownNeuronIndex;
                            Mouse.Capture(theCanvas);
                        }
                        break;
                    case MouseMode.neuron:
                        if (n != null)
                        {
                            if (n.LastCharge < .99)
                                n.CurrentCharge = 1;
                            else
                                n.CurrentCharge = n.LastCharge = 0;
                            e.Handled = true;
                        }
                        break;
                    case MouseMode.synapse:
                        Mouse.Capture(theCanvas);
                        break;
                    case MouseMode.pan:
                        lastPosition1 = e.GetPosition((UIElement)theCanvas.Parent);
                        Mouse.Capture(theCanvas);
                        break;
                }
        }

        public float lastSynapseWeight = 1.0f;
        public void theCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (MainWindow.theNeuronArray == null) return;
            Debug.WriteLine("theCanvas_MouseUp" + MainWindow.theNeuronArray.Generation);
            if (e.RightButton == MouseButtonState.Pressed) return;

            //this duplicates the code of pan mode
            if (e.ChangedButton == MouseButton.Middle)
            {
                theCanvas.RenderTransform = new TranslateTransform(0, 0);
                dp.DisplayOffset += CanvasOffset;
                CanvasOffset = new Vector(0, 0);
                e.Handled = true;
                lastPosition = new Point(0, 0);
                Update();
            }
            else switch (TheMouseMode)
                {
                    case MouseMode.select:
                        FinishSelection();
                        Update();
                        break;
                    case MouseMode.neuron:
                        break;
                    case MouseMode.synapse:
                        if (mouseDownNeuronIndex > -1 && theMouseMode == NeuronArrayView.MouseMode.synapse)
                        {
                            Point p1 = e.GetPosition(theCanvas);
                            LimitMousePostion(ref p1);
                            int index = dp.NeuronFromPoint(p1);
                            MainWindow.theNeuronArray.neuronArray[mouseDownNeuronIndex].AddSynapse(index, lastSynapseWeight, MainWindow.theNeuronArray);
                        }
                        synapseShape = null;
                        mouseDownNeuronIndex = -1;
                        e.Handled = true;
                        Update();
                        break;
                    case MouseMode.pan:
                        theCanvas.RenderTransform = new TranslateTransform(0, 0);
                        dp.DisplayOffset += CanvasOffset;
                        CanvasOffset = new Vector(0, 0);
                        e.Handled = true;
                        Update();
                        break;
                }
            mouseDownNeuronIndex = -1;
            Mouse.Capture(null);
        }

        private void FinishSelection()
        {
            if (dragRectangle != null)
            {
                try
                {
                    //get the neuron pointers from the drag rectangle and save in the selection array
                    Point p1 = new Point(Canvas.GetLeft(dragRectangle), Canvas.GetTop(dragRectangle));
                    Point p2 = new Point(p1.X + dragRectangle.Width - 1, p1.Y + dragRectangle.Height - 1);
                    firstSelectedNeuron = dp.NeuronFromPoint(p1);
                    lastSelectedNeuron = dp.NeuronFromPoint(p2);
                    Debug.Write(firstSelectedNeuron + ", " + lastSelectedNeuron);
                    NeuronSelectionRectangle rr = new NeuronSelectionRectangle(MainWindow.theNeuronArray.rows, firstSelectedNeuron, lastSelectedNeuron);
                    for (int i = 0; i < theSelection.selectedRectangle.Length; i++)
                        if (theSelection.selectedRectangle[i] == null)
                        {
                            theSelection.selectedRectangle[i] = rr;
                            break;
                        }
                }
                catch
                {
                    dragRectangle = null;
                }
                dragRectangle = null;
            }
        }

        //keep any mouse operations within the bounds of the neruon array
        private void LimitMousePostion(ref Point p1)
        {
            if (p1.X < dp.DisplayOffset.X) p1.X = dp.DisplayOffset.X;
            if (p1.Y < dp.DisplayOffset.Y) p1.Y = dp.DisplayOffset.Y;
            float width = dp.NeuronDisplaySize * (MainWindow.theNeuronArray.neuronArray.Length / dp.NeuronRows - 1);
            float height = dp.NeuronDisplaySize * dp.NeuronRows - 1;
            if (p1.X > dp.DisplayOffset.X + width) p1.X = dp.DisplayOffset.X + width;
            if (p1.Y > dp.DisplayOffset.Y + height) p1.Y = dp.DisplayOffset.Y + height;

        }

        NeuronArea na = null;
        public void theCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (MainWindow.theNeuronArray == null) return;
            if (e.RightButton == MouseButtonState.Pressed) return;

            Point currentPosition = e.GetPosition(theCanvas);
            LimitMousePostion(ref currentPosition);
            int currentNeuron = dp.NeuronFromPoint(currentPosition);

            //this duplicates the code of pan mode
            if (Mouse.MiddleButton == MouseButtonState.Pressed)
            {
                if (lastPosition != new Point(0, 0))
                {
                    //this is the calculation used for updates
                    dp.DisplayOffset += currentPosition - lastPosition;

                    //this shifts the display with a transform (but doesn't repaint to fill in the edges
                    Point CurrentPosition1 = e.GetPosition((UIElement)theCanvas.Parent);
                    Vector v = lastPosition1 - CurrentPosition1;
                    if (v.X != 0 || v.Y != 0)
                    {
                        CanvasOffset -= v;
                        TranslateTransform tt = new TranslateTransform(CanvasOffset.X, CanvasOffset.Y);
                        theCanvas.RenderTransform = tt;
                    }
                }
                lastPosition = currentPosition;
                lastPosition1 = e.GetPosition((UIElement)theCanvas.Parent);
            }
            else switch (TheMouseMode)
                {
                    case MouseMode.select:
                        //set the cursor if are we inside an existing module rectangle?
                        if (e.LeftButton == MouseButtonState.Released)
                        {
                            MouseInArea = -1;
                            for (int i = 0; i < MainWindow.theNeuronArray.areas.Count; i++)
                            {
                                na = MainWindow.theNeuronArray.areas[i];
                                MainWindow.theNeuronArray.GetNeuronLocation(currentNeuron, out int x, out int y);
                                na.GetBounds(out int X1, out int Y1, out int X2, out int Y2);
                                X2--; Y2--;
                                if (x >= X1 && x <= X2 && y >= Y1 && y <= Y2)
                                {
                                    MouseInArea = i;
                                    firstSelectedNeuron = currentNeuron;
                                    if (x == X1 && y == Y1)
                                    {
                                        theCanvas.Cursor = Cursors.ScrollNW;
                                        break;
                                    }
                                    else if (x == X1 && y == Y2)
                                    {
                                        theCanvas.Cursor = Cursors.ScrollSW;
                                        break;
                                    }
                                    else if (x == X2 && y == Y1)
                                    {
                                        theCanvas.Cursor = Cursors.ScrollNE;
                                        break;
                                    }
                                    else if (x == X2 && y == Y2)
                                    {
                                        theCanvas.Cursor = Cursors.ScrollSE;
                                        break;
                                    }
                                    else if (x == X1)
                                    {
                                        theCanvas.Cursor = Cursors.ScrollW;
                                        break;
                                    }
                                    else if (x == X2)
                                    {
                                        theCanvas.Cursor = Cursors.ScrollE;
                                        break;
                                    }
                                    else if (y == Y1)
                                    {
                                        theCanvas.Cursor = Cursors.ScrollN;
                                        break;
                                    }
                                    else if (y == Y2)
                                    {
                                        theCanvas.Cursor = Cursors.ScrollS;
                                        break;
                                    }
                                    else
                                    {
                                        theCanvas.Cursor = Cursors.ScrollAll;
                                        break;
                                    }
                                }
                                else
                                    theCanvas.Cursor = Cursors.Cross;
                            }
                        }
                        //sometimes mouseup events are lost...because the underlying canvas is updated
                        if (!(e.LeftButton == MouseButtonState.Pressed))
                        {
                            FinishSelection();
                            break;
                        }
                        if (theCanvas.Cursor == Cursors.Cross)
                        {
                            if (dragRectangle == null)
                                break;

                            //swap to keep the rectangle in positive territory
                            Point p1 = dp.pointFromNeuron(firstSelectedNeuron);
                            Point p2 = dp.pointFromNeuron(currentNeuron);
                            if (p1.X > p2.X)
                            {
                                double temp = p1.X; p1.X = p2.X; p2.X = temp;
                            }
                            if (p1.Y > p2.Y)
                            {
                                double temp = p1.Y; p1.Y = p2.Y; p2.Y = temp;
                            }

                            //snap mouse position to neuron
                            currentNeuron = dp.NeuronFromPoint(p2);
                            p2 = dp.pointFromNeuron(currentNeuron);

                            //update graphic rectangle 
                            dragRectangle.Width = p2.X - p1.X + dp.NeuronDisplaySize;
                            dragRectangle.Height = p2.Y - p1.Y + dp.NeuronDisplaySize;
                            Canvas.SetLeft(dragRectangle, p1.X);
                            Canvas.SetTop(dragRectangle, p1.Y);
                            if (!theCanvas.Children.Contains(dragRectangle)) theCanvas.Children.Add(dragRectangle);
                        }
                        else if (na != null)
                        {
                            if (theCanvas.Cursor == Cursors.ScrollAll)
                            {
                                if (currentNeuron != firstSelectedNeuron)
                                {
                                    int newFirst = na.FirstNeuron + currentNeuron - firstSelectedNeuron;
                                    int newLast = na.LastNeuron + currentNeuron - firstSelectedNeuron;
                                    na.GetAbsNeuronLocation(newFirst, out int xf, out int yf);
                                    na.GetAbsNeuronLocation(newLast, out int xl, out int yl);
                                    if (newFirst > 0 && newLast < MainWindow.theNeuronArray.arraySize &&
                                        xf < xl && yf < yl)
                                    {
                                        na.FirstNeuron += currentNeuron - firstSelectedNeuron;
                                        na.LastNeuron += currentNeuron - firstSelectedNeuron;
                                    }
                                    firstSelectedNeuron = currentNeuron;
                                    Update();
                                }
                            }
                            //top
                            na.GetBounds(out int X1, out int Y1, out int X2, out int Y2);
                            na.GetAbsNeuronLocation(firstSelectedNeuron, out int Xf, out int Yf);
                            na.GetAbsNeuronLocation(currentNeuron, out int Xc, out int Yc);
                            na.GetAbsNeuronLocation(na.LastNeuron, out int Xl, out int Yl);

                            if (theCanvas.Cursor == Cursors.ScrollN ||
                                theCanvas.Cursor == Cursors.ScrollNE ||
                                theCanvas.Cursor == Cursors.ScrollNW)
                            {
                                if (Yc != Yf)
                                {
                                    int newTop = Y1 + Yc - Yf;
                                    if (newTop < Y2)
                                    {
                                        na.FirstNeuron += Yc - Yf;
                                        firstSelectedNeuron = currentNeuron;
                                        Update();
                                    }
                                }
                            }
                            if (theCanvas.Cursor == Cursors.ScrollW ||
                                theCanvas.Cursor == Cursors.ScrollNW ||
                                theCanvas.Cursor == Cursors.ScrollSW)
                            {
                                if (Xc != Xf)
                                {
                                    int newLeft = X1 + Xc - Xf;
                                    if (newLeft < X2)
                                    {
                                        na.FirstNeuron += (Xc - Xf) * MainWindow.theNeuronArray.rows;
                                        firstSelectedNeuron = currentNeuron;
                                        Update();
                                    }
                                }
                            }
                            if (theCanvas.Cursor == Cursors.ScrollS ||
                                theCanvas.Cursor == Cursors.ScrollSE ||
                                theCanvas.Cursor == Cursors.ScrollSW)
                            {
                                if (Yc != Yf)
                                {
                                    int newBottom = Y2 + Yc - Yf;
                                    if (newBottom > Y1)
                                    {
                                        na.LastNeuron += Yc - Yf;
                                        firstSelectedNeuron = currentNeuron;
                                        Update();
                                    }
                                }
                            }
                            if (theCanvas.Cursor == Cursors.ScrollE ||
                                theCanvas.Cursor == Cursors.ScrollNE ||
                                theCanvas.Cursor == Cursors.ScrollSE)
                            {
                                if (Xc != Xf)
                                {
                                    int newRight = X2 + Xc - Xf;
                                    if (newRight > X1)
                                    {
                                        na.LastNeuron += (Xc - Xf) * MainWindow.theNeuronArray.rows;
                                        firstSelectedNeuron = currentNeuron;
                                        Update();
                                    }
                                }
                            }
                        }
                        break;
                    case MouseMode.neuron:
                        break;
                    case MouseMode.synapse:
                        if (e.LeftButton == MouseButtonState.Pressed)
                        {
                            if (mouseDownNeuronIndex < 0) break;
                            if (synapseShape != null)
                                theCanvas.Children.Remove(synapseShape);
                            Shape l = SynapseView.GetSynapseShape(dp.pointFromNeuron(mouseDownNeuronIndex), dp.pointFromNeuron(currentNeuron), this);
                            theCanvas.Children.Add(l);
                            synapseShape = l;
                        }
                        else //we may have missed a mouse-up event...clear out the rubber-banding
                        {
                            synapseShape = null;
                            mouseDownNeuronIndex = -1;
                        }
                        break;
                    case MouseMode.pan:
                        if (e.LeftButton == MouseButtonState.Pressed)
                        {
                            if (lastPosition != new Point(0, 0))
                            {
                                //this is the calculation used for updates
                                dp.DisplayOffset += currentPosition - lastPosition;

                                //this shifts the display with a transform (but doesn't repaint to fill in the edges
                                Point CurrentPosition1 = e.GetPosition((UIElement)theCanvas.Parent);
                                Vector v = lastPosition1 - CurrentPosition1;
                                if (v.X != 0 || v.Y != 0)
                                {
                                    CanvasOffset -= v;
                                    TranslateTransform tt = new TranslateTransform(CanvasOffset.X, CanvasOffset.Y);
                                    theCanvas.RenderTransform = tt;
                                }
                            }
                            lastPosition = currentPosition;
                            lastPosition1 = e.GetPosition((UIElement)theCanvas.Parent);
                        }
                        else
                        {
                            lastPosition = new Point(0, 0);
                        }
                        break;
                }
        }

        public void theCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //zoom in-out the display
            float oldNeuronDisplaySize = dp.NeuronDisplaySize;
            dp.NeuronDisplaySize += e.Delta / 120;
            if (dp.NeuronDisplaySize < 2) dp.NeuronDisplaySize = 2;
            Point mousePostion = e.GetPosition(theCanvas);
            Vector v = (Vector)mousePostion;
            v -= (Vector)dp.DisplayOffset;
            v *= dp.NeuronDisplaySize / oldNeuronDisplaySize;
            dp.DisplayOffset = mousePostion - v;

            scale *= dp.NeuronDisplaySize / oldNeuronDisplaySize;
            ScaleTransform st = new ScaleTransform(scale, scale, mousePostion.X, mousePostion.Y);
            theCanvas.RenderTransform = st;

            //start a timer to do an update so we don't get an update for every wheel click
            dt.Stop();
            dt.Interval = TimeSpan.FromMilliseconds(1000);
            dt.Start();
            MainWindow.UpdateDisplayLabel(dp.NeuronDisplaySize, 0);
        }

        private void Dt_Tick(object sender, EventArgs e)
        {
            dt.Stop();
            scale = 1;
            theCanvas.RenderTransform = new ScaleTransform(1, 1);
            Update();
        }

        //sets neuron 0 to the upper left of the neuron array display
        public void Origin()
        {
            dp.DisplayOffset = new Point(0, 0);
            dp.NeuronDisplaySize = 25;
            Update();
        }

        private void theCanvas_SizeChanged(object sender, SizeChangedEventArgs e)

        {
            NeuronView.theCanvas = theCanvas;//??  
            SynapseView.theCanvas = theCanvas;//??
            if (MainWindow.theNeuronArray == null) return;
            Update();
        }


        private void theCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            if (MainWindow.theNeuronArray == null) return;
        }

        private void TheCanvas_MouseEnter(object sender, MouseEventArgs e)
        {
            switch (TheMouseMode)
            {
                case MouseMode.select:
                    theCanvas.Cursor = Cursors.Cross;
                    break;
                case MouseMode.neuron:
                    theCanvas.Cursor = Cursors.UpArrow;
                    break;
                case MouseMode.synapse:
                    theCanvas.Cursor = Cursors.Arrow;
                    break;
                case MouseMode.pan:
                    theCanvas.Cursor = Cursors.Hand;
                    break;
            }

        }

        private void TheCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            theCanvas.Cursor = Cursors.Arrow;
        }

        //start with empty array??
        //problem of processing order in neuron areas
        //finish help-about page
        //right-click area selection broken when clicked in neuron
        //single step engine should stop engine
        //ctl key shortcuts...cut paste, copy delete, undo
        //standard weights on synapse context menu
        //UI Cleanup
        //optimize engine for bigger array (GPU) ?
        //Refactor the Main.neuron array
        //improve the "area" module dropdown to have module names, colors, parameters

        //networks: NAND, counter, closest match, shifter, delay, latch, big random file
        //XML documentation

        //learning: Add new pattern, tweak/heppign  automate from file, 
        //Decision/planning Tree
        //goals
        //cerebellum
        //proprioception


    }



}
