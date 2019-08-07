using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BrainSimulator
{
    public class ModuleMove : ModuleBase
    {
        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            na.GetNeuronAt(0, 2).Range = 2;
            int[] dist = { 200,100,0,-100,-200};
            int distance = 0;


            for (int i = 0; i < na.Height; i++)
            {
                if (na.GetNeuronAt(0,i).LastCharge > 0.9)
                {
                    distance = dist[i];
                }
            }

            if (na.GetNeuronAt(0,2).LastCharge != 0)
            {
                distance = (int) (na.GetNeuronAt(0,2).LastCharge * 200);
                na.GetNeuronAt(0,2).SetValue(0);
            }

            if (MainWindow.realSim != null) Application.Current.Dispatcher.Invoke((Action)delegate { MainWindow.realSim.Move(distance); });
            ModuleRealityModel mrm = (ModuleRealityModel)FindModuleByType(typeof(ModuleRealityModel));
            if (mrm != null)
                mrm.Move(distance);

            bool moved = false;
            Module2DSim m2D = (Module2DSim)FindModuleByType(typeof(Module2DSim));
            if (m2D != null && distance != 0)
                moved = m2D.Move(distance);
            Module2DModel m2DModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));
            if (m2DModel != null && moved && distance != 0)
                m2DModel.Move(distance);
        }
        public override void Initialize()
        {
            na.GetNeuronAt(0,2).Range = 2;
            na.GetNeuronAt(0, 4).Range = 0;
        }
    }

}
