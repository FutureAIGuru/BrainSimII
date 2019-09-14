using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public partial class NeuronArray
    {
        public void Learner(NeuronArea na)
        {
            string input = na.GetParam("-i");
            NeuronArea naIn = FindAreaByLabel(input);
            if (naIn == null) return;
            string output = na.GetParam("-o");
            NeuronArea naOut = FindAreaByLabel(output);

            Neuron nFound = na.GetNeuronAt(0, 1);
            Neuron nTrigger = na.GetNeuronAt(0, 2);
            Neuron nT0 = na.GetNeuronAt(0, 3);
            Neuron nT1 = na.GetNeuronAt(0, 4);

            if (!na.GetNeuronAt(0, 0).InUse())
            {
                //create the alwaysFire neuron
                Neuron n0 = na.GetNeuronAt(0, 0);
                n0.AddSynapse(n0.Id, 1, this, false);
                n0.SetValue(1);
                n0.Label = "1";
                nFound.Label = "Fnd";
                nTrigger.Label = "T";
                nT0.Label = "T0";
                nT1.Label = "T1";
                nTrigger.AddSynapse(nT0.Id, 1, this, false);
                nTrigger.AddSynapse(nT1.Id, 1, this, false);
                nT0.AddSynapse(nT1.Id, -1, this, false);


                //fire the trigger if the first column of input is zero
                n0.AddSynapse(nTrigger.Id, 1, this, false);
                for (int i = 0; i < naIn.Height; i++)
                    naIn.GetNeuronAt(0, i).AddSynapse(nTrigger.Id, -1, this, false);
            }
            if (!nFound.Fired() && nT1.Fired())
            {
                //add a new stored pattern
                float count = 0;
                for (int i = 0; i < naIn.NeuronCount; i++)
                    if (naIn.GetNeuronAt(i).Fired()) count++;
                if (count > 0)
                {
                    Neuron newNeuron = na.GetFreeNeuron();
                    newNeuron.AddSynapse(nFound.Id, 1, this, false);
                    float weight = 1 / count;
                    float negWeight = -.5f;
                    for (int i = 2; i < naIn.Width; i++)
                        for (int j = 0; j < naIn.Height; j++)
                    {
                        Neuron n = naIn.GetNeuronAt(i,j);
                        Neuron nLeft = naIn.GetNeuronAt(i - 2, j); //2 cols left for synchornization
                        if (n.Fired())
                        {
                            nLeft.AddSynapse(newNeuron.Id, weight, this, false);
                            if (naOut != null)
                            {
                                newNeuron.AddSynapse(naOut.GetNeuronAt(i-2,j).Id, 1, this, false);
                            }
                        }
                        else
                            {
                                nLeft.AddSynapse(newNeuron.Id, negWeight, this, false);
                            }
                        }
                }
            }
        }
    }
}