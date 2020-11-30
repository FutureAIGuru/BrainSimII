//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BrainSimulator.Modules
{
    /// <summary>
    /// Interaction logic for Module2DSimDlg.xaml
    /// </summary>
    public partial class ModuleStrokeFinderDlg : ModuleBaseDlg
    {
        public ModuleStrokeFinderDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;

            ModuleStrokeFinder parent = (ModuleStrokeFinder)base.ParentModule;
            theCanvas.Children.Clear();
            Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            //Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            Point windowCenter = new Point(0,0);

            float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            if (scale == 0) return false;
            TransformGroup tg = new TransformGroup();
        //  tg.Children.Add(new RotateTransform(180));
            tg.Children.Add(new ScaleTransform(scale, scale, 0, 0));;
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
                scale = scale * .1f;
                foreach (ModuleStrokeFinder.Segment0 t in parent.lines)
                {
                    theCanvas.Children.Add(new Line
                    {
                        X1 = t.p1.X / scale,
                        X2 = t.p2.X / scale,
                        Y1 = t.p1.Y / scale,
                        Y2 = t.p2.Y / scale,
                        StrokeThickness = 0.5 / scale,
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        // Stroke = new SolidColorBrush(P1.TheColor)
                        Stroke = new SolidColorBrush(Colors.Green)
                    });
                }
                foreach (ModuleStrokeFinder.Stroke t in parent.strokes)
                {
                    theCanvas.Children.Add(new Line
                    {
                        X1 = t.p1.X / scale,
                        X2 = t.p2.X / scale,
                        Y1 = t.p1.Y / scale,
                        Y2 = t.p2.Y / scale,
                        StrokeThickness = 1 / scale,
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        // Stroke = new SolidColorBrush(P1.TheColor)
                        Stroke = new SolidColorBrush(Colors.Orange)
                    });
                }
            }
            catch { }
            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(true);
        }

    }
}
