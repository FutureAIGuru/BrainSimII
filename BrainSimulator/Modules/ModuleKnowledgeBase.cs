using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public class ModuleKnowledgeBase : ModuleBase
    {
        public class KBThing
        {
            //Type? rectangle , word,   //label? color?  pointer to thingType which is another thing
            //list of related things //weights?
            //parent/child??
            //next or previous thing //lists?  //rings of related things
            //list of things which reference this one
            //thing which caused next  //action..speak/hear/see/move?/existance
            //thing which cause previous
            //useage may be related to weights/ searching
            public string label = ""; //this is just for convenience in debugging and should not be used
            public KBThing parent = null;
            public List<KBThing> children = new List<KBThing>(); //synapses to
            public List<KBThing> references = new List<KBThing>(); //synapses to
            public List<KBThing> referencedBy = new List<KBThing>(); //synapses from
            public KBThing next = null; //next in a sequence of things
            public KBThing previous = null; //predecessor
            public int neuronIndex;
            public long lastActive = 0;
            public float importance = 0; //will be used to prioitize properties/references?
        }
        //absolute things...inputs and outputs...external things
        //group things
        //specify things (ungroup/new group)
        //abstract root! 

        Dictionary<int, KBThing> KBData = new Dictionary<int, KBThing>();
        long generation = 0;
        //methods...addNew, 
        //Search w/ partial match, search with AND (any # of params)
        //
        //From hit: get parent get next, get previous, get childern  describe fully, getnext best hit after search
        int GetFreeNeuron(int parent)
        {
            int retVal = parent;
            while (KBData.ContainsKey(retVal)) retVal++;
            return retVal;
        }
        int AddNew(
            int neuronIndex,
            string label,
            int[] related = null,
            int parent = -1,
            int child = -1,
            int previous = -1)
        {
            KBThing newThing;
            if (neuronIndex == -1 && parent != -1 && related == null) neuronIndex = GetFreeNeuron(parent);
            if (neuronIndex == -1 && related != null)
            {
                //already here!  
                int[] found = SearchFor(related, parent);
                if (found.Length > 0)
                {
                    neuronIndex = found[0];
                    KBData[neuronIndex].lastActive = generation;
                    return neuronIndex;
                }
                if (parent == -1) return -1;
                neuronIndex = GetFreeNeuron(parent);
            }
            if (KBData.ContainsKey(neuronIndex) && neuronIndex != -1)
                newThing = KBData[neuronIndex];
            else
                newThing = new KBThing();

            newThing.neuronIndex = neuronIndex;
            newThing.lastActive = generation;
            newThing.label = label;
            if (related != null)
            {
                foreach (int kbi in related)
                {
                    if (KBData.TryGetValue(kbi, out KBThing kbt))
                    {
                        newThing.references.Add(kbt);
                        kbt.referencedBy.Add(newThing);
                    }
                }
            }
            if (child != -1)
            {
                if (KBData.TryGetValue(child, out KBThing kbt))
                {
                    newThing.children.Add(kbt);
                    kbt.parent = newThing;
                }
            }
            if (parent != -1)
            {
                if (KBData.TryGetValue(parent, out KBThing kbt))
                {
                    newThing.parent = kbt;
                    kbt.children.Add(newThing);
                }
            }
            if (previous != -1)
            {//this adds to the end and might handle an insertion some day
                if (KBData.TryGetValue(previous, out KBThing kbt))
                {
                    newThing.previous = kbt;
                    newThing.previous.next = newThing;
                }
            }
            if (KBData.ContainsKey(neuronIndex))
                KBData[neuronIndex] = newThing;
            else
                KBData.Add(neuronIndex, newThing);
            return neuronIndex;
        }

        int AddSequence(
            int neuronIndex,
            string label,
            int[] sequence,
            int parent = -1)
        {
            int retVal = AddNew(neuronIndex, label, new int[] { sequence[0] }, parent);
            for (int i = 1; i < sequence.Length; i++)
                AddNew(neuronIndex + i, label + i, new int[] { sequence[i] }, neuronIndex, -1, neuronIndex + i - 1);
            return retVal;
        }

        int[] SearchFor(int[] neuronIndices, int parent = -1)
        {
            List<int> retVal = new List<int>();

            foreach (KeyValuePair<int, KBThing> entry in KBData)
            {
                KBThing kbt = entry.Value;
                if (parent == -1 || (kbt.parent != null && kbt.parent.neuronIndex == parent))
                {
                    if (neuronIndices == null)
                    {
                        retVal.Add(entry.Key);
                    }
                    else
                    {
                        bool found = false; ;
                        for (int i = 0; i < neuronIndices.Length; i++)
                        {
                            found = false;
                            if (kbt.references.FindIndex(x => x.neuronIndex == neuronIndices[i]) == -1) break;
                            found = true;
                        }
                        if (found)
                        {
                            retVal.Add(entry.Key);
                        }
                    }
                }
            }
            return retVal.ToArray();
        }

        int[] SearchForSequence(int[] neuronIndices)
        {
            List<int> retVal = new List<int>();
            //find the first thing in the search, then follow the chain of nexts
            foreach (KeyValuePair<int, KBThing> entry in KBData)
            {
                bool found = false;
                KBThing kbt = entry.Value;
                for (int i = 0; i < neuronIndices.Length; i++)
                {
                    found = false;
                    if (kbt.references.FindIndex(x => x.neuronIndex == neuronIndices[i]) == -1) break;
                    if (kbt.next != null && i < neuronIndices.Length - 1)
                    {
                        kbt = kbt.next;
                    }
                    found = true;
                }
                if (found)
                {
                    if (neuronIndices.Length == 1)
                        retVal.Add(kbt.neuronIndex);
                    else
                        retVal.Add(kbt.parent.neuronIndex);
                }
            }
            return retVal.ToArray();
        }

        void DeleteThings(int parent)
        {
            foreach (KBThing kbt in KBData[parent].children)
                KBData.Remove(kbt.neuronIndex);
            KBData[parent].children.Clear();

        }




        string theInPhrase = "";
        string theOutPhrase = "";


        public override void Fire()
        {
            HandleSpeechIn();
            HandleSpeechOut();
            HandleVisionIn();
            HandleMotion();
            HandleTurning();
            generation++;
        }

        private void HandleVisionIn()
        {
            //vision is transient so we'll delete the existing visible things
            DeleteThings(500);
            NeuronArea naRectangles = theNeuronArray.FindAreaByLabel("Rectangles");
            if (naRectangles == null) return;
            for (int i = 1; i < naRectangles.Height; i++)
            {
                int color = naRectangles.GetNeuronAt(0, i).LastChargeInt;
                if (color == 0) break;
                string colorName = Utils.GetColorName(Utils.FromArgb(color));
                //color is a direct index...beware of eventual collisions with other data!
                //if there is no neuron for color...allocate one
                AddNew(color, colorName, null, 20000);
                int square = (int)naRectangles.GetNeuronAt(5, i).LastCharge; //this is a one-of set of neurons
                int rectangle = (int)naRectangles.GetNeuronAt(6, i).LastCharge;
                int shape = -1;
                string shapeString = "??";
                if (square == 1)
                {
                    shape = AddNew(103, "square", null, 100);
                    shapeString = "square";
                }
                if (rectangle == 1)
                {
                    shape = AddNew(101, "rectangle", null, 100);
                    shapeString = "rectangle";
                }
                int index = AddNew(-1, colorName + " " + shapeString, new int[] { color, shape }, 500);
            }
        }

        private void HandleSpeechIn()
        {
            //string input = na.GetParam("-i");
            NeuronArea naSpeechIn = theNeuronArray.FindAreaByLabel("SpeechIn");
            if (naSpeechIn == null) return;
            //new word?
            Neuron nNewWord = naSpeechIn.GetNeuronAt(0, 0);
            if (nNewWord.LastCharge == 1)
            {
                //find the new word (this might be simpler?)
                string word = "";
                naSpeechIn.BeginEnum();
                for (Neuron n = naSpeechIn.GetNextNeuron(); n != null; n = naSpeechIn.GetNextNeuron())
                {
                    if (!n.InUse()) break;
                    word = n.Label;
                }
                if (word != "start" && word != "stop")
                    AddWords(new string[] { word }, 1000);
            }

            //add any new words to the phrase
            //stop when "stop" is encountered
            naSpeechIn.BeginEnum();
            naSpeechIn.GetNextNeuron();//skip the first entry
            for (Neuron n = naSpeechIn.GetNextNeuron(); n != null; n = naSpeechIn.GetNextNeuron())
            {
                if (!n.InUse()) break;
                if (n.LastCharge == 1)
                {
                    if (n.Label == "stop")
                    {
                        ProcessPhrase();
                    }
                    else if (n.Label != "start")
                    {
                        if (theInPhrase != "") theInPhrase += " ";
                        theInPhrase += n.Label;
                        int[] Result = SearchForPhrase(theInPhrase, 2000);
                        //Debug.WriteLine("Phrase Found: " + string.Join(" ", Result));
                    }
                    break;
                }
            }
        }

        //speech out will migrate to a behavior module when it is created.
        void HandleSpeechOut()
        {
            if (theOutPhrase == "") return;

            string word = "";
            int sp = 0;
            theOutPhrase = theOutPhrase.Trim();
            while ((sp = theOutPhrase.IndexOf(' ')) == 0)
                theOutPhrase = theOutPhrase.Substring(1);
            //strip off the first word and pass it to the SpeechOut module
            if (sp == -1)
            {
                word = theOutPhrase;
                theOutPhrase = "";
            }
            else
            {
                word = theOutPhrase.Substring(0, sp + 1);
                theOutPhrase = theOutPhrase.Substring(sp + 1);
            }

            NeuronArea na = theNeuronArray.FindAreaByLabel("SpeechOut");
            if (na == null) return;
            int found = -1;
            na.BeginEnum();
            for (Neuron n = na.GetNextNeuron(); n != null; n = na.GetNextNeuron())
            {
                if (!n.InUse()) break;
                if (n.Label == word)
                {
                    found = n.Id;
                    break;
                }
            }
            if (found == -1)
            {
                Neuron nNew = na.GetFreeNeuron();
                if (nNew == null) return;  //SpeechOut module is full
                found = nNew.Id;
                nNew.Label = word;
                nNew.AddSynapse(nNew.Id, 0, MainWindow.theNeuronArray);
            }
            MainWindow.theNeuronArray.neuronArray[found].SetValue(1);
        }
        void Say(int phraseStart)
        {
            int[] Result = GetSequence(phraseStart);
            //here is a hack to get words from a sequence...the neuronIndices should be in the speechout or speechin arrays
            foreach (int i in Result)
            {
                string word = KBData[i].references[0].label;
                word = word.Substring(1);
                if (theOutPhrase != "")
                    theOutPhrase += " ";
                theOutPhrase += word;
            }
        }
        int turnCounter = 0;
        int motionCounter = 0;
        private void HandleMotion()
        {
            if (motionCounter == 0) return;
            NeuronArea naTurn = theNeuronArray.FindAreaByLabel("Move");
            if (naTurn == null) return;
            if (motionCounter > 0)
            {
                naTurn.GetNeuronAt(1).SetValue(1);
                motionCounter--;
            }
            if (motionCounter < 0)
            {
                naTurn.GetNeuronAt(0).SetValue(1);
                motionCounter++;
            }
        }
        private void HandleTurning()
        {
            if (turnCounter == 0) return;
            NeuronArea naTurn = theNeuronArray.FindAreaByLabel("Turn");
            if (naTurn == null) return;
            if (turnCounter > 0)
            {
                naTurn.GetNeuronAt(1).SetValue(1);
                turnCounter--;
            }
            if (turnCounter < 0)
            {
                naTurn.GetNeuronAt(0).SetValue(1);
                turnCounter++;
            }
        }
        void WhatIsBehindYou() //this is a complete hack and needs replacement
        {
            int count = 0;
            //for (int i = 0; i < thingsInReality.Count; i++)
            //{
            //    Thing t = thingsInReality[i];
            //    if (t.cx < -.9 || t.cx > .7) //we'll have to figure out why the assymetry
            //    {
            //        string colorName = Utils.GetColorName(Utils.FromArgb(t.color));
            //        string shapeName = (t.thingType == 0) ? " square" : " rectangle";
            //        if (count == 0)
            //            theOutPhrase = "Behind me, I remember a " + colorName + shapeName;
            //        else
            //            theOutPhrase += " and a " + colorName + shapeName;
            //        count++;
            //    }
            //}
            //if (count == 0)
            //    theOutPhrase = "I don't think anything is behind me.";
        }
        void ProcessPhrase()
        {
            if (theInPhrase == "") return;
            if (theInPhrase.IndexOf("say") == 0)
            {
                theInPhrase = theInPhrase.Substring(4);
                int ph = AddPhrase(theInPhrase, 2000, 1000);
                Say(ph);
            }
            else if (theInPhrase.IndexOf("what can you see") == 0)
            {
                WhatCanYouSee();
            }
            else if (theInPhrase.IndexOf("what is behind you") == 0)
            {
                WhatIsBehindYou();
            }
            else if (theInPhrase.IndexOf("turn") >= 0)
            {
                turnCounter = 1;
                string[] words = theInPhrase.Split(' ');
                if (words.Length > 2)
                {
                    int.TryParse(words[2], out turnCounter);
                }
                if (words.Length > 1)
                {
                    if (words[1] == "left")
                        turnCounter = -turnCounter;
                    if (words[1] == "around")
                        turnCounter = 137;
                }
            }
            else if (theInPhrase.IndexOf("move") == 0)
            {
                motionCounter = 1;
                string[] words = theInPhrase.Split(' ');
                if (words.Length > 2)
                {
                    int.TryParse(words[2], out motionCounter);
                }
                if (words.Length > 1)
                {
                    if (words[1] == "forward")
                        motionCounter = -motionCounter;
                }
            }
            else
            {
                int ph = AddPhrase(theInPhrase, 2000, 1000);
            }
            theInPhrase = "";
        }


        private void WhatCanYouSee()
        {
            theOutPhrase = "";
            if (KBData[500].children.Count != 0)
            {
                theOutPhrase = "I see";
                int count = KBData[500].children.Count;
                for (int i = 0; i < count; i++)
                {
                    if (count > 1 && i == count - 1)
                        theOutPhrase += " and";

                    KBThing kbt = KBData[500].children[i];
                    theOutPhrase += " a";
                    foreach (KBThing kbtCh in kbt.references)
                    {
                        theOutPhrase += " " + kbtCh.label;
                    }
                    if (i < count - 1)
                        theOutPhrase += ",";
                }
            }
            else
            {
                theOutPhrase = "I don't see anything right now.";
            }


        }


        public  override void Initialize()
        {
            AddNew(20000, "color");

            AddNew(100, "shape");
            AddNew(101, "rectangle", null, 100);
            AddNew(102, "circle", null, 100);
            AddNew(103, "square", null, 100);

            AddNew(500, "thingsInView");
            //AddNew(501, "red rect", new int[] { 0, 26 }, 27);
            //AddNew(502, "blue rect", new int[] { 1, 26 }, 27);

            int[] Result = SearchFor(new int[] { 0, 26 });
            Result = SearchFor(new int[] { 1, 26 });
            Result = SearchFor(new int[] { 2, 26 });

            AddNew(1000, "word");

        }
        //for testing only
        int Label(string label, int start)
        {
            for (int i = start; true; i++)
            {//if words are not sequential...you've found the last one
                if (KBData.TryGetValue(i, out KBThing kbt))
                {
                    if (kbt.label == label) return kbt.neuronIndex;
                }
                else
                    break;
            }
            return -1;
        }
        string CreateLabel(string word)
        {
            word = "w" + word.First().ToString().ToUpper() + word.Substring(1);
            return word;
        }
        int GetFreeEntry(int parent)
        {
            int neuronIndex = parent;
            while (KBData.ContainsKey(neuronIndex))
                neuronIndex++; //finds a free element // assumes no deletions
            return neuronIndex;
        }
        //for testing only
        void AddWords(string[] words, int parent)
        {
            int neuronIndex = GetFreeEntry(parent);
            for (int i = 0; i < words.Length; i++)
            {
                string newLabel = CreateLabel(words[i]);
                //does the word already exist?
                if (Label(newLabel, parent) == -1) //prevents adding words multiple times
                {
                    AddNew(neuronIndex++, newLabel, null, parent);
                }
            }
        }
        int[] GetSequence(int start)
        {
            List<int> RetVal = new List<int>();
            KBThing kbt = KBData[start];
            while (kbt != null)
            {
                RetVal.Add(kbt.neuronIndex);
                kbt = kbt.next;
            }
            return RetVal.ToArray();
        }

        //for testing only
        int AddPhrase(string phrase, int parent, int wordsStart)
        {
            int retVal = -1;
            int[] Result = SearchForPhrase(phrase, parent);
            string[] words = phrase.Split(' ');
            int neuronIndex = GetFreeEntry(parent);
            int[] wordIndices = new int[words.Length];
            for (int i = 0; i < words.Length; i++)
            {
                wordIndices[i] = Label(CreateLabel(words[i]), wordsStart);
            }
            bool found = false;
            for (int i = 0; i < Result.Length; i++)
            {
                found = false;
                int[] Sequence = GetSequence(Result[i]);
                if (Sequence.Length == wordIndices.Length)
                {
                    for (int j = 0; j < Sequence.Length; j++)
                    {
                        found = false;
                        if (wordIndices[j] != KBData[Sequence[j]].references[0].neuronIndex) break;
                        found = true;
                    }
                }
                if (found)
                {
                    retVal = Result[i];
                    break;
                }
            }
            if (!found)
                retVal = AddSequence(neuronIndex, "", wordIndices, parent);
            return retVal;
        }
        int[] SearchForPhrase(string phrase, int parent)
        {
            string[] words = phrase.Split(' ');
            int[] wordIndices = new int[words.Length];
            for (int i = 0; i < words.Length; i++)
            {
                wordIndices[i] = Label(CreateLabel(words[i]), 1000);
            }
            return SearchForSequence(wordIndices);
        }

    }
}
