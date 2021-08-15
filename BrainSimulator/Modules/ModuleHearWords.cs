//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;

namespace BrainSimulator.Modules
{
    public class ModuleHearWords : ModuleBase
    {
        private List<string> words = new List<string>();


        public override void Fire()
        {
            Init();  //be sure to leave this here
            if (words.Count > 0)
            {
                string word = words[0];
                na.BeginEnum();
                for (Neuron n = na.GetNextNeuron(); n != null; n = na.GetNextNeuron())
                {
                    if (n.Label == word)
                    {
                        n.SetValue(1);
//                        n.CurrentCharge = 1;
                        break;
                    }
                    else if (n.Label == "")
                    {
                        //the word has not been heard before, add it
                        n.Label = word;
                        n.CurrentCharge = 1;
                        //connection to UKS 
                        ModuleUKSN nmUKS = (ModuleUKSN)FindModuleByType(typeof(ModuleUKSN));
                        if (nmUKS != null)
                        {
                            IList<Thing> words = nmUKS.Labeled("Word").Children;
                            Thing w = nmUKS.Valued(word, words);
                            if (w == null)
                            {
                                string label = "w" + char.ToUpper(word[0]) + word.Substring(1);
                                w = nmUKS.AddThing(label, nmUKS.Labeled("Word"), word);
                            }
                            Neuron n1 = nmUKS.GetNeuron(w);
                            if (n1 != null)
                                n.AddSynapse(n1.Id, 1);
                            //TODO: add a synapse to the speakwords module as well
                        }
                        break;
                    }
                }
                words.RemoveAt(0);
            }
        }

        public void HearPhrase(string phrase)
        {
            if (words.Count != 0) return;
            string[] words1 = phrase.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string word in words1)
            {
                words.Add(word);
            }
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            na.BeginEnum();
            for (Neuron n = na.GetNextNeuron(); n != null; n = na.GetNextNeuron())
            {
                n.Label = "";
                n.DeleteAllSynapes();
            }

            Neuron n1 = na.GetNeuronAt(0);
            n1.Label = "good";
            Neuron n1Target = GetNeuron("Module2DUKS", "Positive");
            if (n1Target != null)
                n1.AddSynapse(n1Target.Id, 1);
            Neuron n2 = na.GetNeuronAt(1);
            n2.Label = "no";
            Neuron n2Target = GetNeuron("Module2DUKS", "Negative");
            if (n2Target != null)
                n2.AddSynapse(n2Target.Id, 1);
        }
    }
}
