using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public class ModuleEmotions : ModuleBase
    {
        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
        }
        public override void Initialize()
        {
            //if (naIn == null) return; //if naIn is needed?

            for (int i = 0; i < na.Width; i++)
            {
                for (int j = 0; j < na.Height; j++)
                {
                    Neuron n = na.GetNeuronAt(i, j);
                    n.Model = Neuron.modelType.OneTime;
                    n.DeleteAllSynapes();
                    n.CurrentCharge = 0;
                    n.LastCharge = 0;
                }
            }
            for (int i = 0; i < na.Width; i += 3)
            {
                na.GetNeuronAt(i, 0).Label = "☺";
                na.GetNeuronAt(i+1, 0).Label = "▲";
                na.GetNeuronAt(i+2, 0).Label = "▼";
                ;
                for (int j = 0; j < na.Height; j++)
                {
                    Neuron n = na.GetNeuronAt(i, j);
                    n.AddSynapse(n.Id, 1, theNeuronArray, false);
                    if (j < na.Height - 1)
                    {
                        Neuron n11 = na.GetNeuronAt(i + 1, j + 1);
                        Neuron n12 = na.GetNeuronAt(i + 2, j + 1);
                        n.AddSynapse(n12.Id, .5f, theNeuronArray, false);
                        n11.AddSynapse(n.Id, 1, theNeuronArray, false);
                        n12.AddSynapse(n.Id, -1, theNeuronArray, false);
                        na.GetNeuronAt(i + 1, 0).AddSynapse(n11.Id, .5f, theNeuronArray, false);
                        na.GetNeuronAt(i + 2, 0).AddSynapse(n12.Id, .5f, theNeuronArray, false);
                    }
                    if (j > 0)
                    {
                        Neuron n01 = na.GetNeuronAt(i + 1, j);
                        Neuron n02 = na.GetNeuronAt(i + 2, j);
                        n.AddSynapse(n01.Id, .5f, theNeuronArray, false);
                        n01.AddSynapse(n.Id, -1, theNeuronArray, false);
                        n02.AddSynapse(n.Id, 1, theNeuronArray, false);
                    }

                    na.GetNeuronAt(i, na.Height / 2).CurrentCharge = 1;
                }
            }
        }


    }
}
