//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;

namespace BrainSimulator.Modules
{
    public class ModuleUKSN : ModuleUKS
   {
        public ModuleUKSN()
        {
            minHeight = 8;
            minWidth = 8;
        }

        public long immediateMemory = 2; //items are more-or-less simultaneous
        public static long shortTermMemory = 10; //items are close in time

        int EventCount = 0; //sequence # for labelling Event entries
        int phraseCount = 0; //sequence # for labelling phrases
        int wordCount = 0; //sequence # for labelling wordss
        List<Thing> activeSequences = new List<Thing>();
        int firstThing; //this temporarily allows embedding of a few command neurons in the KB neuron array

        List<Thing> shortTermMemoryList = new List<Thing>(); //order of words recently received in order to match existing phrases
        Random rand = new Random(); //used to randomize trial of actions

        //once for each cycle of the engine
        public override void Fire()
        {
            base.Fire();

            LearnWordLinks();

            //ShowReferences();

            LearnActions();

            CheckForPhrase();

            DoActions();

            PlayActiveSequences();

            HandleTalking();

            Dream();

        }


        // did a neuron fire?
        private bool NeuronFired(string label)
        {
            Neuron n = mv.GetNeuronAt(label);
            if (n == null) return false;
            return n.Fired();
        }



        //this returns the most recent firing regardless of how long ago it was
        Thing MostRecentFired(Thing t)
        {
            if (t == null) return null;
            Thing retVal = null;
            long firedAt = 0;
            foreach (Thing child in t.Children)
            {
                int i = UKS.IndexOf(child);
                Neuron n = mv.GetNeuronAt(i);
                if (n != null)
                {
                    if (n.LastFired > firedAt)
                    {
                        retVal = child;
                        firedAt = n.LastFired;
                    }
                }
            }
            return retVal;
        }

        List<Thing> AnyChildrenFired(Thing t, long maxCycles = 1, long minCycles = 1, bool checkDescendents = false)
        {
            List<Thing> retVal = new List<Thing>();
            if (t == null) return retVal;
            foreach (Thing child in t.Children)
            {
                if (Fired(child, maxCycles, true, minCycles))
                    retVal.Add(child);
                if (Fired(child, maxCycles, false, minCycles))
                    retVal.Add(child);
                if (checkDescendents)
                    retVal.AddRange(AnyChildrenFired(child, maxCycles, minCycles));
            }
            return retVal;
        }

        public override Thing Valued(object value, IList<Thing> KBt = null, float toler = 0)
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
            return;
            //the following code works but is too slow
            //if (na == null) return;
            //for (int i = 0; i < na.NeuronCount && i < UKS.Count; i++)
            //{
            //    if (UKS[i] == null) continue;
            //    if (na.GetNeuronAt(i).Label != UKS[i].Label)
            //    {
            //        na.GetNeuronAt(i).Label = UKS[i].Label;
            //        SetNeuronValue("KBOut", i, 0f, UKS[i].Label);
            //    }
            //}
            //for (int i = UKS.Count; i < na.NeuronCount; i++)
            //{
            //    na.GetNeuronAt(i).Label = "";
            //    SetNeuronValue("KBOut", i, 0f, "");
            //}
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
                for (int j = i + 1; j < mv.NeuronCount; j++)
                {
                    Neuron targetNeuron = mv.GetNeuronAt(j - 1);
                    Neuron sourceNeuron = mv.GetNeuronAt(j);
                    MainWindow.thisWindow.theNeuronArrayView.MoveOneNeuron(sourceNeuron, targetNeuron,false);
                }
                //repeat this process for the output array
                ModuleView naModule = theNeuronArray.FindModuleByLabel("KBOut");
                if (naModule != null)
                {
                    for (int j = i + 1; j < naModule.NeuronCount; j++)
                    {
                        Neuron targetNeuron = naModule.GetNeuronAt(j - 1);
                        Neuron sourceNeuron = naModule.GetNeuronAt(j);
                        MainWindow.thisWindow.theNeuronArrayView.MoveOneNeuron(sourceNeuron, targetNeuron,false);
                    }
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
            foreach (Neuron n in mv.Neurons)
            {
                n.DeleteAllSynapes();
            }
            ModuleView na1 = theNeuronArray.FindModuleByLabel("KBOut");
            if (na1 != null)
                foreach (Neuron n in na1.Neurons)
                {
                    n.DeleteAllSynapes();
                }

            //since we are inserting at 0, these are in reverse order
            UKS.Insert(0, new Thing { Label = "Sleep" });
            UKS.Insert(0, new Thing { Label = "Max" });
            UKS.Insert(0, new Thing { Label = "Ref" });
            UKS.Insert(0, new Thing { Label = "Parent" });
            firstThing = 4;
            EventCount = 0;
            phraseCount = 0;

            //add connections from speakPhonemes to this module

            ModuleView naPhonemes = theNeuronArray.FindModuleByLabel("ModuleSpeakPhonemes");
            if (naPhonemes != null)
            {
                Neuron n2 = GetNeuron(Labeled("SayRnd"));
                Neuron n3 = naPhonemes.GetNeuronAt("BabyTalk");
                if (n2 != null && n3 != null)
                    n3.AddSynapse(n2.Id, 1);
                Thing parent = Labeled("Vowel");
                foreach (Neuron n in naPhonemes.Neurons)
                {

                    if (n.Label == "Conson.") parent = Labeled("Consonant");
                    if (n.Label.Length == 1)
                    {
                        string label = "spk" + n.Label;
                        Thing pn = AddThing(label, new Thing[] { parent });
                        Neuron n1 = GetNeuron(pn, false);
                        if (n1 != null)
                        {
                            n1.AddSynapse(n.Id, 1);
                        }
                    }
                }
            }

            UpdateNeuronLabels();
        }


