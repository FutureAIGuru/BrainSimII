//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator.Modules
{
    public class ModuleAND : ModuleBase
    {
        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
        }
        public override void Initialize()
        {
            na.GetNeuronAt("0,0").Label = "in1";
            na.GetNeuronAt("0,1").Label = "in2";
            na.GetNeuronAt("1,1").Label = "out2";
            na.GetNeuronAt("in1").AddSynapse(na.GetNeuronAt("out2").Id, .5f);
        }
        public override void ShowDialog() //delete this function if it isn't needed
        {
            base.ShowDialog();
        }
    }


}
