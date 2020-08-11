//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            Focusable = false;
        }

        float zoom = 1 / 12f;
        Vector pan = new Vector(0, 0);

        public override bool Draw(bool checkDrawTimer)
        {
            if (MainWindow.shiftPressed) theCanvas.Cursor = Cursors.Hand; else theCanvas.Cursor = Cursors.Arrow;
            if (!base.Draw(checkDrawTimer)) return false;

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
            tg.Children.Add(new ScaleTransform(scale, -scale, 0, 0));
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
                if (parent.objects[i].theColor != Colors.Black && parent.texture != 0)
                {
                    theCanvas.Children.Add(new Line
                    {
                        X1 = parent.objects[i].P1.X,
                        X2 = parent.objects[i].P2.X,
                        Y1 = parent.objects[i].P1.Y,
                        Y2 = parent.objects[i].P2.Y,
                        StrokeThickness = 4 / scale,
                        Stroke = new SolidColorBrush(parent.objects[i].theColor),
                    });
                    //dash the line
                    PointPlus P1 = new PointPlus(parent.objects[i].P1);
                    PointPlus P2 = new PointPlus(parent.objects[i].P2);
                    PointPlus delta = P2 - P1;
                    delta.R = .1f;
                    Segment s = new Segment(P1, P2, parent.objects[i].theColor);
                    for (int j = 1; j < 1 + s.Length * 10; j += 2)
                    {
                        PointPlus PStart = new PointPlus((Point)(P1.V + j * delta.V));
                        PointPlus PEnd = new PointPlus((Point)(P1.V + (j + .5f) * delta.V));
                        theCanvas.Children.Add(new Line
                        {
                            X1 = PStart.X,
                            X2 = PEnd.X,
                            Y1 = PStart.Y,
                            Y2 = PEnd.Y,
                            StrokeThickness = 4 / scale,
                            Stroke = new SolidColorBrush(Colors.AliceBlue),
                        });
                    }
                }
                else
                {
                    theCanvas.Children.Add(new Line
                    {
                        X1 = parent.objects[i].P1.X,
                        X2 = parent.objects[i].P2.X,
                        Y1 = parent.objects[i].P1.Y,
                        Y2 = parent.objects[i].P2.Y,
                        StrokeThickness = 4 / scale,
                        Stroke = new SolidColorBrush(parent.objects[i].theColor),
                    });
                }
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
            if ((bool)cbArcs.IsChecked)
            {
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
            }
            ////draw the body...it's a transparent gif
            Image body = new Image()
            {
                Source = new BitmapImage(new Uri("/Resources/entity.png", UriKind.Relative)),
                Width = 2 * parent.bodyRadius,
                Height = 2 * parent.bodyRadius
            };
            TransformGroup tg1 = new TransformGroup();
            tg1.Children.Add(new TranslateTransform(-parent.bodyRadius, -parent.bodyRadius));
            tg1.Children.Add(new RotateTransform(90 + parent.entityDirection1 * 180 / Math.PI));
            body.RenderTransform = tg1;
            Canvas.SetLeft(body, parent.entityPosition.X);
            Canvas.SetTop(body, parent.entityPosition.Y);
            theCanvas.Children.Add(body);

            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(true);
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
            e.Handled = true;
            if (!MainWindow.shiftPressed) return;
            zoom += e.Delta / 12000f;
            if (zoom < 0.001) zoom = 0.001f;
            Draw(true);
            MainWindow.thisWindow.Activate();
        }

        Point prevPos = new Point(0, 0);
        private void TheCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            if (!MainWindow.shiftPressed) return;
            Point currentPosition = e.GetPosition(this);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Vector move = new Vector();
                move = (currentPosition - prevPos);
                pan += move;
            }
            prevPos = currentPosition;
            MainWindow.thisWindow.Activate();
        }
        public void SetHand()
        {
            theCanvas.Cursor = Cursors.Hand;
        }
        public void ClearHand()
        {
            theCanvas.Cursor = Cursors.Arrow;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Module2DSim parent = (Module2DSim)base.ParentModule;
            parent.SetModel();
        }

        private void TheCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MainWindow.thisWindow.Activate();
        }

        private void ModuleBaseDlg_Loaded(object sender, RoutedEventArgs e)
        {
            Module2DSim parent = (Module2DSim)base.ParentModule;
            speedSlider.Value = parent.inMotion;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Module2DSim parent = (Module2DSim)base.ParentModule;
            parent.inMotion = (int)speedSlider.Value;
        }

        private void CbArcs_Click(object sender, RoutedEventArgs e)
        {
            Draw(false);
        }
    }
}
