using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BrainSimulator
{
    public class ModuleTurn : ModuleBase
    {
        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            if (MainWindow.realSim == null) return;
            if (na.NeuronCount < 2) return;
            if (na.GetNeuronAt(0).LastCharge > 0.9)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate { MainWindow.realSim.Turn(23); });
                ModuleRealityModel mrm = (ModuleRealityModel)FindModuleByType(typeof(ModuleRealityModel));
                if (mrm != null)
                    mrm.Rotate(20);
            }
            if (na.GetNeuronAt(1).LastCharge > 0.9)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate { MainWindow.realSim.Turn(-23); });
                ModuleRealityModel mrm = (ModuleRealityModel)FindModuleByType(typeof(ModuleRealityModel));
                if (mrm != null)
                    mrm.Rotate(-20);
            }

        }
    }
}
