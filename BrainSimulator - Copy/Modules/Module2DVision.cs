using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BrainSimulator
{
    public class Module2DVision : ModuleBase
    {

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable

            Module2DModel naModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));
 
            double GetDirectionOfNeuron (int index)
            { return Utils.fieldOfView / (na.Width - 1) * index - Utils.fieldOfView / 2; }

            int startOfColor = -1;
            Color curColor = Colors.Wheat;
            for (int i = 0; i < na.Width; i++)
            {
                Color x = Utils.FromArgb(na.GetNeuronAt(i, 0).CurrentChargeInt);
                if (x != curColor)
                {
                    if (i - startOfColor > 2)
                    {
                        int index = (i + startOfColor) / 2;
                        naModel.SetColor((float)GetDirectionOfNeuron(index), curColor);
                    }
                    startOfColor = i;
                    curColor = x;
                }
            }
            if (startOfColor < na.Width-3)
            {
                int index = (na.Width-1 + startOfColor) / 2;
                naModel.SetColor((float)GetDirectionOfNeuron(index), curColor);
            }

            ////can we see something not in the model?
            //TimeSpan ts = DateTime.Now - dt;
            //if (ts > new TimeSpan(0, 0, 0, 0, 5000))
            //{
            //    for (int i = 0; i < na.Width; i++)
            //    {
            //        int cur = na.GetNeuronAt(i, 0).CurrentChargeInt;
            //        if (na.GetNeuronAt(i, 0).CurrentChargeInt != 0)
            //        {
            //            bool newObjectSeen = naModel.IsAlreadyInModel((float)GetDirectionOfNeuron(i), Utils.FromArgb(na.GetNeuronAt(i, 0).CurrentChargeInt));
            //            //if (!newObjectSeen)
            //            //{
            //            //    dt = DateTime.Now;
            //            //    NeuronArea naBehavior = theNeuronArray.FindAreaByLabel("ModuleBehavior");
            //            //    naBehavior.GetNeuronAt(3, 0).SetValue(1);
            //            //    naBehavior.GetNeuronAt(4, 0).SetValue((float)theta);
            //            //    naBehavior.GetNeuronAt(5, 0).SetValue(1);
            //            //    naBehavior.GetNeuronAt(6, 0).SetValue(1f);
            //            //    break;
            //            //}
            //        }
            //    }
            //}
        }
        public override void Initialize()
        {
        }
        public override void ShowDialog() //delete this function if it isn't needed
        {
            base.ShowDialog();
        }
    }


}
