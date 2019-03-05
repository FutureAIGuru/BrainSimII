using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public partial class NeuronArray
    {
        public void ModuleName(NeuronArea na)
        {
            string input = na.GetParam("-i");
            NeuronArea naIn = FindAreaByLabel(input);
            if (naIn == null) return;

            naIn.BeginEnum();
            for (Neuron n = naIn.GetNextNeuron(); n != null; n = naIn.GetNextNeuron())
            {

            }

        }
    }
}