using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public partial class NeuronArray
    {
        public void PointsOfInterest(NeuronArea na)
        {
            string input = na.GetParam("-i");
            NeuronArea naIn = FindAreaByLabel(input);
            if (naIn == null) return;
            na.ClearNeuronChargeInArea();

            naIn.BeginEnum();
            for (Neuron n = naIn.GetNextNeuron(); n != null; n = naIn.GetNextNeuron())
            {
                n.Range = 2;
                naIn.GetBounds(out int X1, out int Y1, out int X2, out int Y2);
                naIn.GetNeuronLocation(n, out int X, out int Y);
                if (X == 0 || Y == 0 || X == X2 - X1 - 1 || Y == Y2 - Y1 - 1) continue;
                //don't check the edge rows & columns

                int neuronIndex = n.Id;
                int outputIndex = neuronIndex + na.FirstNeuron - naIn.FirstNeuron;
                int neuronVal = neuronArray[neuronIndex].LastChargeInt;
                if (neuronVal == 0xffffff) continue;  //ignore white pixels
                int [] sv = {
                        neuronArray[neuronIndex-1-rows].LastChargeInt,
                        neuronArray[neuronIndex - 1].LastChargeInt,
                        neuronArray[neuronIndex - 1 + rows].LastChargeInt,
                        neuronArray[neuronIndex + rows].LastChargeInt,
                        neuronArray[neuronIndex + 1 + rows].LastChargeInt,
                        neuronArray[neuronIndex + 1].LastChargeInt,
                        neuronArray[neuronIndex + 1 - rows].LastChargeInt,
                        neuronArray[neuronIndex - rows].LastChargeInt
                        };
                if (sv[1] != sv[5] && sv[3] != sv[7])
                {
                    neuronArray[outputIndex].LastChargeInt =
                    neuronArray[outputIndex].CurrentChargeInt = neuronVal;
                }
                else
                {
                    neuronArray[outputIndex].LastCharge =
                    neuronArray[outputIndex].CurrentCharge= 0;
                }
            }
        }
    }
}
