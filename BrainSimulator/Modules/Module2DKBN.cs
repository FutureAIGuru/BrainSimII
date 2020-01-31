//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class Module2DKBN : Module2DKB
    {
        public long immediateMemory = 2; //items are more-or-less simultaneous
        long shortTermMemory = 6; //items are close in time
        public override string ShortDescription => "A Knowledge Graph KB module expanded with a neuron arraysfor intputs and outputs.";
        public override string LongDescription => "This is like a KB module but expanded to be accessible via neuron firings instead of just programmatically. " + base.LongDescription;

        int situationCount = 0; //sequence # for labelling situation entries
        int phraseCount = 0; //sequence # for labelling phrases
        Thing activeSequence = null;
        int firstThing; //this temporarily allows embedding of a few command neurons in the KB neuron array

        List<Thing> shortTermMemoryList = new List<Thing>(); //order of words recently received in order to match existing phrases
        Random r = new Random(); //used to randomize trial of actions

        //once for each cycle of the engine
        public override void Fire()
        {
            base.Fire();

            //LearnWordLinks();

            //ShowReferences();

            LearnActions();

            CheckForPhrase();

            DoActions();

            PlayActiveSequence();

            HandleSpeech();

            Dream();
        }


        // did a neuron fire?
        private bool NeuronFired(string label)
        {
            Neuron n = na.GetNeuronAt(label);
            if (n == null) return false;
            return n.Fired();
        }

        //this is needed for the dialog treeview
        public List<Thing> GetTheKB()
        {
            return UKS;
        }

        //execute the debugging commands if the associated neuron fired
        private void ShowReferences()
        {
            for (int i = firstThing; i < na.NeuronCount && i < UKS.Count; i++)
            {
                Neuron n = na.GetNeuronAt(i);
                if (NeuronFired("Parent"))
                {
                    if (n.Fired())
                    {
                        foreach (Thing t in UKS[i].Parents)
                        {
                            int j = UKS.IndexOf(t);
                            if (j >= 0)
                            {
                                SetNeuronValue("KBOut", j, 1);
                            }
                        }
                    }
                }
                if (NeuronFired("Ref"))
                {
                    if (n.Fired())
                    {
                        foreach (Link l in UKS[i].References)
                        {
                            int j = UKS.IndexOf(l.T);
                            if (j >= 0)
                            {

                                SetNeuronValue("KBOut", j, 1);
                            }
                        }
                    }
                }
                if (NeuronFired("Max"))
                {
                    if (n.Fired())
                    {
                        Link Max = new Link { weight = -1 };
                        foreach (Link l in UKS[i].References)
                        {
                            if (l.Value() > Max.weight) Max = l;
                        }
                        if (Max.weight > -1)
                        {
                            int j = UKS.IndexOf(Max.T);
                            if (j >= 0)
                            {
                                SetNeuronValue("KBOut", j, 1);
                            }
                        }
                    }
                }
            }
        }

        private void CheckForPhrase()
        {
            List<Thing> words = GetChildren(Labeled("Word"));
            List<Thing> phrases = GetChildren(Labeled("Phrase"));
            Thing shortTerm = Labeled("phTemp");
            if (shortTerm == null) return;

            bool paused = true;
            foreach (Thing word in words)
            {
                if (Fired(word, immediateMemory))
                {
                    if (shortTermMemoryList.Count == 0 || word != shortTermMemoryList.LastOrDefault()) //can't duplicate a word in a phrase 
                        shortTermMemoryList.Add(word);
                    shortTerm.AddReference(word);
                    paused = false;
                    word.useCount++;
                }
            }
            if (paused && shortTermMemoryList.Count > 0)
            {
                //TODO see if a sibling phrase already exists

                //our word input has paused.  See if the current phrase is already stored and add it if not
                Thing possibleNewPhrase = AddThing("ph" + phraseCount++, new Thing[] { Labeled("Phrase") }, null, shortTermMemoryList.ToArray());
                List<Link> exitingPhrase = possibleNewPhrase.FindSimilar(phrases, true, 1);
                if (exitingPhrase.Count == 1 && exitingPhrase[0].weight == 1)
                {
                    //existing phrase
                    DeleteThing(possibleNewPhrase);
                    Fire(exitingPhrase[0].T);
                }
                else
                {
                    //new phrase
                    Fire(possibleNewPhrase);
                }

                shortTermMemoryList.Clear();
                shortTerm.References.Clear();
            }
        }

        //this learns associations between words and situations
        private void LearnWordLinks()
        {
            List<Thing> words = GetChildren(Labeled("Word"));
            foreach (Thing word in words)
            {
                if (Fired(word, immediateMemory))
                {
                    List<Thing> colors = GetChildren(Labeled("Color"));
                    foreach (Thing t in colors)
                    {
                        if (Fired(t, immediateMemory))
                            word.AdjustReference(t, 1); //Hit
                        else
                            word.AdjustReference(t, -1); //Miss
                    }
                }
            }
        }

        //return neuron associated with KB thing
        public Neuron GetNeuron(Thing t)
        {
            int i = UKS.IndexOf(t);
            if (i == -1) return null;
            return na.GetNeuronAt(i);
        }

        //fire the neuron associated with a KB thing
        public void Fire(Thing t, bool fireInput = true) //false fires the neuron in the output array
        {
            int i = UKS.IndexOf(t);
            if (i == -1) return;
            if (fireInput)
                SetNeuronValue(null, i, 1);
            else
                SetNeuronValue("KBOut", i, 1);
            t.useCount++;
        }

        //did the neuron associated with a thing fire?  Might change to indicate how long ago
        public bool Fired(Thing t, long pastCycles)
        {
            int i = UKS.IndexOf(t);
            Neuron n = na.GetNeuronAt(i);
            if (n == null) return false;
            long timeSinceLastFire = MainWindow.theNeuronArray.Generation - n.LastFired;
            bool retVal = timeSinceLastFire < pastCycles;
            return retVal;
        }

        //did the output neuron associated with a thing fire?  Might change to indicate how long ago
        public bool FiredOutput(Thing t, long pastCycles)
        {
            int i = UKS.IndexOf(t);
            ModuleView naModule = theNeuronArray.FindAreaByLabel("KBOut");
            Neuron n = naModule.GetNeuronAt(i);
            if (n == null) return false;
            long timeSinceLastFire = MainWindow.theNeuronArray.Generation - n.LastFired;
            bool retVal = timeSinceLastFire < pastCycles;
            return retVal;
        }

        //if a reward/punishment has been given, save the inputs and action
        private void LearnActions()
        {
            if (lastPhraseHeard == null || lastActionPerformed == null) return;
            List<Thing> outcomes = GetChildren(Labeled("Outcome"));
            foreach (Thing outcome in outcomes)
            {
                if (Fired(outcome, immediateMemory))
                {
                    float positive = (outcome.Label == "Positive") ? 1 : -1;
                    //do we already know this?
                    List<Thing> situations = GetChildren(Labeled("Situation"));
                    foreach (Thing learned in situations)
                    {
                        if (learned.References.Count > 0 && learned.References[0].T == lastPhraseHeard)  //TODO remove hard-coding of phrase as first reference
                        {
                            learned.AdjustReference(lastActionPerformed, positive);
                            //TODO do some use-count here?
                            lastPhraseHeard = null;
                            lastActionPerformed = null;
                            break;
                        }
                    }

                    //store the new situation memory
                    //TODO add a time limit so really old inputs aren't stored?
                    if (lastPhraseHeard != null && lastActionPerformed != null)
                    {
                        Thing newSituation = AddThing("si" + situationCount++, new Thing[] { Labeled("Situation") }, null, new Thing[] { lastPhraseHeard });
                        newSituation.AdjustReference(lastActionPerformed, positive);
                    }
                    Fire(lastActionPerformed, false);
                    lastPhraseHeard = null;
                    lastActionPerformed = null;
                }
            }
        }

        Thing lastPhraseHeard = null;
        Thing lastActionPerformed = null;

        private void DoActions()
        {
            List<Thing> actions = GetChildren(Labeled("Action"));
            List<Thing> actionsNotYetTried = new List<Thing>(actions);// clone the list so we can keep track of what's already been tried
            List<Thing> phrases = GetChildren(Labeled("Phrase"));

            foreach (Thing phrase in phrases)
            {
                if (Fired(phrase, immediateMemory))
                {
                    Thing bestAction = null;
                    float bestWeight = -float.MaxValue;

                    lastPhraseHeard = phrase;
                    bool bHasAction = false;

                    foreach (Link l in phrase.ReferencedBy)
                    {
                        if (l.T.Parents.Contains(Labeled("Situation")))
                        {
                            Thing situation = l.T;
                            for (int i = 1; i < situation.References.Count; i++)
                            {
                                Thing action = situation.References[i].T;
                                float weight = situation.References[i].weight;
                                if (situation.References[i].weight > 0)
                                {
                                    //we have a positive action, perform it
                                    Fire(action, false);
                                    bHasAction = true;
                                    lastActionPerformed = action;
                                    bestAction = action;
                                    break;
                                }
                                else
                                {
                                    actionsNotYetTried.Remove(action);
                                    if (weight > bestWeight)
                                    {
                                        bestAction = action;
                                        bestWeight = weight;
                                    }
                                }
                            }
                        }
                    }

                    //if there is no stored action for this stimulus with a positive outcome, try a new action
                    if (!bHasAction)
                    {
                        //try noaction first
                        if (actionsNotYetTried.Count > 0 && actionsNotYetTried[0].Label == "NoAction")
                        {
                            Fire(actionsNotYetTried[0], false);
                            lastActionPerformed = actionsNotYetTried[0];
                            break;
                        }

                        //try actions near actions on similar phrases
                        List<Link> similarPhrases = lastPhraseHeard.FindSimilar(Labeled("Phrase").Children, true, 5);
                        foreach (Link l in similarPhrases)
                        {
                            Thing t = l.T;
                            //find a situation with this phrase
                            Thing situation = null;
                            foreach (Link l2 in t.ReferencedBy)
                            {
                                //TODO handle multiple situations containing this phrase
                                if (l2.T.Parents.Contains(Labeled("Situation")))
                                {
                                    situation = l2.T;
                                    break;
                                }
                            }
                            if (situation == null) break;
                            for (int i = 1; i < situation.References.Count; i++)
                            {
                                if (situation.References[i].weight > 0)
                                {
                                    //check if actions is in actionsnotyettried
                                    if (actionsNotYetTried.Contains(situation.References[i].T))
                                    {
                                        Fire(situation.References[i].T, false);
                                        lastActionPerformed = situation.References[i].T;
                                        return;
                                    }
                                    //try nearby actions
                                    else
                                    {
                                        int index = actions.IndexOf(situation.References[i].T);
                                        if (index != -1)
                                        {
                                            for (int j = 1; j < 20; j++)
                                            {
                                                int offset = (j / 2) * ((j % 2) * 2 - 1);
                                                offset += index;
                                                if (offset >= 0 && offset < actions.Count)
                                                {
                                                    if (actionsNotYetTried.Contains(actions[offset]))
                                                    {
                                                        Fire(actions[offset], false);
                                                        lastActionPerformed = actions[offset];
                                                        return;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        //try a random action
                        if (actionsNotYetTried.Count > 0)
                        {
                            int i = (int)(r.NextDouble() * actionsNotYetTried.Count);
                            Fire(actionsNotYetTried[i], false);
                            lastActionPerformed = actionsNotYetTried[i];
                        }
                        //all actions have been tried, retry something that was rejected before
                        else
                        {
                            Fire(bestAction, false);
                            lastActionPerformed = bestAction;
                        }
                        //TODO find the least used of unreferenced actions
                    }
                }
            }


        }

        private void HandleSpeech()
        {
            if (!FiredOutput(Labeled("Say"), immediateMemory)) return;
            //TODO select a phrase to say
            //things you can say are children of "Say".  They can contain sequences of words OR word-parameters which are affected by the situation
            List<Thing> sayPhrases = Labeled("Say").Children;
            Thing bestPhrase = null;
            foreach (Thing phrase in sayPhrases)
            {
                if (Fired(phrase, immediateMemory))
                {
                    bestPhrase = phrase;
                    activeSequence = new Thing()
                    {
                        References = bestPhrase.References
                    };
                    return;
                }
            }
            if (bestPhrase == null)
            {
                //no good phrase exists, try to create a new one
                //did anything fire we know words for?
                List<Thing> colors = Labeled("Color").Children; //change this to all sensory inputs
                foreach (Thing color in colors)
                {
                    if (Fired(color, immediateMemory)) //a sensory thing fired
                    {
                        //do we have a word associated?
                        Thing word = color.MostLikelyReference(Thing.ReferenceDirection.referenceBy, Labeled("Word"));

                        { Fire(word, false); };
                        word = word;
                    }
                }

            }
            return;
            if (lastPhraseHeard != null)
            {
                List<Link> similarPhrases = lastPhraseHeard.FindSimilar(Labeled("Phrase").Children, false, 5);
                if (similarPhrases.Count > 0)
                {
                    activeSequence = new Thing()
                    {
                        References = new List<Link>(similarPhrases[0].T.References)
                    };
                }
                else
                {
                    List<Thing> allPhrases = Labeled("Say").Children;
                    Thing randomPhrase = allPhrases[r.Next(0, allPhrases.Count)];
                    {
                        activeSequence = new Thing()
                        {
                            References = new List<Link>(randomPhrase.References)
                        };
                    }
                }
            }
        }

        //this handle playing a single sequence of actions (like saying a phrase)
        private void PlayActiveSequence()
        {
            if (activeSequence == null) return;
            Fire(activeSequence.References[activeSequence.currentReference++].T, false);
            if (activeSequence.currentReference == activeSequence.References.Count)
            {
                activeSequence = null;
            }
        }

        //handle  housekeeping
        private void Dream()
        {
            //TODO generalize phrases which differen only in the value of a parameter (children)
            //delete a bunch of extraneous links by choosing only the hightest value
            if (!NeuronFired("Sleep")) return;
            List<Thing> words = GetChildren(Labeled("Word"));
            foreach (Thing word in words)
            {
                Thing t = word.MostLikelyReference(Thing.ReferenceDirection.reference);
                if (t != null)
                {
                    Thing t1 = t.MostLikelyReference(Thing.ReferenceDirection.referenceBy);
                    if (t1 == word)
                    {

                    }
                }
            }
        }


        public override Thing AddThing(string label, Thing[] parents, object value = null, Thing[] references = null)
        {
            int i = UKS.Count;
            Thing retVal = base.AddThing(label, parents, value, references);
            UpdateNeuronLabels();
            return retVal;
        }
        public override void DeleteThing(Thing t)
        {
            int i = UKS.IndexOf(t);
            base.DeleteThing(t);
            //because we removed a node, any external synapses to related neurons need to be adjusted to point to the right place
            //on any neurons which might have shifted.
            if (i > -1)
            {
                for (int j = i + 1; j < na.NeuronCount; j++)
                {
                    Neuron targetNeuron = na.GetNeuronAt(j - 1);
                    Neuron sourceNeuron = na.GetNeuronAt(j);
                    MainWindow.thisWindow.theNeuronArrayView.MoveOneNeuron(sourceNeuron, targetNeuron);
                }
                //repeat this process for the output array
                ModuleView naModule = theNeuronArray.FindAreaByLabel("KBOut");
                for (int j = i + 1; j < naModule.NeuronCount; j++)
                {
                    Neuron targetNeuron = naModule.GetNeuronAt(j - 1);
                    Neuron sourceNeuron = naModule.GetNeuronAt(j);
                    MainWindow.thisWindow.theNeuronArrayView.MoveOneNeuron(sourceNeuron, targetNeuron);
                }
            }
            UpdateNeuronLabels();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            base.Initialize();
            //since we are inserting at 0, these are in reverse order
            UKS.Insert(0, new Thing { Label = "Sleep" });
            UKS.Insert(0, new Thing { Label = "Max" });
            UKS.Insert(0, new Thing { Label = "Ref" });
            UKS.Insert(0, new Thing { Label = "Parent" });
            firstThing = 4;
            situationCount = 0;
            phraseCount = 0;
            UpdateNeuronLabels();
        }

        public override Thing Valued(object value, List<Thing> KBt = null, float toler = 0)
        {
            Thing retVal = base.Valued(value, KBt, toler);
            if (retVal != null)
            {
                Fire(retVal);
            }
            return retVal;
        }


        private void UpdateNeuronLabels()
        {
            if (na == null) return;
            for (int i = 0; i < na.NeuronCount && i < UKS.Count; i++)
            {
                if (UKS[i] == null) continue;
                na.GetNeuronAt(i).Label = UKS[i].Label;
                SetNeuronValue("KBOut", i, 0f, UKS[i].Label);
            }
            for (int i = UKS.Count; i < na.NeuronCount; i++)
            {
                na.GetNeuronAt(i).Label = "";
                SetNeuronValue("KBOut", i, 0f, "");
            }
        }
    }
}
