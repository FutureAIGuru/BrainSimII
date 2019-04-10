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
            if (MainWindow.realSim == null) return;
            if (na.NeuronCount < 2) return;
            if (na.GetNeuronAt(0).LastCharge > 0.9)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate { MainWindow.realSim.Move(200); });
                ModuleRealityModel mrm = (ModuleRealityModel)FindModuleByType(typeof(ModuleRealityModel));
                if (mrm != null)
                    mrm.Move(200);
            }
            if (na.GetNeuronAt(1).LastCharge > 0.9)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate { MainWindow.realSim.Move(-200); });
                ModuleRealityModel mrm = (ModuleRealityModel)FindModuleByType(typeof(ModuleRealityModel));
                if (mrm != null)
                    mrm.Move(-200);
            }
        }
    }

}
