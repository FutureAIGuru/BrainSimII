//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Speech.Recognition;
using System.Windows;

namespace BrainSimulator.Modules
{
    public class ModuleSpeechIn : ModuleBase
    {
        SpeechRecognitionEngine recognizer = null;

        public ModuleSpeechIn()
        {
            minHeight = 4;
            minWidth = 3;
        }

        //keeps the temporary phrase so it can be recognized across multiple engine cycles
        private List<string> words = new List<string>();

        public override void Fire()
        {
            Init();
            if (recognizer == null) return;

            //if a word is in the input queue...process one word
            if (words.Count > 0)
            {
                string word = words[0].ToLower();
                na.BeginEnum();
                int found = -1;
                Neuron n = null;
                for (n = na.GetNextNeuron(); n != null; n = na.GetNextNeuron())
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
                    n.SetValue(1);
                    //Debug.WriteLine("Fired Neuron for word: " + word);
                }

                words.RemoveAt(0);
            }
        }

        private void StartRecognizer()
        {
            // Create an in-process speech recognizer for the en-US locale.  
            recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
            if (recognizer == null)
            {
                MessageBox.Show("Could not open speech recognition engine.");
                return;
            }
            CreateGrammar();

            // Add a handler for the speech recognized event.  
            recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(Recognizer_SpeechRecognized);

            // Configure input to the speech recognizer.  
            try
            {
                recognizer.SetInputToDefaultAudioDevice();
            }
            catch (Exception e)
            {
                MessageBox.Show("Speech Recognition could not start because: " + e.Message);
                return;
            }
            //// Start asynchronous, continuous speech recognition.  
            recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        private void CreateGrammar()
        {

            //// Create and load a dictation grammar.  This handles for many words and doesn't work very well
            //recognizer.LoadGrammar(new DictationGrammar());

            // create a small custom grammar for testing
            Choices reward = new Choices("good", "no");
            Choices color = new Choices("black", "red", "blue", "green", "orange", "white");
            Choices size = new Choices("big", "medium", "little", "long", "short");
            Choices distance = new Choices("near", "nearer", "nearest", "far", "farther", "farest");
            Choices attrib = new Choices(new GrammarBuilder[] { color, distance, size });
            Choices shape = new Choices("dot", "line");

            Choices attribs = new Choices("color", "size", "distance", "shape");

            Choices action = new Choices("go", "stop", "turn", "move");
            Choices direction = new Choices("right", "left", "forward", "backwards", "around");

            Choices command = new Choices("this is", "this is a", "this is the", "this is an");
            Choices query = new Choices("what can you see", "what is behind you", "what is this");

            GrammarBuilder attribQuery = new GrammarBuilder();
            attribQuery.Append("what");
            attribQuery.Append(attribs);
            attribQuery.Append("is this");


            GrammarBuilder say = new GrammarBuilder();
            say.Append("say");
            say.AppendDictation();

            GrammarBuilder declaration = new GrammarBuilder();
            declaration.Append(command);
            declaration.Append(attrib, 0, 4);
            declaration.Append(shape, 0, 1);

            GrammarBuilder actionCommand = new GrammarBuilder();
            actionCommand.Append(action);
            actionCommand.Append(direction, 0, 1);

            Choices digit = new Choices("1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "point");
            GrammarBuilder number = new GrammarBuilder();
            number.Append(digit, 1, 5);

            Choices commands = new Choices(reward, say, attribQuery, declaration, actionCommand, query, number);
            Choices attentionWord = new Choices("Sallie", "Computer");

            GrammarBuilder a = new GrammarBuilder();
            a.Append(attentionWord, 1, 1);
            a.Append(commands);
            a.Append(digit);


            //some words we might need some day
            //Choices article = new Choices("a", "an", "the", "some", "containing", "with", "which are");
            //Choices emotion = new Choices("ecstatic", "happy", "so-so", "OK", "sad", "unhappy");
            //Choices timeOfDay = new Choices("morning", "afternoon", "evening", "night");

            //someday we'll need numbers
            //Choices number = new Choices();
            //for (int i = 1; i < 200; i++)
            //    number.Add(i.ToString());

            //how to add singular/plural to choices
            //PluralizationService ps = PluralizationService.CreateService(new CultureInfo("en-us"));
            //string[] attribList = new string[] { "attributes", "sequences", "colors", "sizes", "shapes", "digits", "things" };
            //string[] attribList1 = new string[attribList.Length];
            //for (int i = 0; i < attribList.Length; i++)
            //    attribList1[i] = ps.Singularize(attribList[i]);


            //how to specify a custom pronunciation with SRGS--these don't integrate with the rest of the grammar
            //SrgsItem cItem = new SrgsItem();
            //SrgsToken cWord = new SrgsToken("computer"); 
            //cWord.Pronunciation = "kəmpjutər";
            //cItem.Add(cWord);
            //SrgsRule srgsRule = new SrgsRule("custom", cItem);
            //SrgsDocument tokenPron = new SrgsDocument(srgsRule);
            //tokenPron.PhoneticAlphabet = SrgsPhoneticAlphabet.Ipa;
            //Grammar g_Custom = new Grammar(tokenPron);
            //recognizer.LoadGrammar(g_Custom);


            //get the words from the grammar and label neurons
            string c = a.DebugShowPhrases;
            c = c.Replace((char)0x2018, ' ');
            c = c.Replace((char)0x2019, ' ');
            string[] c1 = c.Split(new string[] { "[", ",", "]", " " }, StringSplitOptions.RemoveEmptyEntries);
            c1 = c1.Distinct().ToArray();

            //int i1 = 1;
            //na.BeginEnum();
            //for (Neuron n = na.GetNextNeuron(); n != null && i1 < c1.Length; i1++, n = na.GetNextNeuron())
            //    n.Label = c1[i1].ToLower();

            Grammar gr = new Grammar(a);
            recognizer.LoadGrammar(gr);
            //            gr = new Grammar(reward);
            //            recognizer.LoadGrammar(gr);
        }

        // Handle the SpeechRecognized event.  
        //WARNING: this could be asynchronous to everything else
        void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            string text = e.Result.Text;

            //get the audio to do something
/*            RecognizedAudio ra = e.Result.GetAudioForWordRange(e.Result.Words[0], e.Result.Words[e.Result.Words.Count-1]);
            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            ra.WriteToAudioStream(stream);
            stream.Position = 0;
            byte[] rawBytes = new byte[stream.Length];
            stream.Read(rawBytes, 0, (int)stream.Length);
            System.IO.FileStream fStream = new System.IO.FileStream(@"C:\Users\c_sim\Documents\BrainSim\xx.wav",System.IO.FileMode.CreateNew);
            ra.WriteToAudioStream(fStream);
            fStream.Close();
*/

            string debug = "";
            foreach (RecognizedWordUnit w in e.Result.Words)
                debug += w.Text + "(" + w.Confidence + ") ";
            bool anyLowConfidence = false;
            Debug.WriteLine("To remove the warning about not being used..." + anyLowConfidence);
            float minConfidence = .91f;
            if (text.IndexOf("Sallie say") == 0)
                minConfidence = .6f;
            foreach (RecognizedWordUnit word in e.Result.Words)
            {
                if (word.Confidence < minConfidence) anyLowConfidence = true;
            }
            //if (e.Result.Confidence < .9 || e.Result.Words[0].Confidence < .92 || anyLowConfidence)
            //{
            //    //System.Media.SystemSounds.Asterisk.Play();
            //    Debug.WriteLine("Words Detected: " + debug + " IGNORED");
            //    return;
            //}
            Debug.WriteLine("Words Detected: " + debug);

            //use this to work with pronunciations instead of words
            //foreach (RecognizedWordUnit word in e.Result.Words)
            //{
            //    words.Add(word.Pronunciation);
            //}
            //return;

            string[] tempWords = text.Split(' ');
            foreach (string word in tempWords)
            {
                if (word.ToLower() != "sallie" && word.ToLower() != "computer")
                    words.Add(word.ToLower());
            }
            ModuleHearWords nmHear = (ModuleHearWords)FindModuleByType(typeof(ModuleHearWords));
            if (nmHear != null)
            {
                String phrase = e.Result.Text;
                if (e.Result.Words.Count != 1)
                {
                    int i = phrase.IndexOf(' ');
                    phrase = phrase.Substring(i + 1);
                }
                nmHear.HearPhrase(phrase);
            }
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

        public override void SetUpAfterLoad()
        {
            base.SetUpAfterLoad();
            Init();
            Initialize();
        }

        public override void Initialize()
        {
            ClearNeurons();
            AddLabel("1");
            AddLabel("2");
            AddLabel("3");
            AddLabel("4");
            AddLabel("5");
            AddLabel("6");
            AddLabel("7");
            AddLabel("8");
            AddLabel("9");
            AddLabel("0");
            AddLabel("point");

            if (recognizer != null)
            {
                recognizer.RecognizeAsyncStop();
                recognizer.Dispose();
            }

            StartRecognizer();
        }


    }
}
