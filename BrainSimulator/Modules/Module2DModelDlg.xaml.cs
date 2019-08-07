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

namespace BrainSimulator
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


        public override bool Draw()
        {
            if (!base.Draw()) return false;

            Module2DModel parent = (Module2DModel)base.Parent1;
            theCanvas.Children.Clear();
            Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 20;
            TransformGroup tg = new TransformGroup();
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
                StrokeThickness = 1/scale,
                Stroke = Brushes.Black
            });
            theCanvas.Children.Add(new Line
            {
                X1 = 0,
                X2 = 0,
                Y1 = -.20,
                Y2 = .20,
                StrokeThickness = 1/scale,
                Stroke = Brushes.Black
            });


            //draw the objects
            for (int i = 0; i < parent.objects.Count; i++)
            {
                Color theColor = parent.objects[i].theColor;

                LinearGradientBrush lb = new LinearGradientBrush();
                lb.StartPoint = new Point(0, 0);
                lb.EndPoint = new Point(1, 0);
                if (parent.objects[i].conf1 == 1)
                    lb.GradientStops.Add(new GradientStop(theColor, 0.0));
                else
                    lb.GradientStops.Add(new GradientStop(Colors.Transparent, 0.0));
                lb.GradientStops.Add(new GradientStop(theColor, 0.5));
                if (parent.objects[i].conf2 == 1)
                    lb.GradientStops.Add(new GradientStop(theColor, 1.0));
                else
                    lb.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));

                theCanvas.Children.Add(new Line
                {
                    X1 = parent.objects[i].P1.X,
                    X2 = parent.objects[i].P2.X,
                    Y1 = parent.objects[i].P1.Y,
                    Y2 = parent.objects[i].P2.Y,
                    StrokeThickness = 3 / scale,
                    //Stroke = new SolidColorBrush(parent.objects[i].theColor)
                    Stroke = lb
                });
            }
            return true;
        }


        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw();
        }

    }
}
