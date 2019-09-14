using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Recognition;
using System.Diagnostics;
using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;


namespace BrainSimulator
{
    public partial class NeuronArray
    {

        SpeechRecognitionEngine recognizer = null;

        //the first neuron in the array flags a new word being added

        //keeps the temporary phrase so it can be recognized across multiple engine cycles
        List<string> words = new List<string>();
        int delay = 0;//used in countdown by the old phrase recognizer

        public void SpeechIn(NeuronArea na)
        {
            //initialize the recognizer if necessary
            if (recognizer == null)
            {
                // Create an in-process speech recognizer for the en-US locale.  
                recognizer = new SpeechRecognitionEngine(
                  new System.Globalization.CultureInfo("en-US"));
                Grammar gr = CreateGrammar();

                recognizer.LoadGrammar(gr);

                //// Add a handler for the speech recognized event.  
                recognizer.SpeechRecognized +=
                  new EventHandler<SpeechRecognizedEventArgs>(recognizer_SpeechRecognized);

                //// Configure input to the speech recognizer.  
                recognizer.SetInputToDefaultAudioDevice();

                //// Start asynchronous, continuous speech recognition.  
                recognizer.RecognizeAsync(RecognizeMode.Multiple);
               // Neuron naNew = na.GetNeuronAt(0, 0);
                //naNew.AddSynapse(naNew.Id, 0, this, false); //without a synapse, the neuron is not in use
             //   naNew.Label = "++";
            }

            //if a word is in the input queue...process one word
            if (words.Count > 0)
            {
                string word = words[0].ToLower();
                na.BeginEnum();
                int found = -1;
                for (Neuron n = na.GetNextNeuron(); n != null; n = na.GetNextNeuron())
                {
                    if (n.Label == word)
                    {
                        found = n.Id;
                        break;
                    }
                    if (n.Label == "") //neuron isn't used yet
                    {
                        n.Label = word;
                        found = n.Id;
                        break;
                    }
                }
                if (found != -1) //perhaps the array was full
                {
                    //                neuronArray[found].LastCharge = 1;//SetValue(1);
                    neuronArray[found].CurrentCharge = 1;//.SetValue(1);
                    Debug.WriteLine("Fired Neuron for word: " + word);
                }

                words.RemoveAt(0);
            }
        }

