using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public partial class NeuronArray
    {
        int currentNeuronInPoints = -1;
        public void Fovea(NeuronArea na)
        {
            if (FoveaBitmap == null) return;
            //string input = na.GetParam("-i");
            //NeuronArea naIn = FindAreaByLabel(input);
            //if (naIn == null) return;

            //for (int i = currentNeuronInPoints + 1; i < naIn.NeuronCount; i++)
            //{
            //    Neuron nIn = naIn.GetNeuronAt(i);
            //    if (nIn != null && nIn.LastCharge > 0)
            //    {
            //        naIn.GetBounds(out int X1, out int Y1, out int X2, out int Y2);
            //float ratio = FoveaBitmap.Width / (X2 - X1);
            //naIn.GetNeuronLocation(nIn, out int X, out int Y);
            //int x = (int)(X * ratio);//(x,y) is the center of the fovea area
            //int y = (int)(Y * ratio);
            int x = 100; int y = 100;

            for (int j = 0; j < na.Width; j++)
                for (int k = 0; k < na.Height; k++)
                {
                    Neuron n = na.GetNeuronAt(j, k);
                    int xPixel = x + j - na.Width / 2;
                    int yPixel = y + k - na.Height / 2;
                    if (xPixel >= FoveaBitmap.Width || xPixel < 0) break;
                    if (yPixel >= FoveaBitmap.Height || yPixel < 0) break;
                    System.Drawing.Color c = FoveaBitmap.GetPixel(xPixel, yPixel);
                    System.Windows.Media.Color c1 = new System.Windows.Media.Color
                    { A = c.A, R = c.R, G = c.G, B = c.B };
                    int theColor = Utils.ToArgb(c1);
                    if (theColor != 0 && theColor != 303)
                        n.SetValueInt(theColor);
                    else
                        n.SetValueInt(0);
                }

            //currentNeuronInPoints = i;
            //return;
            ////currentNeuronInPoints = -1;
            //FoveaBitmap = null;
        }
    }
}