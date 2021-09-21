//
// Copyright (c) [Name]. All rights reserved.  
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
    public partial class ModuleBoundarySegmentsDlg : ModuleBaseDlg
    {
        public ModuleBoundarySegmentsDlg()
        {
            InitializeComponent();
        }

        Random rnd = new Random();
        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second

            ModuleBoundarySegments parent = (ModuleBoundarySegments)base.ParentModule;
            theCanvas.Children.Clear();
            Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);

            var boudaries = parent.segments;
            if (boudaries.Count < 1) return false;
            double largest = 0;
            try
            {
                largest = boudaries.Max(a => new[] { a.p1.X, a.p2.X, a.p1.Y, a.p2.Y }.Max());
            }
            catch { return false; };

            largest += 10; //a little margin

            double scale = (float)Math.Min(windowSize.X, windowSize.Y) / largest;
            if (scale == 0) return false;

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
                foreach (Point p in parent.favoredPoints)
                {
                    theCanvas.Children.Add(new Line
                    {
                        X1 = p.X * scale,
                        Y1 = p.Y * scale,
                        X2 = p.X * scale,
                        Y2 = p.Y * scale,
                        StrokeThickness = 15,
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        // Stroke = new SolidColorBrush(P1.TheColor)
                        Stroke = new SolidColorBrush(Colors.Pink)
                    });
                }
                foreach (ModuleBoundarySegments.Arc t in parent.segments)
                {
                    theCanvas.Children.Add(new Line
                    {
                        X1 = t.p1.X * scale,
                        X2 = t.p2.X * scale,
                        Y1 = t.p1.Y * scale,
                        Y2 = t.p2.Y * scale,
                        StrokeThickness = 2,
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        // Stroke = new SolidColorBrush(P1.TheColor)
                        Stroke = new SolidColorBrush(Colors.Blue)
                    });
                }
            }
            catch { }
            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

    }
}
