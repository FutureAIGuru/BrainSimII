using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BrainSimulator
{
    public partial class NeuronArray
    {

        private void OpenTheCameraWindow()
        {
            MainWindow.theCameraWindow = new CameraHandler();
            MainWindow.theCameraWindow.Show();
        }

        public void Camera(NeuronArea na)
        {

            //the simulation view is opened and remains open for the duration of the Application
            //if closed by alt-F4, it cannot be reopened...oh well
            if (MainWindow.theCameraWindow == null)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate { OpenTheCameraWindow(); });
            }

            if (MainWindow.theCameraWindow == null) return;


            System.Drawing.Bitmap bitmap1;
            //            Application.Current.Dispatcher.Invoke((Action)delegate { MainWindow.theCameraWindow.GetBitMap(); });

            //the transfer from the sim to the view is double-buffered.
            if (MainWindow.theCameraWindow.theBitMap1 != null)
            {
                bitmap1 = MainWindow.theCameraWindow.theBitMap1;
                MainWindow.theCameraWindow.theBitMap1 = null;
            }
            else if (MainWindow.theCameraWindow.theBitMap2 != null)
            {
                bitmap1 = MainWindow.theCameraWindow.theBitMap2;
                MainWindow.theCameraWindow.theBitMap2 = null;
            }
            else
                return;

            na.GetBounds(out int X1, out int Y1, out int X2, out int Y2);
            float ratio = bitmap1.Width / (X2 - X1);
            float ratio2 = bitmap1.Height / (Y2 - Y1);
            if (ratio2 < ratio) ratio = ratio2;

            for (int i = X1; i < X2; i++)
                for (int j = Y1; j < Y2; j++)
                {
                    int neuronIndex = GetNeuronIndex(i, j);
                    Neuron n = MainWindow.theNeuronArray.neuronArray[neuronIndex];
                    int x = (int)((i - X1) * ratio);
                    int y = (int)((j - Y1) * ratio);
                    if (x >= bitmap1.Width) break;
                    if (y >= bitmap1.Height) break;
                    System.Drawing.Color c = bitmap1.GetPixel(x, y);
                    System.Windows.Media.Color c1 = new System.Windows.Media.Color
                    { A = c.A, R = c.R, G = c.G, B = c.B };
                    int theColor = Utils.ToArgb(c1);

                    if (theColor != 0 && theColor != 303)
                        n.SetValueInt(theColor);
                    else
                        n.SetValueInt(0);
                }

            NeuronArea naFovea = FindAreaByLabel("Fovea");
            if (naFovea == null) return;

            FoveaBitmap = bitmap1;

        }


    }
}