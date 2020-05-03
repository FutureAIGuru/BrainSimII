//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleMoveObject : ModuleBase
    {
        int state = 0;

        public override void Fire()
        {
            Init();  //be sure to leave this here

            if (GetNeuronValue("Auto") == 0) return;
            ModuleBehavior mBehavior = (ModuleBehavior)FindModuleByType(typeof(ModuleBehavior));
            if (mBehavior == null) return;
            if (GetNeuronValue("ModuleBehavior", "Done") == 0) return;
            Module2DModel mModel = (Module2DModel)FindModuleByType(typeof(Module2DModel));
            if (mModel is null) return;
            mModel.GetSegmentsFromUKS();
            List<Thing> segments = mModel.GetUKSSegments();
            if (segments.Count == 0) return;
            Thing t = segments[0];
            Segment s = Module2DModel.SegmentFromUKSThing(t);

            switch (state)
            {
                case 0: //go to first endpoint
                    PointPlus target = s.MidPoint();
                    target.X -= 0.2f; //body radius
                    mBehavior.TurnTo(-target.Theta);
                    mBehavior.MoveTo(target.R);
                    mBehavior.TurnTo(target.Theta);
                    state++;
                    break;
                case 1:
                    mBehavior.MoveTo(0.1f);
                    state++;
                    break;
                case 2:
                    state++;
                    break;
                case 3:
                    float motion = s.Motion.R;
                    state++;
                    break;
                case 4: //go to second endpoint
                    mBehavior.TurnTo(s.Angle());
                    mBehavior.MoveTo(s.Length()/2);
                    mBehavior.TurnTo(-s.Angle());
                    state++;
                    break;

            }
            //situation action outcome


            //back  off and try again from different locations/angles
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            AddLabel("Auto");
            state = 0;
        }
    }
}
