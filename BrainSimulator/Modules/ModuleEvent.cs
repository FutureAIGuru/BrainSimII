//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Collections.Generic;

namespace BrainSimulator.Modules
{
    public class ModuleEvent : ModuleBase
    {
        int pointCount = 0;
        int eventCount = 0;
        int landmarkCount = 0;
        int pairCount = 0;

        public override void Fire()
        {
            Init();  //be sure to leave this here
        }

        public Thing CreateEvent(Thing newLandmark)
        {
            ModuleUKSN UKS = (ModuleUKSN)FindModleu(typeof(ModuleUKSN));
            if (UKS is null) return null;
            Thing retVal = UKS.AddThing("E" + eventCount++.ToString(), "Event");
            retVal.AddReference(newLandmark);
            return retVal;
        }

        public void AddOutcomePair(Thing Event, Thing theAction, Thing theOutcome)
        {
            ModuleUKSN UKS = (ModuleUKSN)FindModleu(typeof(ModuleUKSN));
            if (UKS is null) return;
            Thing newOutcomePair = UKS.AddThing("x" + pairCount++, Event); //creates new thing as it as a child 
            newOutcomePair.AddReference(theAction);
            newOutcomePair.AddReference(theOutcome);
        }

        public Thing CreateLandmark(List<Thing> near)
        {
            //a landmark is a collection of nearby segments
            //these must be cloned so they can be fixed in position rather than being adjusted relative to Sallie's position
            ModuleUKSN UKS = (ModuleUKSN)FindModleu(typeof(ModuleUKSN));
            if (UKS is null) return null;
            Thing newLandmark = UKS.AddThing("Lm" + landmarkCount++.ToString(), "Landmark");
            foreach (Thing t in near)
            {
                Segment s = Module2DModel.SegmentFromUKSThing(t);
                Thing P1 = UKS.AddThing("lmp" + pointCount++.ToString(), "Point"); //These points are not included in the model and do don't move with poin of view
                P1.V = s.P1.Clone();
                Thing P2 = UKS.AddThing("lmp" + pointCount++.ToString(), "Point");
                P2.V = s.P2.Clone();
                Thing S = UKS.AddThing("l" + t.Label.ToString(), "SSegment");
                S.AddReference(P1);
                S.AddReference(P2);
                S.AddReference(t.References[2].T);//the color
                newLandmark.AddReference(S);
            }

            return newLandmark;
        }


        public override void Initialize()
        {
            pointCount = 0;
            eventCount = 0;
            landmarkCount = 0;
            pairCount = 0;

        }
    }
}