        //return neuron associated with KB thing
        public Neuron GetNeuron(Thing t, bool input = true)
        {
            int i = UKS.IndexOf(t);
            if (i == -1) return null;
            if (input)
            {
                return mv.GetNeuronAt(i);
            }
            else
            {
                ModuleView na1 = theNeuronArray.FindModuleByLabel("KBOut");
                if (na1 == null) return null;
                return na1.GetNeuronAt(i);
            }
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
        private ModuleView GetOutputModule()
        {
            return theNeuronArray.FindModuleByLabel("KBOut");
        }

        //did the neuron associated with a thing fire?  
        //firing at least minPastCycles but not more than maxPastCycles ago
        public bool Fired(Thing t, long maxPastCycles = 1, bool firedInput = true, long minPastCycles = 0)
        {
            int i = UKS.IndexOf(t);
            ModuleView naModule = mv;
            if (!firedInput)
            {
                naModule = GetOutputModule();
                if (naModule == null) return false;
            }
            Neuron n = naModule.GetNeuronAt(i);
            if (n == null) return false;
            long timeSinceLastFire = MainWindow.theNeuronArray.Generation - n.LastFired;
            bool retVal = timeSinceLastFire <= maxPastCycles;
            if (retVal)
                retVal = timeSinceLastFire >= minPastCycles;
            return retVal;
        }

        public void Play(Thing t)
        {
            if (!activeSequences.Contains(t))
                activeSequences.Add(t);
        }


        //this handles playing sequences of actions (like saying a phrase)
        //These are fired one-per-cycle until the end of the sequence
        //If a reference has a weight of -1, the system waits until it fires.
        private void PlayActiveSequences()
        {
            for (int i = 0; i >= 0 && i < activeSequences.Count; i++)
            {
                Thing activeSequence = activeSequences[i];
                if (activeSequence.References[activeSequence.currentReference].weight == -1)
                { //wait for firing of the specified thing
                    if (Fired(activeSequence.References[activeSequence.currentReference].T))
                    {
                        activeSequence.currentReference++;
                    }
                }
                else
                {
                    //fire the specified thing
                    Fire(activeSequence.References[activeSequence.currentReference++].T, false);
                }
                if (activeSequence.currentReference == activeSequence.References.Count)
                {
                    activeSequence.currentReference = 0;
                    activeSequences.RemoveAt(i);
                    i--;
                }
            }
        }

        public new Thing FindBestReference(Thing t, Thing parent = null)
        {
            if (t == null) return null;
            Thing retVal = null;
            float bestWeight = -100;
            foreach (Link l in t.References)
            {
                if (parent == null || l.T.Parents[0] == parent)
                {
                    if (l.weight > bestWeight)
                    {
                        retVal = l.T;
                        bestWeight = l.weight;
                    }
                }
            }
            return retVal;
        }

        //we'll want to change this to consider use-count and recency
        private void Forget(Thing t, int countToSave = 3)
        {
            if (t == null) return;
            while (t.Children.Count > countToSave)
            {
                DeleteThing(t.Children[0]);
            }
        }


        /// <summary>
        /// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>

        //execute the debugging commands if the associated neuron fired
        private void ShowReferences()
        {
            for (int i = firstThing; i < mv.NeuronCount && i < UKS.Count; i++)
            {
                Neuron n = mv.GetNeuronAt(i);
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

        private void BuildAssociation(Thing source, Thing dest, int maxCycles = 1, int minCycles = 0, int delta = 0)
        {
            if (source == null || dest == null) return;
            List<Thing> sources = AnyChildrenFired(source, maxCycles, minCycles, true);
            List<Thing> dests = AnyChildrenFired(dest, maxCycles + delta, minCycles + delta, true);
            foreach (Thing t in sources)
                foreach (Thing t1 in dests)
                    t.AdjustReference(t1);
        }



        private void CheckForPhrase()
        {
            //associate phonemes I've heard with phonemes I spoke 2 gens earlier
            BuildAssociation(Labeled("Phoneme"), Labeled("SpeakPhn"), 1, 1, 2);
            List<Thing> phonemes = AnyChildrenFired(Labeled("Phoneme"), 0, 0, true);
            IList<Thing> phrases = GetChildren(Labeled("Phrase"));
            Thing shortTerm = Labeled("phTemp");
            if (shortTerm == null) return;

            bool paused = true;
            foreach (Thing phoneme in phonemes)
            {
                if (shortTermMemoryList.Count == 0 || phoneme != shortTermMemoryList.LastOrDefault()) //can't duplicate a word in a phrase 
                    shortTermMemoryList.Add(phoneme);
                shortTerm.AddReference(phoneme);
                paused = false;
                phoneme.useCount++;
            }
            if (paused && shortTermMemoryList.Count > 0)
            {
                //is this a phrase I just said or is it outside input?
                if (AnyChildrenFired(Labeled("SpeakPhn"), shortTermMemory, 0, true).Count > 0)
                {
                    Forget(Labeled("PhraseISpoke"), 10);
                    shortTermMemoryList.Clear();
                    shortTerm.ReferencesWriteable.Clear();
                    return;
                }

                //find any known words in the phrase
                IList<Thing> words = GetChildren(Labeled("Word"));
                foreach (Thing word in words)
                {
                    if (word.References.Count > 0)
                    {
                        for (int i = 0; i < shortTermMemoryList.Count; i++)
                        {
                            bool match = true;
                            int k = i;
                            foreach (Link l in word.References)
                            {
                                if (k >= shortTermMemoryList.Count || shortTermMemoryList[k++] != l.T)
                                {
                                    match = false;
                                    break;
                                }
                            }
                            if (match)
                            {
                                word.useCount++;
                                shortTermMemoryList[i] = word;
                                shortTermMemoryList.RemoveRange(i + 1, word.References.Count - 1);
                            }
                        }
                    }
                }

                //anything which wasn't a word, convert to a word
                List<Thing> phonemesInWord = new List<Thing>();
                int startOfNewWord = -1;
                for (int i = 0; i < shortTermMemoryList.Count; i++)
                {
                    if (shortTermMemoryList[i].Parents[0] == Labeled("Word"))
                    {
                        if (phonemesInWord.Count > 0)
                        {
                            Thing newWord = AddThing("w" + wordCount++, Labeled("Word"), null, phonemesInWord.ToArray());
                            phonemesInWord.Clear();
                            if (startOfNewWord != -1)
                            {
                                shortTermMemoryList[startOfNewWord] = newWord;
                                shortTermMemoryList.RemoveRange(startOfNewWord + 1, newWord.References.Count - 1);
                                startOfNewWord = -1;
                            }
                        }
                    }
                    else
                    {
                        if (startOfNewWord == -1) startOfNewWord = i;
                        phonemesInWord.Add(shortTermMemoryList[i]);
                    }
                }
                if (phonemesInWord.Count > 0)
                {
                    Thing newWord = AddThing("w" + wordCount++, Labeled("Word"), null, phonemesInWord.ToArray());
                    shortTermMemoryList[startOfNewWord] = newWord;
                    shortTermMemoryList.RemoveRange(startOfNewWord + 1, newWord.References.Count - 1);
                    startOfNewWord = -1;
                }

                //Now add the phrase to the knowledge store
                Thing possibleNewPhrase = AddThing("ph" + phraseCount++, Labeled("Phrase"), null, shortTermMemoryList.ToArray());
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
                shortTerm.ReferencesWriteable.Clear();
                Fire(Labeled("Say"));
            }
        }


        private void HandleTalking()
        {
            //hack to prevent mixed phrases
            if (activeSequences.Count != 0) return;
            if (Fired(Labeled("Say"), immediateMemory))
            {
                List<Thing> phrasesToSay = AnyChildrenFired(Labeled("Phrase"));
                if (phrasesToSay.Count != 0)
                {
                    Thing newUtterance = AddThing("uu" + EventCount++, new Thing[] { Labeled("Utterance") }, null, null);
                    foreach (Link l in phrasesToSay[0].References)
                    {
                        //if the link is to a word, get the pronunciation of the word
                        if (l.T.Parents[0] == Labeled("Word"))
                        {
                            foreach (Link l1 in l.T.References)
                                newUtterance.AddReference(l1.T);
                            //newUtterance.AddReference(FindBestReference(l1.T));
                        }
                        else
                            //if the link is to a phoneme, add it
                            newUtterance.AddReference(l.T);
                    }
                    ModuleSpeakPhonemes nm = (ModuleSpeakPhonemes)FindModleu(typeof(ModuleSpeakPhonemes));

                    //what's the closest phrase we can say based on the phonemes we've learned to say so we can speak the phrase
                    foreach (Link l in newUtterance.References)
                    {
                        Thing t = l.T;
                        if (t.References.Count == 0)
                        {
                            //we've heard a phoneme we haven't learned how to speak
                            float bestDist = 100;
                            Thing closest = null;
                            foreach (Thing t1 in Labeled("Phoneme").Children)
                            {
                                if (t1.References.Count > 0)
                                {
                                    char p1 = t.Label[2];
                                    char p2 = t1.Label[2];
                                    float dist = nm.PhonemeDistance(p1, p2);
                                    if (dist > -1 && dist < bestDist)
                                    {
                                        closest = t1;
                                        bestDist = dist;
                                    }
                                }
                            }
                            Thing t2 = FindBestReference(closest);
                            if (t2 != null)
                                l.T = t2;
                        }
                        else
                        {
                            l.T = FindBestReference(t);
                        }
                    }
                    newUtterance.AddReference(Labeled("End"));
                    activeSequences.Add(newUtterance);
                }
            }
            if (Fired(Labeled("SayRnd"), 0))
            {
                //say a random utterance  (there is a hack to handle combiners which is needed to work with TTS)
                IList<Thing> vowels = Labeled("Vowel").Children;
                IList<Thing> consonants = Labeled("Consonant").Children;
                if (consonants.Count == 0) return;
                Thing consonant = consonants[rand.Next(consonants.Count - 2)];
                Thing vowel1 = vowels[rand.Next(vowels.Count)];
                Thing combiner = Labeled("spk\u0361");

                int useCombiner = rand.Next(20);

                int repeatCount = rand.Next(3);
                repeatCount++;

                Thing newUtterance = AddThing("uu" + EventCount++, new Thing[] { Labeled("Utterance") }, null, null);
                for (int i = 0; i < repeatCount; i++)
                {
                    if (useCombiner < ModuleSpeakPhonemes.combiners.Count)
                    {
                        string s = ModuleSpeakPhonemes.combiners[useCombiner];
                        if (useCombiner == 0)
                        {
                            newUtterance.AddReference(Labeled("spk" + s[0]));
                            newUtterance.AddReference(combiner);
                            newUtterance.AddReference(Labeled("spk" + s[2]));
                            newUtterance.AddReference(vowel1);
                        }
                        else
                        {
                            newUtterance.AddReference(consonant);
                            newUtterance.AddReference(Labeled("spk" + s[0]));
                            newUtterance.AddReference(combiner);
                            newUtterance.AddReference(Labeled("spk" + s[2]));
                        }
                    }
                    else
                    {
                        newUtterance.AddReference(consonant);
                        newUtterance.AddReference(vowel1);
                    }
                }
                newUtterance.AddReference(Labeled("End"));
                Forget(Labeled("Utterance"));
                activeSequences.Add(newUtterance);
                Fire(newUtterance);
            }

            /*//TODO select a phrase to say
            //things you can say are children of "Say".  They can contain sequences of words OR word-parameters which are affected by the Event
            List<Thing> sayPhrases = Labeled("Say").Children;
            Thing bestPhrase = null;
            foreach (Thing phrase in sayPhrases)
            {
                if (Fired(phrase, immediateMemory))
                {
                    bestPhrase = phrase;
                    activeSequences.Add(new Thing())
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

                        { Fire(word, false);
                        };
                        word = word;
                    }
                }

                //}
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
            */
        }

        //this learns associations between words and Events
        private void LearnWordLinks()
        {
            IList<Thing> words = GetChildren(Labeled("Word"));
            foreach (Thing word in words)
            {
                if (Fired(word, immediateMemory))
                {
                    IList<Thing> colors = GetChildren(Labeled("Color"));
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


        //if a reward/punishment has been given, save the inputs and action
        private void LearnActions()
        {
            if (lastPhraseHeard == null || lastActionPerformed == null) return;
            IList<Thing> outcomes = GetChildren(Labeled("Outcome"));
            foreach (Thing outcome in outcomes)
            {
                if (Fired(outcome, immediateMemory))
                {
                    float positive = (outcome.Label == "Positive") ? 1 : -1;
                    //do we already know this?
                    IList<Thing> Events = GetChildren(Labeled("Event"));
                    foreach (Thing learned in Events)
                    {
                        //TODO we now have other types of Events...need to detect
                        if (learned.References.Count > 0 && learned.References[0].T == lastPhraseHeard)  //TODO remove hard-coding of phrase as first reference
                        {
                            learned.AdjustReference(lastActionPerformed, positive);
                            //TODO do some use-count here?
                            lastPhraseHeard = null;
                            lastActionPerformed = null;
                            break;
                        }
                    }

                    //store the new Event memory
                    //TODO add a time limit so really old inputs aren't stored?
                    if (lastPhraseHeard != null && lastActionPerformed != null)
                    {
                        Thing newEvent = AddThing("si" + EventCount++, new Thing[] { Labeled("Event") }, null, new Thing[] { lastPhraseHeard });
                        if (newEvent != null)
                        newEvent.AdjustReference(lastActionPerformed, positive);
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
            IList<Thing> actions = GetChildren(Labeled("Action"));
            List<Thing> actionsNotYetTried = new List<Thing>(actions);// clone the list so we can keep track of what's already been tried
            IList<Thing> phrases = GetChildren(Labeled("Phrase"));

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
                        if (l.T.Parents.Contains(Labeled("Event")))
                        {
                            Thing Event = l.T;
                            for (int i = 1; i < Event.References.Count; i++)
                            {
                                Thing action = Event.References[i].T;
                                float weight = Event.References[i].weight;
                                if (Event.References[i].weight > 0)
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
                            //find a Event with this phrase
                            Thing Event = null;
                            foreach (Link l2 in t.ReferencedBy)
                            {
                                //TODO handle multiple Events containing this phrase
                                if (l2.T.Parents.Contains(Labeled("Event")))
                                {
                                    Event = l2.T;
                                    break;
                                }
                            }
                            if (Event == null) break;
                            for (int i = 1; i < Event.References.Count; i++)
                            {
                                if (Event.References[i].weight > 0)
                                {
                                    //check if actions is in actionsnotyettried
                                    if (actionsNotYetTried.Contains(Event.References[i].T))
                                    {
                                        Fire(Event.References[i].T, false);
                                        lastActionPerformed = Event.References[i].T;
                                        return;
                                    }
                                    //try nearby actions
                                    else
                                    {
                                        int index = actions.IndexOf(Event.References[i].T);
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
                            int i = (int)(rand.NextDouble() * actionsNotYetTried.Count);
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


        //handle  housekeeping
        private void Dream()
        {
            //TODO generalize phrases which differen only in the value of a parameter (children)
            //delete a bunch of extraneous links by choosing only the hightest value
            if (!NeuronFired("Sleep")) return;
            IList<Thing> words = GetChildren(Labeled("Word"));
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
    }
}
