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
    public class ModuleWords : ModuleBase
    {
        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            Module naKB = theNeuronArray.FindAreaByLabel("Module2DKB");
            if (naKB == null) return;
            Module2DKB kb = (Module2DKB)naKB.TheModule;
            Thing tVisible = kb.Labeled("Visible");
            Thing tWords = kb.Labeled("Word");

            //Say the current association
            if (na.GetNeuronAt("Speak").CurrentCharge == 1)
            {
                foreach (Link l in tVisible.ReferencedBy)
                {
                    //these are segments
                    foreach (Link l1 in l.T.References)
                    {
                        //these are the properties of each visible segment
                        //are any linked to words?
                        if (l1.T.Parents.Contains(tWords))
                        {

                        }
                        foreach (Link l2 in l1.T.References)
                        {
                            //these are the properties of each visible segment
                            //are any linked to words?
                            if (l2.T.Parents.Contains(tWords))
                            {
                                na.GetNeuronAt(l2.T.V.ToString()).SetValue(1)
;                            }

                        }
                    }
                }
            }

            //build the association
            for (int i = 1; i < na.Width; i++)
            {
                Neuron n = na.GetNeuronAt(i, 0);
                if (n.Fired())
                {
                    Thing tWord = kb.Labeled(n.Label, tWords.Children);
                    if (tWord == null)
                        tWord = kb.AddThing(n.Label, new Thing[] { tWords }, n.Label);
                    foreach (Link l in tVisible.ReferencedBy)
                    {
                        //these are segments
                        foreach(Link l1 in l.T.References)
                        {
                            //these are the properties of each segment
                            if (l1.T != tVisible)
                                l1.T.AdjustReference(tWord, .1f);
                        }
                    }
                }
            }
        }
        public override void Initialize()
        {
            na.GetNeuronAt(0, 0).Label = "Speak";
            na.GetNeuronAt(1, 0).Label = "Red";
            na.GetNeuronAt(2, 0).Label = "Green";
            na.GetNeuronAt(3, 0).Label = "Blue";
            na.GetNeuronAt(4, 0).Label = "Orange";
        }
        public override void ShowDialog() //delete this function if it isn't needed
        {
            base.ShowDialog();
        }
    }


}
