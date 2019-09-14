using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public class ModuleEmpty : ModuleBase
    {
        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
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
