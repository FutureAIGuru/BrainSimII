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

            //theCanvas.Children.RemoveRange(1, theCanvas.Children.Count-1);
            theCanvas.Children.Clear();
            Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            TransformGroup tg = new TransformGroup();
            tg.Children.Add(new ScaleTransform(scale, -scale, 0, 0));// windowCenter.X, windowCenter.Y));
            tg.Children.Add(new TranslateTransform(windowCenter.X, windowCenter.Y));
            theCanvas.RenderTransform = tg;


            //add a background
            Rectangle r = new Rectangle() { Height = parent.boundarySize * 2, Width = parent.boundarySize * 2, Stroke = Brushes.AliceBlue,Fill = Brushes.AliceBlue};
            Canvas.SetLeft(r, -parent.boundarySize);
            Canvas.SetTop(r, -parent.boundarySize );
            theCanvas.Children.Add(r);
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

            Vector v = position - parent.CameraPosition;
            float dist = (float)v.Length;
            double angle = (float)Utils.ToPolar((Point)v).theta;
            angle = (float) angle - (Math.PI/2 - parent.CameraDirection1);
            NeuronArea na = MainWindow.theNeuronArray.FindAreaByLabel("ModuleBehavior");
            na.GetNeuronAt(3,0).SetValue(1);
            na.GetNeuronAt(4,0).SetValue((float)angle);
            na.GetNeuronAt(5, 0).SetValue(1);
            na.GetNeuronAt(6, 0).SetValue(dist);



        }
    }
}