        private static Grammar CreateGrammar()
        {
            //// Create and load a dictation grammar.  This is for many words and doesn't work very well
            //recognizer.LoadGrammar(new DictationGrammar());

            // create a small custom grammar for testing
            Choices digit = new Choices("1", "2", "3", "4", "5", "6", "7", "8", "9", "0","point");
            Choices number = new Choices();
            for (int i = 1; i < 200; i++)
                number.Add(i.ToString());
            Choices emotion = new Choices("ecstatic", "happy", "so-so", "OK", "sad", "unhappy");
            Choices timeOfDay = new Choices("morning", "afternoon", "evening", "night");
            Choices color = new Choices("black", "gray", "pink", "red", "blue", "orange","white");
            Choices shape = new Choices("square", "rectangle", "circle");
            Choices direction = new Choices("right", "left", "forward", "backwards");
            Choices size = new Choices("big", "medium", "little");
            Choices motion = new Choices("move", "turn");
            Choices sequence = new Choices("pi", "mary");

            Choices command = new Choices("Computer", "Computer Say", "Computer what can you see?", "Computer What is behind you?", "Computer turn around", "Computer what attributes do you know","Computer what is");
            Choices query = new Choices("what is", "add","name");
            Choices article = new Choices("a", "an", "the", "some","containing","with","which are");
            Choices words = new Choices("mary", "had", "a", "little", "lamb");

            //GrammarBuilder a0 = new GrammarBuilder(command);
            //GrammarBuilder a1 = new GrammarBuilder(command);
            //a1.Append("Good");
            //a1.Append(timeOfDay);
            //GrammarBuilder a2 = new GrammarBuilder(command);
            //a2.Append("this is a");
            //a2.Append(color);
            //a2.Append(shape);
            //GrammarBuilder a3 = new GrammarBuilder(command);
            //a3.Append("How are You");
            //GrammarBuilder a4 = new GrammarBuilder(command);
            //a4.Append(motion);
            //a4.Append(direction);
            //GrammarBuilder a4a = new GrammarBuilder(command);
            //a4a.Append(motion);
            //a4a.Append(direction);
            //a4a.Append(number);

            //GrammarBuilder d1 = new GrammarBuilder(command);
            //d1.Append(digit);
            //GrammarBuilder d2 = new GrammarBuilder(command);
            //d2.Append(digit);
            //d2.Append(digit);
            //GrammarBuilder d3 = new GrammarBuilder(command);
            //d3.Append(digit);
            //d3.Append(digit);
            //d3.Append(digit);
            //GrammarBuilder d4 = new GrammarBuilder(command);
            //d4.Append(digit);
            //d4.Append(digit);
            //d4.Append(digit);
            //d4.Append(digit);
            //GrammarBuilder d5 = new GrammarBuilder(command);
            //d5.Append(digit);
            //d5.Append(digit);
            //d5.Append(digit);
            //d5.Append(digit);
            //d5.Append(digit);
            //GrammarBuilder d6 = new GrammarBuilder(command);
            //d6.Append(digit);
            //d6.Append(digit);
            //d6.Append(digit);
            //d6.Append(digit);
            //d6.Append(digit);
            //d6.Append(digit);
            //GrammarBuilder d10 = new GrammarBuilder(command);
            //d10.Append(digit);
            //d10.Append(digit);
            //d10.Append(digit);
            //d10.Append(digit);
            //d10.Append(digit);
            //d10.Append(digit);
            //d10.Append(digit);
            //d10.Append(digit);
            //d10.Append(digit);
            //d10.Append(digit);
            //            Choices choices = new Choices(new GrammarBuilder[] { a0, a1, a2, a3, a4, a4a, d1, d2, d3, d4, d5, d6, d10 });
            PluralizationService ps = PluralizationService.CreateService(new CultureInfo("en-us"));

            string[] attribList = new string[] { "attributes", "sequences", "colors", "sizes", "shapes", "digits", "things" };
            string[] attribList1 = new string[attribList.Length];
            for(int i = 0; i < attribList.Length; i++)
                attribList1[i] = ps.Singularize(attribList[i]);

            List<GrammarBuilder> gb = new List<GrammarBuilder>();
            GrammarBuilder a = new GrammarBuilder("Computer");
            a.Append(query,0,1);
            a.Append(article, 0, 1);
            a.Append(sequence, 0, 1);
            a.Append(new Choices(attribList), 0, 1);
            a.Append(new Choices(attribList1), 0, 1);
            a.Append(article, 0, 1);
            a.Append(color,0,1);
            a.Append(shape, 0, 1);
            a.Append(size, 0, 1);
            a.Append(digit, 0, 4);
            a.Append(words, 0, 5);
            gb.Add(a);

            Choices choices = new Choices(gb.ToArray());
            Grammar gr = new Grammar((GrammarBuilder)choices);
            GrammarBuilder g1 = new GrammarBuilder();

            return gr;
        }

        // Handle the SpeechRecognized event.  
        //WARNING: this could be asynchronous to everything else
        void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            string text = e.Result.Text;
            float i = e.Result.Confidence;
            if (i < .5) return;
            string[] tempWords = text.Split(' ');
           // words.Add("start");
            foreach (string word in tempWords)
            {
                if (word.ToLower() != "computer")
                    words.Add(word.ToLower());
            }
           // words.Add("stop");
            Debug.WriteLine("Words Detected: " + text);
        }

        public void PauseRecognition()
        {
            if (recognizer != null)
                recognizer.RecognizeAsyncStop();
        }

