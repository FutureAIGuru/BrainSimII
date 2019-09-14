//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

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
            Init();  //be sure to leave this here
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
