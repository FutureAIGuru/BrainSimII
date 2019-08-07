using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public class ModuleEntity : ModuleBase
    {
        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable

            NeuronArea naBehavior = theNeuronArray.FindAreaByLabel("ModuleBehavior");
            if (naBehavior == null) return;

            if (na.GetNeuronAt(0, 0).Fired()) //collision
            {
                naBehavior.GetNeuronAt(0, 0).SetValue(1); //stop
                naBehavior.GetNeuronAt(2, 0).SetValue(1); //Back-off
            }
            else if (na.GetNeuronAt(1, 0).Fired()) //idle
            {
                na.GetNeuronAt(1, 0).SetValue(0);
                Module2DModel naModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));
                PolarVector pv = naModel.FindLowConfidence();
                //pv.r += Math.PI / 4;
                if (pv != null)
                {
                    naBehavior.GetNeuronAt(3, 0).SetValue(1);
                    naBehavior.GetNeuronAt(4, 0).SetValue(-(float)pv.theta);
                    naBehavior.GetNeuronAt(5, 0).SetValue(1);
                    naBehavior.GetNeuronAt(6, 0).SetValue((float)pv.r);
                }
                else
                {
                    naBehavior.GetNeuronAt(1, 0).SetValue(1); //random
                }
            }
            else //seach for low confidence
            {

            }

            /*int index = 0;
            if (na.GetNeuronAt(0, 0).Fired() || na.GetNeuronAt(3, 0).Fired()) //pain
            {
                if (naTurn != null)
                {
                    index = (x > .5) ? 0 : 4;
                    naTurn.GetNeuronAt(index, 0).CurrentCharge = 1;
                }
            }
            else if (na.GetNeuronAt(2, 0).Fired()) //pleasure
            {
                if (naTurn != null)
                    naTurn.GetNeuronAt(2, 0).CurrentCharge = 1;
            }
            else //no-change
            {
                if (naTurn != null)
                {
                    index = (x > .5) ? 1 : 3;
                    naTurn.GetNeuronAt(index, 0).CurrentCharge = 1;
                }
            }

            //move
            if (naMove != null)
                naMove.GetNeuronAt(0, (naMove.Height - 1)).CurrentCharge = 1;
                */
        }
        public override void Initialize()
        {
            na.GetNeuronAt(0, 0).Label = "Collision";
            na.GetNeuronAt(1, 0).Label = "Idle";
        }
    }
}
