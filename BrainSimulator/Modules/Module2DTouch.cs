using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BrainSimulator
{
    public class Module2DTouch : ModuleBase
    {
        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            Module2DModel naModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));
            if (naModel == null) return;

            for (int i = 0; i < na.Height; i++)
            {
                //neurons:  0:touch   1:antAngle  2:antDistance 3: sensedLineAngle 4: conf1 5: len1 6: conf2 7: len2
                if (na.GetNeuronAt(0, i).CurrentCharge == 0) continue;
                float antAngle = na.GetNeuronAt(1, i).CurrentCharge;
                float antDist = na.GetNeuronAt(2, i).CurrentCharge;
                float lineAngle = na.GetNeuronAt(3, i).CurrentCharge;
                float conf1 = na.GetNeuronAt(4, i).CurrentCharge;
                float l1 = na.GetNeuronAt(5, i).CurrentCharge;
                float conf2 = na.GetNeuronAt(6, i).CurrentCharge;
                float l2 = na.GetNeuronAt(7, i).CurrentCharge;

                //create the line segment (all relative coordinates)
                PolarVector antennaPos = new PolarVector()
                { r = antDist, theta = Math.PI/2 - antAngle };

                lineAngle = (float)Math.PI / 2 - (lineAngle + antAngle);

                PolarVector pv1 = new PolarVector() { r = l1, theta = lineAngle };
                PolarVector pv2 = new PolarVector() { r = l2, theta = Math.PI+lineAngle };

                Point P1 = (Vector)Utils.ToCartesian(antennaPos) + Utils.ToCartesian(pv1);
                Point P2 = (Vector)Utils.ToCartesian(antennaPos) + Utils.ToCartesian(pv2);

                naModel.AddSegment(P1, P2, conf1, conf2);
            }
        }
        public override void Initialize()
        {
        }
    }


}
