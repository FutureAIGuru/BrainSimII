using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for FiringHistorWindow.xaml
    /// </summary>
    public partial class FiringHistoryWindow : Window
    {
        DispatcherTimer dt;
        bool FirstTime;

        public FiringHistoryWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            ShowInTaskbar = false;
            dt = new DispatcherTimer();
            dt.Interval = new TimeSpan(0, 0, 1);
            dt.Tick += Dt_Tick;
            dt.Start();
            FirstTime = true;
        }

        private void Dt_Tick(object sender, EventArgs e)
        {
            if (FirstTime)
                scroller.ScrollToRightEnd();
            FirstTime = false;
            Draw();
        }

        public void Draw()
        {
            //if you are not full-width or at end, don't repaint because it makes the image move around
            bool atRightEnd = scroller.HorizontalOffset + scroller.RenderSize.Width == scroller.ExtentWidth;
            bool fullWidth = scroller.RenderSize.Width == scroller.ExtentWidth;
            if (theCanvas.Children.Count != 0 && !atRightEnd && !fullWidth) return;
            if (dragging) return;
            ReallyDraw();
        }

        private void ReallyDraw()
        {
            theCanvas.Children.Clear();
            theCanvas2.Children.Clear();
            long maxX = MainWindow.theNeuronArray.Generation;
            long minX = FiringHistory.EarliestValue();
            if (minX <= maxX) //any samples?
            {
                Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
                double xScale = windowSize.X / (maxX - minX);
                double yDelta = windowSize.Y / (FiringHistory.history.Count + 1);

                for (int i = 0; i < FiringHistory.history.Count; i++)
                {
                    double yPos0 = yDelta * (i + 1);
                    double yPos1 = yPos0 - yDelta / 2;
                    double yPos2 = yPos0 + yDelta / 10;

                    Polyline pl = new Polyline()
                    {
                        Stroke = new SolidColorBrush(Colors.Black),
                        StrokeThickness = 4,
                        StrokeEndLineCap = PenLineCap.Round,
                    };
                    pl.Points.Add(new Point(0, yPos0));

                    for (int j = 0; j < FiringHistory.history[i].Samples.Count; j++)
                    {
                        double X = (FiringHistory.history[i].Samples[j]);
                        X -= minX;
                        X *= xScale;
                        pl.Points.Add(new Point(X, yPos0));
                        pl.Points.Add(new Point(X + xScale / 6, yPos1));
                        pl.Points.Add(new Point(X + xScale / 5, yPos1));
                        pl.Points.Add(new Point(X + xScale / 4, yPos2));
                        pl.Points.Add(new Point(X + xScale / 2, yPos0));
                    }

                    pl.Points.Add(new Point(theCanvas.Width, yPos0));
                    theCanvas.Children.Add(pl);
                    string label = "#" + FiringHistory.history[i].NeuronID.ToString();
                    if (MainWindow.theNeuronArray.GetNeuron(FiringHistory.history[i].NeuronID).Label != "")
                        label = MainWindow.theNeuronArray.GetNeuron(FiringHistory.history[i].NeuronID).Label;

                    Label l = new Label
                    {
                        Content = label,
                        Foreground = new SolidColorBrush(Colors.White)
                    };
                    l.MouseMove += L_MouseMove;
                    l.MouseLeftButtonUp += L_MouseLeftButtonUp;
                    Canvas.SetLeft(l, 10);
                    Canvas.SetTop(l, yPos1);
                    theCanvas2.Children.Add(l);
                }
            }
        }

        private void L_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point currentPosition = e.GetPosition(theCanvas2);
            if (dragging)
            {
                Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
                double yDelta = windowSize.Y / (FiringHistory.history.Count + 1);
                int newPos = (int)(currentPosition.Y / yDelta);
                if (sender is Label l)
                {
                    int old = theCanvas2.Children.IndexOf(l);
                    //Debug.WriteLine("Line " + newPos + "   " + l.Content + "  " + old);
                    FiringHistory.NeuronHistory temp = FiringHistory.history[old];
                    if (newPos > old) newPos--;
                    if (newPos >= 0 && newPos < FiringHistory.history.Count)
                    {
                        FiringHistory.history.RemoveAt(old);
                        if (newPos > FiringHistory.history.Count)
                            FiringHistory.history.Add(temp);
                        else
                            FiringHistory.history.Insert(newPos, temp);
                    }
                }
            }
            dragging = false;
            ReallyDraw();
        }

        bool dragging = false;
        Point prevPos;
        private void L_MouseMove(object sender, MouseEventArgs e)
        {
            Point currentPosition = e.GetPosition(theCanvas2);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                dragging = true;
                if (sender is Label l)
                {
                    l.CaptureMouse();
                    Point oldPos = new Point(Canvas.GetLeft(l),Canvas.GetTop(l));
                    Point newPos = oldPos + (currentPosition - prevPos);
                    Canvas.SetLeft(l, newPos.X);
                    Canvas.SetTop(l, newPos.Y);
                }
            }
            else
            {

            }
            prevPos = currentPosition;
        }

        double wheelScale = 0;
        private void TheCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            wheelScale += e.Delta / 120;
            if (wheelScale < 0) wheelScale = 0;
            theCanvas.Width = scroller.ActualWidth * (1 + wheelScale);
            theCanvas.Children.Clear();
            ReallyDraw();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            FiringHistory.Clear();
            wheelScale = 0;
            theCanvas.Width = scroller.ActualWidth;
            scroller.ScrollToRightEnd();
            ReallyDraw();
        }

        private void RemoveTags_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < FiringHistory.history.Count; i++)
            {
                int nID = FiringHistory.history[i].NeuronID;
                MainWindow.theNeuronArray.GetNeuron(nID).KeepHistory = false;
            }
            FiringHistory.history.Clear();
            this.Close();
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (scroller.ActualWidth > theCanvas.Width)
            {
                theCanvas.Width = Width;
                scroller.ScrollToRightEnd();
            }
            ReallyDraw();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Top = Owner.Top + Owner.Height - this.ActualHeight - 5;
            this.Left = Owner.Left + Owner.Width - this.ActualWidth - 5;
        }
    }
}
