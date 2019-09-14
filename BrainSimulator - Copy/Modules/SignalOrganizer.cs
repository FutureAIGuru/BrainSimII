using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public partial class NeuronArray
    {
        //this expands a set of inputs from an area of neuron-values to individual neurons (by column)
        //need a way to only get new values when necessary
        public void SignalOrganizer(NeuronArea na)
        {
            na.ClearNeuronChargeInArea();
            int theCol = 0;
            for (int i = 0; i < na.Width; i++) //the column number in this array
            {
                string input = na.GetParam("-i" + i);
                int strIndex = input.IndexOf('(');
                int oX = 0; //start
                int oY = 0;
                int oXe = 0; //end
                int oYe = 0;
                if (strIndex != -1) //get value from a single neuron
                {
                    string loc = input.Substring(strIndex + 1, input.Length - strIndex - 2);
                    input = input.Substring(0, strIndex);
                    string[] xy = loc.Split(',');
                    if (xy.Length < 2) return;
                    string[] x = xy[0].Split('-');
                    string[] y = xy[1].Split('-');
                    int.TryParse(x[0], out oX);
                    int.TryParse(y[0], out oY);
                    oXe = oX; oYe = oY;
                    if (x.Length > 1) int.TryParse(x[1], out oXe);
                    if (y.Length > 1) int.TryParse(y[1], out oYe);
                }
                NeuronArea naIn = FindAreaByLabel(input);
                if (naIn == null) return;
                for (int j = oX; j <= oXe; j++)
                    for (int k = oY; k <= oYe; k++)
                    {
                        Neuron n = naIn.GetNeuronAt(j, k);
                        if (n == null) break;
                        float value = n.LastCharge;
                        int theRow = 0;
                        if (n.Range == 0)
                        {//range [0,1]
                            if (value < 0) value = 0;
                            if (value > 1) value = 1;
                            theRow = (int)((na.Height - 1) * value);
                        }
                        else if (n.Range == 1)
                        {//range [-1,1]
                            if (value < -1) value = -1;
                            if (value > 1) value = 1;
                            //map to [0,1]
                            value = value / 2 + 0.5f;
                            theRow = (int)((na.Height - 1) * value);
                        }
                        na.GetNeuronAt(theCol, theRow).SetValue(1);
                        theCol++;
                    }
            }
        }
    }
}