        public void ResumeRecognition()
        {
            if (recognizer != null)
                if (recognizer.AudioState == AudioState.Stopped)
                    recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        public void PhraseBuffer(NeuronArea na)
        {
            string input = na.GetParam("-i");
            NeuronArea naIn = FindAreaByLabel(input);
            if (naIn == null) return;

            // Add any new word to the phrase buffer
            naIn.BeginEnum();
            for (Neuron n = naIn.GetNextNeuron(); n != null; n = naIn.GetNextNeuron())
            {
                if (n.LastCharge > .90)
                {
                    if (n.Label == "start") //start of a new phrase
                        na.ClearNeuronArea();

                    Neuron n1 = na.GetFreeNeuron();
                    if (n1 != null)
                        n1.AddSynapse(n.Id, 1, this, false);
                    break;
                }
            }
        }

        public void PhraseStore(NeuronArea na)
        {
            string input = na.GetParam("-i");
            NeuronArea naIn = FindAreaByLabel(input);
            if (naIn == null) return;
            string output = na.GetParam("-o");
            NeuronArea naOut = FindAreaByLabel(output);
            if (naOut == null) return;

            //Now check the phrase buffer
            if (words.Count == 0 && neuronArray[naIn.FirstNeuron].InUse() && naIn.NeuronsFiredInArea() == 0)
            {
                //this lets the search percolate through before testing the result
                if (delay < 2)
                {
                    delay++;
                    return;
                }
                delay = 0;
                //phrase is complete..search in the stored Phrases 
                //is the phrase already in the phrases store?
                int found = naOut.NeuronsFiredInArea();
                if (found == 0)
                {
                    //add the phrase to the phrases store
                    Neuron nFound = naOut.GetFreeNeuron();
                    if (nFound != null)
                    {
                        //build the recognition sequence
                        float phraseLength = naIn.NeuronsInUseInArea();
                        Neuron previousNeuron = null;
                        naIn.BeginEnum();
                        for (Neuron n = naIn.GetNextNeuron(); n != null; n = naIn.GetNextNeuron())
                        {
                            if (n.synapses.Count < 1) break;
                            float weight = 0.5f;
                            if (previousNeuron == null)
                                weight = 1;
                            //add the phrase to the phrases store
                            Neuron newNeuron = na.GetFreeNeuron();
                            Neuron wordNeuron = neuronArray[n.synapses[0].TargetNeuron];
                            if (newNeuron == null) break;
                            newNeuron.AddSynapse(nFound.Id, 1 / phraseLength, this, false);
                            wordNeuron.AddSynapse(newNeuron.Id, weight, this, false);
                            if (previousNeuron != null)
                                previousNeuron.AddSynapse(newNeuron.Id, weight, this, false);
                            previousNeuron = newNeuron;
                        }
                    }
                }
                naIn.ClearNeuronArea();
                naOut.ClearNeuronChargeInArea();
                naIn.ClearNeuronChargeInArea();
                na.ClearNeuronChargeInArea();

                //this little hack reduces the currentCharge.  This keeps 13 and 123 from being the same without extra work
                na.BeginEnum();
                for (Neuron n = na.GetNextNeuron(); n != null; n = na.GetNextNeuron())
                    n.CurrentCharge *= 0.9f;
            }
        }

        public void Phrase(NeuronArea na)
        {
            NeuronArea naIn = FindAreaByCommand("SpeechIn");
            NeuronArea naOut = FindAreaByCommand("SpeechOut");
            if (na == null || naIn == null || naOut == null) return;

            NeuronArea naP = FindAreaByLabel("Phrases");
            NeuronArea naF = FindAreaByLabel("PhraseFound");
            NeuronArea naS = FindAreaByLabel("SpeakPhrase");
            if (naP == null || naF == null) return;


            Neuron n1 = na.GetFreeNeuron();

            // Add any new word to the phrase buffer
            naIn.BeginEnum();
            for (Neuron n = naIn.GetNextNeuron(); n != null; n = naIn.GetNextNeuron())
            {
                if (n.LastCharge > .90 && n1 != null)
                {
                    n1.AddSynapse(n.Id, 1, this, false);
                    //add an output neuron with the same label as the input
                    naOut.BeginEnum();
                    for (Neuron nOut = naOut.GetNextNeuron(); nOut != null; nOut = naOut.GetNextNeuron())
                    {
                        if (n.Label == "start") naF.ClearNeuronChargeInArea();
                        if (nOut.Label == n.Label)
                        {
                            n1.AddSynapse(nOut.Id, 1, this, false);
                            break;
                        }
                        if (!nOut.InUse())
                        {
                            nOut.Label = n.Label;
                            n1.AddSynapse(nOut.Id, 1, this, false);
                            break;
                        }
                    }
                    break;
                }
            }

            //Now check the phrase buffer
            if (words.Count == 0 && neuronArray[na.FirstNeuron].InUse() && naIn.NeuronsFiredInArea() == 0)
            {
                //this lets the search percolate through before testing the result
                if (delay == 0)
                {
                    delay++;
                    return;
                }
                delay = 0;
                //phrase is complete..search in the stored Phrases 
                //is the phrase already in the phrases store?
                int found = naF.NeuronsFiredInArea();
                if (found == 0)
                {
                    //add the phrase to the phrases store
                    Neuron nFound = naF.GetFreeNeuron();
                    Neuron nSpeak = naS.GetFreeNeuron();
                    if (nFound != null && nSpeak != null)
                    {
                        nFound.AddSynapse(nSpeak.Id, 1, this, false);
                        //build the recognition sequence
                        float phraseLength = na.NeuronsInUseInArea();
                        Neuron previousNeuron = null;
                        na.BeginEnum();
                        for (Neuron n = na.GetNextNeuron(); n != null; n = na.GetNextNeuron())
                        {
                            if (n.synapses.Count < 1) break;
                            float weight = 0.5f;
                            if (previousNeuron == null)
                                weight = 1;
                            //add the phrase to the phrases store
                            Neuron newNeuron = naP.GetFreeNeuron();
                            Neuron wordNeuron = neuronArray[n.synapses[0].TargetNeuron];
                            if (newNeuron == null) break;
                            newNeuron.AddSynapse(nFound.Id, 1 / phraseLength, this, false);
                            wordNeuron.AddSynapse(newNeuron.Id, weight, this, false);
                            if (previousNeuron != null)
                                previousNeuron.AddSynapse(newNeuron.Id, weight, this, false);
                            previousNeuron = newNeuron;
                        }
                        //build the playback sequence
                        previousNeuron = null;
                        na.BeginEnum();
                        int count = 0;
                        for (Neuron n = na.GetNextNeuron(); n != null; n = na.GetNextNeuron())
                        {
                            //skip the first and last...start and stop markers
                            count++;
                            if (count == 1) continue;
                            if (count == (int)phraseLength) break;
                            if (n.synapses.Count < 1) break;
                            //add the phrase to the phrases store
                            Neuron newNeuron = naP.GetFreeNeuron();
                            if (newNeuron == null) break;
                            newNeuron.AddSynapse(n.synapses[1].TargetNeuron, 1, this, false);
                            if (previousNeuron != null)
                                previousNeuron.AddSynapse(newNeuron.Id, 1, this, false);
                            else
                                nSpeak.AddSynapse(newNeuron.Id, 1, this, false);
                            previousNeuron = newNeuron;
                        }
                    }
                }
                na.ClearNeuronArea();
                naP.ClearNeuronChargeInArea();
                naIn.ClearNeuronChargeInArea();
            }
            //this little hack reduces the currentCharge.  This keeps 13 and 123 from being the same without extra work
            naP.BeginEnum();
            for (Neuron n = naP.GetNextNeuron(); n != null; n = naP.GetNextNeuron())
                n.CurrentCharge *= 0.9f;
        }

    }
}