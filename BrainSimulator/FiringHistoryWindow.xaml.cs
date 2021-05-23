using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Diagnostics;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for FiringHistorWindow.xaml
    /// </summary>
    public partial class FiringHistoryWindow : Window
    {
        DispatcherTimer dt;
        bool FirstTime;
        int refractoryPeriod = 1;
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
            refractoryPeriod = MainWindow.theNeuronArray.RefractoryDelay;
            refractoryPeriod++;
            theCanvas.Children.Clear();
            theCanvas2.Children.Clear();
            long maxX = MainWindow.theNeuronArray.Generation;
            long minX = FiringHistory.EarliestValue();
            if (minX <= maxX) //any samples?
            {
                long sampleCount = maxX - minX;
                if (sampleCount > 100 && wheelScale == 0)
                {
                    theCanvas.Width = scroller.ActualWidth * (float)sampleCount / 100f;
                    scroller.ScrollToRightEnd();
                }
                Point windowSize = new Point(theCanvas.Width, theCanvas.ActualHeight);
                double xScale = windowSize.X / (float)sampleCount;
                double yDelta = .9 * windowSize.Y / (FiringHistory.history.Count);
                yDelta = Math.Min(yDelta, 200);

                //TODO:  The commented-out lines will create a smooth curve instead of the line approximation
                for (int i = 0; i < FiringHistory.history.Count; i++)
                {
                    double yPos0 = yDelta * (i + 1);
                    double yPos1 = yPos0 - yDelta * .85;
                    double yPos15 = yPos0 - yDelta * .875;
                    double yPos2 = yPos0 + yDelta / 20;
                    double yPos3 = yPos0 + yDelta / 10;
                    //PointCollection pc = new PointCollection();

                    Polyline pl = new Polyline()
                    {
                        Stroke = new SolidColorBrush(Colors.Black),
                        StrokeThickness = 4,
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeLineJoin = PenLineJoin.Round,
                    };
                    pl.Points.Add(new Point(0, yPos0));
                    //                    pc.Add(new Point(0, yPos0));

                    float lastValue = 0;
                    for (int j = 0; j < FiringHistory.history[i].Samples.Count; j++)
                    {
                        double X = (FiringHistory.history[i].Samples[j].generation);
                        float value = FiringHistory.history[i].Samples[j].value;
                        float value1 = value;
                        if (j < FiringHistory.history[i].Samples.Count - 1)
                            value1 = FiringHistory.history[i].Samples[j + 1].value;
                        X -= minX;
                        X *= xScale;
                        if (value >= 1)
                        {
                            double xDelta = xScale / 10;
                            xDelta *= refractoryPeriod;
                            if (lastValue == 1) lastValue = 0;
                            float yPosLastValue = (float)(yPos0 - lastValue * yDelta / 3);
                            pl.Points.Add(new Point(X, yPosLastValue));
                            pl.Points.Add(new Point(X + xDelta, yPos1));
                            pl.Points.Add(new Point(X + 1.1 * xDelta, yPos15));
                            pl.Points.Add(new Point(X + 1.2 * xDelta, yPos1));
                            pl.Points.Add(new Point(X + 2.2 * xDelta, yPos0));
                            pl.Points.Add(new Point(X + 2.5 * xDelta, yPos2));
                            pl.Points.Add(new Point(X + 3.5 * xDelta, yPos3));
                            pl.Points.Add(new Point(X + 4.2 * xDelta, yPos3));
                            pl.Points.Add(new Point(X + 6 * xDelta, yPos2));
                            pl.Points.Add(new Point(X + 9 * xDelta, yPos0));
                            j += refractoryPeriod-1;
                            //pc.Add(new Point(X, yPosLastValue));
                            //pc.Add(new Point(X + xDelta, yPos1));
                            //pc.Add(new Point(X + 2 * xDelta, yPos2));
                            //pc.Add(new Point(X + 3 * xDelta, yPos2));
                            //pc.Add(new Point(X + 5 * xDelta, yPos3));
                            //pc.Add(new Point(X + 9 * xDelta, yPos0));
                        }
                        else
                        {
                            float yPosValue = (float)(yPos0 - value * yDelta / 3);
                            pl.Points.Add(new Point(X, yPosValue));
                            if (value1 >= value)
                                pl.Points.Add(new Point(X + xScale * 9 / 10, yPosValue));
                            //pc.Add(new Point(X, yPosValue));
                        }
                        lastValue = value;
                    }

                    //pl.Points.Add(new Point(theCanvas.Width, yPos0));
                    //pc.Add(new Point(theCanvas.Width, yPos0));

                    //PolyBezierSegment pbs = new PolyBezierSegment { Points = pc };
                    //PathSegmentCollection psc = new PathSegmentCollection { pbs };
                    //PathFigure pf = new PathFigure { StartPoint = new Point(0, yPos0), Segments = psc };
                    //PathFigureCollection pfc = new PathFigureCollection { pf };
                    //PathGeometry pg = new PathGeometry { Figures = pfc };
                    //Path thePath = new Path { Stroke = Brushes.Green, StrokeThickness = 2, Data = pg };


                    theCanvas.Children.Add(pl);
                    //theCanvas.Children.Add(thePath);
                    string label = "#" + FiringHistory.history[i].NeuronID.ToString();
                    if (MainWindow.theNeuronArray.GetNeuron(FiringHistory.history[i].NeuronID).Label != "")
                        label = MainWindow.theNeuronArray.GetNeuron(FiringHistory.history[i].NeuronID).Label;

                    Label l = new Label
                    {
                        Content = label,
                        Foreground = new SolidColorBrush(Colors.White),
                        FontSize = yDelta / 3                    };
                    l.MouseMove += L_MouseMove;
                    l.MouseLeftButtonUp += L_MouseLeftButtonUp;
                    Canvas.SetLeft(l, 10);
                    Canvas.SetTop(l, yPos1);
                    theCanvas2.Children.Add(l);
                    theCanvas.Children.Add(new Line
                    {
                        X1 = 0,
                        X2 = theCanvas.Width,
                        Y1 = yPos0,
                        Y2 = yPos0,
                        Stroke = Brushes.Green,
                        StrokeDashArray = new DoubleCollection { 3, 5 },
                        StrokeThickness = 2,
                    });
                    theCanvas.Children.Add(new Line
                    {
                        X1 = 0,
                        X2 = theCanvas.Width,
                        Y1 = yPos0 - yDelta / 3,
                        Y2 = yPos0 - yDelta / 3,
                        Stroke = Brushes.Red,
                        StrokeDashArray = new DoubleCollection { 3, 5 },
                        StrokeThickness = 2,
                    });
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
                    Point oldPos = new Point(Canvas.GetLeft(l), Canvas.GetTop(l));
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
            wheelScale = theCanvas.Width / scroller.ActualWidth;
            Debug.WriteLine("Old WheelScale: " + wheelScale);
            wheelScale += e.Delta / 120;
            Debug.WriteLine("New WheelScale: " + wheelScale);
            if (wheelScale < 0) wheelScale = 0;
            Debug.WriteLine("Positive WheelScale: " + wheelScale);
            theCanvas.Width = scroller.ActualWidth * (1 + wheelScale);
            Debug.WriteLine("New CanvasWidth: " + wheelScale);
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
