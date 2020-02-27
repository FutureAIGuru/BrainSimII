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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BrainSimulator.Modules
{
    /// <summary>
    /// Interaction logic for Module2DSimDlg.xaml
    /// </summary>
    public partial class Module2DSimDlg : ModuleBaseDlg
    {
        public Module2DSimDlg()
        {
            InitializeComponent();
        }
        //this is here so the last change will cause a screen update after 1 second
        DispatcherTimer dt = null;
        private void Dt_Tick(object sender, EventArgs e)
        {
            dt.Stop(); ;
            Application.Current.Dispatcher.Invoke((Action)delegate { Draw(); });
        }

        float zoom = 1 / 12f;
        Vector pan = new Vector(0, 0);

        public override bool Draw()
        {
            if (!base.Draw())
            {
                if (dt == null)
                {
                    dt = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 100) };
                    dt.Tick += Dt_Tick;
                }
                dt.Stop();
                dt.Start();
                return false;
            }

            Module2DSim parent = (Module2DSim)base.ParentModule;

            //theCanvas.Children.RemoveRange(1, theCanvas.Children.Count-1);
            theCanvas.Children.Clear();
            Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            windowCenter += pan;
            float scale = (float)Math.Min(windowSize.X, windowSize.Y) * zoom;
            if (scale == 0) return false;

            TransformGroup tg = new TransformGroup();
            tg.Children.Add(new RotateTransform(90));
            tg.Children.Add(new ScaleTransform(scale, -scale, 0, 0));// windowCenter.X, windowCenter.Y));
            tg.Children.Add(new TranslateTransform(windowCenter.X, windowCenter.Y));
            theCanvas.RenderTransform = tg;


            //add a background
            Rectangle r = new Rectangle() { Height = parent.boundarySize * 2, Width = parent.boundarySize * 2, Stroke = Brushes.AliceBlue, Fill = Brushes.AliceBlue };
            Canvas.SetLeft(r, -parent.boundarySize);
            Canvas.SetTop(r, -parent.boundarySize);
            theCanvas.Children.Add(r);
            //draw the camera track...
            Polyline p = new Polyline();
            p.StrokeThickness = 1 / scale;
            p.Stroke = Brushes.Pink;
            for (int i = 0; i < parent.entityTrack.Count; i++)
            {
                p.Points.Add(
                    new Point(
                        parent.entityTrack[i].X,
                        parent.entityTrack[i].Y
                        )
                        );
            }
            theCanvas.Children.Add(p);

            //draw the objects
            for (int i = 0; i < parent.objects.Count; i++)
            {
                theCanvas.Children.Add(new Line
                {
                    X1 = parent.objects[i].P1.X,
                    X2 = parent.objects[i].P2.X,
                    Y1 = parent.objects[i].P1.Y,
                    Y2 = parent.objects[i].P2.Y,
                    StrokeThickness = 5 / scale,
                    Stroke = new SolidColorBrush(parent.objects[i].theColor)
                });
            }

            //draw the arms...
            if (parent.armActual.Length == 2)
            {
                for (int i = 0; i < parent.armActual.Length; i++)
                {
                    PointPlus c = new PointPlus
                    {
                        P = (Point)(parent.armActual[i] - parent.entityPosition)
                    };
                    double sa = c.R * .85; //lower arm lengths
                    //double sb = (c.R-sa) * 4; //upper arm lengths
                    //double sa = .45; // lower arm lengths
                    double sb = .3; // upper arm lengths
                    float a = (float)(Math.Acos((sb * sb + c.R * c.R - sa * sa) / (2 * sb * c.R)));
                    if (!Double.IsNaN(a))

                    {
                        PointPlus pa = new PointPlus
                        {
                            R = (float)sb,
                            Theta = c.Theta
                        };
                        if (i == 0) pa.Theta += a;
                        else pa.Theta -= a;
                        pa.P = (Point)(pa.P + (Vector)parent.entityPosition);
                        theCanvas.Children.Add(new Line
                        {
                            X1 = parent.entityPosition.X,
                            Y1 = parent.entityPosition.Y,
                            X2 = pa.P.X,
                            Y2 = pa.P.Y,
                            StrokeThickness = 2 / scale,
                            Stroke = Brushes.Black
                        });
                        theCanvas.Children.Add(new Line
                        {
                            X1 = pa.P.X,
                            Y1 = pa.P.Y,
                            X2 = parent.armActual[i].X,
                            Y2 = parent.armActual[i].Y,
                            StrokeThickness = 2 / scale,
                            Stroke = Brushes.Black
                        });
                    }
                }
            }

            //draw the current field of view
            for (int i = 0; i < parent.currentView0.Count; i++)
            {
                try //TODO lock the list
                {
                    theCanvas.Children.Add(new Line
                    {
                        X1 = parent.currentView0[i].P1.X,
                        Y1 = parent.currentView0[i].P1.Y,
                        X2 = parent.currentView0[i].P1.X,
                        Y2 = parent.currentView0[i].P1.Y,
                        StrokeThickness = 3 / scale,
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        Stroke = new SolidColorBrush(parent.currentView0[i].theColor)
                    });
                }
                catch { }
            }
            for (int i = 0; i < parent.currentView1.Count; i++)
            {
                try //another thread might mess the array up TODO, lock the object list
                {
                    theCanvas.Children.Add(new Line
                    {
                        X1 = parent.currentView1[i].P1.X,
                        Y1 = parent.currentView1[i].P1.Y,
                        X2 = parent.currentView1[i].P1.X,
                        Y2 = parent.currentView1[i].P1.Y,
                        StrokeThickness = 3 / scale,
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        Stroke = new SolidColorBrush(parent.currentView1[i].theColor)
                    });
                }
                catch { }
            }

            ////draw the body...it's a transparent gif
            Image body = new Image()
            {
                Source = new BitmapImage(new Uri("/Icons/entity.png", UriKind.Relative)),
                Width = 2 * parent.BodyRadius,
                Height = 2 * parent.BodyRadius
            };
            TransformGroup tg1 = new TransformGroup();
            tg1.Children.Add(new TranslateTransform(-parent.BodyRadius, -parent.BodyRadius));
            tg1.Children.Add(new RotateTransform(90 + parent.entityDirection1 * 180 / Math.PI));
            body.RenderTransform = tg1;
            Canvas.SetLeft(body, parent.entityPosition.X);
            Canvas.SetTop(body, parent.entityPosition.Y);
            theCanvas.Children.Add(body);

            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw();
        }

        private void TheCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Module2DSim parent = (Module2DSim)base.ParentModule;

            Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;


            Point position = e.GetPosition(theCanvas);

            PointPlus v = new PointPlus { P = (Point)(position - parent.entityPosition) };
            float dist = (float)v.R;
            double angle = (float)v.Theta;
            double deltaAngle = angle - parent.entityDirection1;
            ModuleView naGoToDest = MainWindow.theNeuronArray.FindAreaByLabel("ModuleGoToDest");
            if (naGoToDest != null)
            {
                naGoToDest.GetNeuronAt("Go").SetValue(1);
                naGoToDest.GetNeuronAt("Theta").SetValue((float)deltaAngle);
                naGoToDest.GetNeuronAt("R").SetValue(dist);
            }
            else
            {
                ModuleView naBehavior = MainWindow.theNeuronArray.FindAreaByLabel("ModuleBehavior");
                if (naBehavior != null)
                {
                    naBehavior.GetNeuronAt("TurnTo").SetValue(1);
                    naBehavior.GetNeuronAt("Theta").SetValue((float)-deltaAngle);
                    naBehavior.GetNeuronAt("MoveTo").SetValue(1);
                    naBehavior.GetNeuronAt("R").SetValue(dist);
                }
            }
        }

        private void TheCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            zoom += e.Delta / 12000f;
            if (zoom < 0.001) zoom = 0.001f;
            Draw();
        }

        Point prevPos = new Point(0, 0);
        private void TheCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point currentPosition = e.GetPosition(this);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Vector move = new Vector();
                move = (currentPosition - prevPos);
                pan += move;
            }
            prevPos = currentPosition;
        }
    }
}
