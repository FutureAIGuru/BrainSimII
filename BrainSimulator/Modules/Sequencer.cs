using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public partial class NeuronArray
    {
        public void Sequencer(NeuronArea na)
        {
            string input = na.GetParam("-i");
            NeuronArea naIn = FindAreaByLabel(input);
            if (!na.GetNeuronAt(0, 0).InUse()) //is already initialized?
            {
                if (naIn != null)
                {
                    for (int i = 0; i < na.Height && i < naIn.NeuronCount; i++)
                    {
                        Neuron n = naIn.GetNeuronAt(i);
                        n.AddSynapse(na.GetNeuronAt(0, i).Id, 1, this, false);
                    }
                }
                for (int i = 0;i < na.Width - 1; i++)
                {
                    for (int j = 0; j < na.Height; j++)
                    {
                        Neuron n = na.GetNeuronAt(i, j);
                        n.AddSynapse(na.GetNeuronAt(i + 1, j).Id, 1, this, false);
                    }
                }
            }
        }
    }
}