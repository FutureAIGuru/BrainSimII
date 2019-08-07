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
    public partial class Module2DSimDlg : ModuleBaseDlg
    {
        public Module2DSimDlg()
        {
            InitializeComponent();
        }

        public override bool Draw()
        {
            if (!base.Draw()) return false;

            Module2DSim parent = (Module2DSim)base.Parent1;

            theCanvas.Children.RemoveRange(1, theCanvas.Children.Count-1);
            Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            TransformGroup tg = new TransformGroup();
            tg.Children.Add(new ScaleTransform(scale, -scale, 0, 0));// windowCenter.X, windowCenter.Y));
            tg.Children.Add(new TranslateTransform(windowCenter.X, windowCenter.Y));
            theCanvas.RenderTransform = tg;
            
            
            //draw the camera track...
            Polyline p = new Polyline();
            p.StrokeThickness = 1 / scale;
            p.Stroke = Brushes.Pink;
            for (int i = 0; i < parent.CameraTrack.Count; i++)
            {
                p.Points.Add(
                    new Point(
                        parent.CameraTrack[i].X,
                        parent.CameraTrack[i].Y
                        )
                        );
            }
            theCanvas.Children.Add(p);

            //draw the antennae...
            for (int i = 0; i < parent.antennaeActual.Length; i++)
            theCanvas.Children.Add(new Line
            {
                X1 = parent.CameraPosition.X,
                Y1 = parent.CameraPosition.Y,
                X2 = parent.antennaeActual[i].X,
                Y2 = parent.antennaeActual[i].Y,
                StrokeThickness = 2 / scale,
                Stroke = Brushes.Black
            });
 
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

            //draw the current field of view
            for (int i = 0; i < parent.currentView.Count; i++)
            {
                theCanvas.Children.Add(new Line
                {
                    X1 = parent.currentView[i].P1.X,
                    X2 = 1 / scale + parent.currentView[i].P1.X,
                    Y1 = parent.currentView[i].P1.Y,
                    Y2 = 1 / scale + parent.currentView[i].P1.Y,
                    StrokeThickness = 1 / scale,
                    Stroke = new SolidColorBrush(parent.currentView[i].theColor)
                });
            }

            return true;
        }


        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw();
        }


        private void TheCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Module2DSim parent = (Module2DSim)base.Parent1;

            Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;


            Point position = e.GetPosition(theCanvas);
            position = (Point)(position - windowCenter);
            position.X /= scale;
            position.Y /= scale;
            Point p1Abs = new Point(parent.objects[4].P1.X , parent.objects[4].P1.Y);
            Vector v1 = Point.Subtract(p1Abs,parent.CameraPosition);
            PolarVector pv = Utils.ToPolar((Point)v1);
            pv.theta += parent.CameraDirection1;
            Point p1Rel = Utils.ToCartesian(pv);

            MessageBox.Show(
                "Pressed: " + e.GetPosition(theCanvas).ToString() +
                "\r\n " + p1Abs.X + "," + p1Abs.Y + " " +
                "\r\n " + p1Rel.X + "," + p1Rel.Y + " " +
                "\r\n " + ((Vector)p1Rel).Length
                );
        }
    }
}
