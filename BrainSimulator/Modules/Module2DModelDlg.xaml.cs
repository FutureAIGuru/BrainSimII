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
    public partial class Module2DModelDlg : ModuleBaseDlg
    {
        public Module2DModelDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;

            Module2DModel parent = (Module2DModel)base.ParentModule;
            theCanvas.Children.Clear();
            Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);

            float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            if (scale == 0) return false;
            TransformGroup tg = new TransformGroup();
            tg.Children.Add(new RotateTransform(90));
            tg.Children.Add(new ScaleTransform(scale, -scale, 0, 0));// windowCenter.X, windowCenter.Y));
            tg.Children.Add(new TranslateTransform(windowCenter.X, windowCenter.Y));
            theCanvas.RenderTransform = tg;

            //draw an origin point
            theCanvas.Children.Add(new Line
            {
                X1 = -.20,
                X2 = .20,
                Y1 = 0,
                Y2 = 0,
                StrokeThickness = 1 / scale,
                Stroke = Brushes.Black
            });
            theCanvas.Children.Add(new Line
            {
                X1 = 0,
                X2 = 0,
                Y1 = -.20,
                Y2 = .20,
                StrokeThickness = 1 / scale,
                Stroke = Brushes.Black
            });

            //draw possible points;
            try
            {
                foreach (Thing t in parent.GetKBPoints() ?? Enumerable.Empty<Thing>())
                {
                    if (t.V is PointPlus P1 && !float.IsInfinity(P1.X) && !float.IsInfinity(P1.Y))
                    {
                        theCanvas.Children.Add(new Line
                        {
                            X1 = P1.X,
                            X2 = P1.X,
                            Y1 = P1.Y,
                            Y2 = P1.Y,
                            StrokeThickness = 3 / scale,
                            StrokeEndLineCap = PenLineCap.Round,
                            StrokeStartLineCap = PenLineCap.Round,
                            // Stroke = new SolidColorBrush(P1.TheColor)
                            Stroke = new SolidColorBrush(Colors.Orange)
                        });
                    }
                }
            }
            catch { }
            //draw the objects'
            try
            {
                foreach (Thing t in parent.GetKBSegments() ?? Enumerable.Empty<Thing>())
                {
                    Segment segment = Module2DModel.SegmentFromKBThing(t);
                    Color theColor = Utils.IntToColor(segment.theColor);
                    Point P1 = segment.P1.P;
                    Point P2 = segment.P2.P;
                    Point P1P = P1 + (P2 - P1) * segment.P1.Conf/2;// .2;
                    Point P2P = P1 + (P2 - P1) * (1- segment.P2.Conf/2);// .8;

                    theCanvas.Children.Add(new Line
                    {
                        X1 = P1.X,
                        X2 = P2.X,
                        Y1 = P1.Y,
                        Y2 = P2.Y,
                        StrokeThickness = 4 / scale,
                        Stroke = new SolidColorBrush(theColor),
                    });

                    if (segment.P1.Conf != 0)
                    {
                        theCanvas.Children.Add(new Line
                        {
                            X1 = P1.X,
                            X2 = P1P.X,
                            Y1 = P1.Y,
                            Y2 = P1P.Y,
                            StrokeThickness = 4 / scale,
                            Stroke = new SolidColorBrush(Colors.White),
                        });
                    }
                    if (segment.P2.Conf != 0)
                    {
                        theCanvas.Children.Add(new Line
                        {
                            X1 = P2.X,
                            X2 = P2P.X,
                            Y1 = P2.Y,
                            Y2 = P2P.Y,
                            StrokeThickness = 4 / scale,
                            Stroke = new SolidColorBrush(Colors.White),
                        });

                    }
                }
            }
            catch{}

            if (parent.imagining)
            {
                //draw any imagined objects
                for (int i = 0; i < parent.imagination.Count; i++)
                {
                    Color theColor = Utils.IntToColor(parent.imagination[i].theColor);
                    Point P1 = parent.imagination[i].P1.P;
                    Point P2 = parent.imagination[i].P2.P;
                    Point P1P = P1 + (P2 - P1) * .2;
                    Point P2P = P1 + (P2 - P1) * .8;

                    theCanvas.Children.Add(new Line
                    {
                        X1 = P1.X,
                        X2 = P2.X,
                        Y1 = P1.Y,
                        Y2 = P2.Y,
                        StrokeThickness = 4 / scale,
                        Stroke = new SolidColorBrush(theColor),
                        Opacity = .5
                    });
                }


                LinearGradientBrush lb = new LinearGradientBrush();
                lb.StartPoint = new Point(0, 1);
                lb.EndPoint = new Point(0, 0);
                lb.GradientStops.Add(new GradientStop(Colors.Transparent, 0.0));
                lb.GradientStops.Add(new GradientStop(Colors.Transparent, 0.4));
                lb.GradientStops.Add(new GradientStop(Colors.White, .76));
                lb.GradientStops.Add(new GradientStop(Colors.White, 1.0));
                Rectangle r = new Rectangle()
                {
                    Width = 40,
                    Height = 40,
                    Opacity = .75,
                    Fill = lb
                };
                Canvas.SetTop(r, -20);
                Canvas.SetLeft(r, -20);
                theCanvas.Children.Add(r);

            }
            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(true);
        }

    }
}
