﻿//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Collections.Generic;

namespace BrainSimulator.Modules
{
    public class ModuleLearning : ModuleBase
    {
        //THIS IS A DEMONSTRATION module showing the rewrite needed to migrate a learning algorithm out of the UKSN module.
        //As learning algorithms become more complex and prolific, this will become progressively more important.


        ModuleUKSN KB = null;

        public override string ShortDescription { get => "TO DO: Short description of Module Learning."; }
        public override string LongDescription
        {
            get =>
                "TO DO: Long description of Module Learning.";
        }

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            if (KB == null) Initialize();
            if (KB == null) return;
            List<Thing> words = KB.GetChildren(KB.Labeled("Word"));
            foreach (Thing word in words)
            {
                if (KB.Fired(word, KB.immediateMemory))
                {
                    List<Thing> colors = KB.GetChildren(KB.Labeled("Color"));
                    foreach (Thing t in colors)
                    {
                        if (KB.Fired(t, KB.immediateMemory))
                            word.AdjustReference(t, 1); //Hit
                        else
                            word.AdjustReference(t, -1); //Miss
                    }
                }
            }
        }

        public override void Initialize()
        {
            ModuleView naKB = theNeuronArray.FindModuleByLabel("ModuleUKS");
            if (naKB != null)
            {
                if (naKB.TheModule != null)
                {
                    KB = (ModuleUKSN)naKB.TheModule;
                }
            }
        }
    }
}
