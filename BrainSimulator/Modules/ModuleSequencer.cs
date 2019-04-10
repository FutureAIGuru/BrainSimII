using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public class ModuleSequencer : ModuleBase
    {
        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
        }
        protected override void Initialize()
        {
            if (naIn == null) return;
            if (!na.GetNeuronAt(0, 0).InUse()) //is already initialized?
            {
                if (naIn != null)
                {
                    for (int i = 0; i < na.Height && i < naIn.NeuronCount; i++)
                    {
                        Neuron n = naIn.GetNeuronAt(i);
                        n.AddSynapse(na.GetNeuronAt(0, i).Id, 1, theNeuronArray, false);
                    }
                }
                for (int i = 0; i < na.Width - 1; i++)
                {
                    for (int j = 0; j < na.Height; j++)
                    {
                        Neuron n = na.GetNeuronAt(i, j);
                        n.AddSynapse(na.GetNeuronAt(i + 1, j).Id, 1, theNeuronArray, false);
                    }
                }
            }
        }
    }


}
