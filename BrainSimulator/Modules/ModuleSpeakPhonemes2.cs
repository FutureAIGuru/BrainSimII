//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using System.Speech.Synthesis;
using System.Diagnostics;

using System.Speech.Recognition; //needed to get pronunciation from text
using System.Speech.Recognition.SrgsGrammar;
using System.IO;

namespace BrainSimulator.Modules
{
    public class ModuleSpeakPhonemes2 : ModuleBase
    {
        SpeechSynthesizer synth = null;
        string phraseToSpeak = "";
        bool validating = false;

        public override void Fire()
        {
            Init();  //be sure to leave this here
            if (synth == null) return;

            if (GetNeuronValue("Cancel") == 1)
            {
                synth.SpeakAsyncCancelAll();
            }
            if (GetNeuronValue("Validate") == 1)
            {
                if (!validating)
                {
                    hitWords.Clear();
                    missWords.Clear();
                    missPhrase.Clear();
                    hit = 0;
                    miss = 0;
                }
                validating = true;
            }
            else
            {
                if (validating)
                {
                    if (hit + miss == 0)
                        Debug.WriteLine("No Validation Data");
                    else
                    {
                        Debug.WriteLine("Validation: " + hit + " / " + miss + " = " + 100 * hit / (hit + miss));
                        Debug.WriteLine("Validation: " + hitWords.Count + " / " + missWords.Count + " = " + 100 * hitWords.Count / (hitWords.Count + missWords.Count));
                    }
                }
                validating = false;
            }

            bool paused = true;
            for (int i = 3; i < na.NeuronCount; i++)
            {
                Neuron n = na.GetNeuronAt(i);
                if (n.Fired())
                {
                    if (n.Label.Length == 1)
                    {
                        phraseToSpeak += n.Label;
                        paused = false;
                    }
                    if (n.synapses.Count == 0)
                    {
                        //connect it to the knowledge store
                        //connection to KB 
                        //ModuleUKS2 nmKB = (ModuleUKS2)FindModuleByName("AudibleUKS");
                        if (FindModuleByName("AudibleUKS") is ModuleUKS2 UKS)
                        {
                            string label = "pn" + n.Label;
                            List<Thing> phonemes = UKS.Labeled("Phoneme").Children;
                            Thing pn = UKS.Labeled(label, phonemes);
                            if (pn == null) //this should always be null
                            {
                                pn = UKS.AddThing(label, new Thing[] { UKS.Labeled("Phoneme") }, pn);
                            }
                            Neuron n1 = UKS.GetNeuron(pn);
                            Neuron n2 = UKS.GetNeuron(pn, false);
                            if (n1 != null)
                            {
                                n.AddSynapse(n1.Id, 1);
                                n1.SetValue(1);
                                n2.AddSynapse(n.Id, 1);
                            }
                        }
                    }
                }
            }
            if (phonemesToFire != "")
            {
                char c = phonemesToFire[0];
                bool fired = false;
                if (c != ' ')
                {
                    for (int i = 0; i < na.NeuronCount; i++)
                    {
                        Neuron n = na.GetNeuronAt(i);
                        if (n.Label == c.ToString())
                        {
                            n.SetValue(1);
                            fired = true;
                            break;
                        }
                    }
                    if (!fired)
                    {
                        Neuron n = AddLabel(c.ToString());
                        //connect it to the knowledge store
                        //connection to KB 
                        //ModuleUKS2 nmKB = (ModuleUKS2)FindModuleByName("AudibleUKS");
                        if (FindModuleByName("AudibleUKS") is ModuleUKS2 UKS)
                        {
                            string label = "pn" + n.Label;
                            List<Thing> phonemes = UKS.Labeled("Phoneme").Children;
                            Thing pn = UKS.Labeled(label, phonemes);
                            if (pn == null) //this should always be null
                            {
                                pn = UKS.AddThing(label, new Thing[] { UKS.Labeled("Phoneme") }, pn);
                            }
                            Neuron n1 = UKS.GetNeuron(pn);
                            Neuron n2 = UKS.GetNeuron(pn, false);
                            if (n1 != null)
                            {
                                n.AddSynapse(n1.Id, 1);
                                n2.AddSynapse(n.Id, 1);
                                n.SetValue(1);
                            }
                        }
                    }
                }
                phonemesToFire = phonemesToFire.Substring(1);
            }

            if (paused && phraseToSpeak != "")
            {
                if (dlg != null)
                    ((ModuleSpeakPhonemes2Dlg)dlg).SetLabel(phraseToSpeak);

                if (na.GetNeuronAt("Enable").Fired())
                {
                    ModuleSpeechIn msi = (ModuleSpeechIn)FindModuleByType(typeof(ModuleSpeechIn));
                    if (msi != null)
                        msi.PauseRecognition(); //if there is a recognizer active
                                                //synth.SpeakAsync(phraseToSpeak + ".");
                                                //phraseToSpeak = "";

                    PromptBuilder pb1 = new PromptBuilder();
                    if (typedIn)
                    {
                        pb1.StartVoice("Microsoft David Desktop");
                        pb1.StartStyle(new PromptStyle(PromptRate.Medium));
                    }
                    else
                    {
                        pb1.StartVoice("Microsoft Zira Desktop");
                        pb1.StartStyle(new PromptStyle(PromptRate.Slow));
                    }

                    pb1.AppendTextWithPronunciation("not used", phraseToSpeak.Trim());
                    pb1.EndStyle();
                    pb1.EndVoice();
                    string x = pb1.ToXml();
                    Debug.WriteLine(debugString(phraseToSpeak));
                    //synth.Speak(pb1);
                    synth.SpeakAsync(pb1);
                }
                //string heard = GetPronunciationFromText("", phraseToSpeak); //it would be nice to hear what was said but it doesn't work with this engine
                phraseToSpeak = "";
                typedIn = false;
            }
        }

        private string debugString(string t)
        {
            string t1 = "";
            foreach (char c in t)
            {
                t1 += ' ' + c.ToString() + " 0x" + ((int)c).ToString("x4");
            }
            return t1;
        }

        public override void SetUpAfterLoad()
        {
            Init();
            base.SetUpAfterLoad();
            Initialize();
        }

        static public List<string> combiners = new List<string>();

        private void Synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            // Restart speech recognition.  
            ModuleSpeechIn msi = (ModuleSpeechIn)FindModuleByType(typeof(ModuleSpeechIn));
            if (msi != null)
                msi.ResumeRecognition();
        }

        //we'll use this to get nearby phonemes
        public float PhonemeDistance(char ph1, char ph2)
        {
            float retVal = -1;
            FindPhoneme(ph1, out bool vowel1, out int row1, out int col1, out int offset1);
            FindPhoneme(ph2, out bool vowel2, out int row2, out int col2, out int offset2);
            if (vowel1 == vowel2)
            {
                retVal = Math.Abs(offset2 - offset1) + 5 * Math.Abs(row2 - row1) + 2 * Math.Abs(col2 - col1);
            }
            return retVal;
        }

        private bool FindInArray(string[,] array, char c, out int row, out int col, out int offset)
        {
            row = -1;
            col = -1;
            offset = -1;
            int maxi = array.GetUpperBound(0);
            int maxj = array.GetUpperBound(1);
            for (int i = 0; i <= maxi; i++)
                for (int j = 0; j <= maxj; j++)
                {
                    string s = array[i, j];
                    int index = s.IndexOf(c);
                    if (index != -1)
                    {
                        row = i;
                        col = j;
                        offset = index;
                        return true;
                    }
                }
            return false;
        }
        private void FindPhoneme(char s, out bool vowel, out int row, out int col, out int offset)
        {
            vowel = false;
            if (FindInArray(vowels1, s, out row, out col, out offset))
            {
                vowel = true;
                return;
            }
            FindInArray(consonants1, s, out row, out col, out offset);
        }

        string[,] vowels1 = new string[7, 5]
        {
                {"iy","","ɨʉ","","ɯu"},
                {"","ɪʏ","","ʊ",""},
                {"eø","","ɘɵ","","ɤo"},
                {"","","ə","",""},
                {"ɛœ","","ɜɞ","","ʌɔ"},
                {"æ","","ɐ","",""},
                {"aɶ","","ɑɒ","",""},
            };
        string[,] consonants1 = new string[9, 11]
        {
                {"ʍ","w","ɥ","ʜ","ʢ","ʡ","ɕʑ","ɺ","ɧ","",""},
                {"pb","","","td","","ʈɖ","cɟ","k\u0067","qɢ","","ʔ"},//ɡ
                {"m","ɱ","","n","","ɳ","ɲ","ŋ","ɴ","",""},
                {"ʙ","","","r","","","","ʀ","","",""},
                {"","ɟ","","ɾ","","ɽ","","","","",""},
                {"ɸβ","fv","θð","sz","ʃʒ","ʂʐ","çʝ","xɣ","χʁ","ħʕ","hɦ"},
                {"","","","ɬɮ","","","","","","",""},
                {"","ʋ","","ɹ","","ɻ","j","ɰ","","",""},
                {"","","","l","","ɭ","ʎ","ʟ","","\u0361","."}, //the last 2 are needed to control tts
        };

        string phonemesToFire = "";
        bool typedIn = false;
        int hit = 0;
        int miss = 0;
        List<string> hitWords = new List<String>();
        List<string> missWords = new List<String>();
        List<string> missPhrase = new List<String>();
        public void FirePhonemes(string phonemes)
        {
            if (validating)
            {
                string[] words = phonemes.Trim().Split(' ');
                if (FindModuleByName("AudibleUKS") is ModuleUKS2 aUKS)
                {
                    ((ModuleSpeakPhonemes2Dlg)dlg).SetLabel(phonemes);
                    List<Thing> allWords = aUKS.Labeled("Word").Children;
                    List<Thing> phrases = aUKS.Labeled("Phrase").Children;
                    List<Thing> theWord = new List<Thing>();
                    List<Thing> thePhrase = new List<Thing>();
                    string localMissWords = "";
                    foreach (string word in words)
                    {
                        theWord.Clear();
                        foreach (char c in word)
                            theWord.Add(aUKS.Labeled("pn" + c));
                        Thing t = aUKS.ReferenceMatch(theWord, allWords);
                        if (t == null)
                        {
                            if (!missWords.Contains(word)) missWords.Add(word);
                            localMissWords += word + " ";
                            miss++;
                        }
                        else
                        {
                            thePhrase.Add(t);
                            if (!hitWords.Contains(word)) hitWords.Add(word);
                            hit++;
                        }
                    }
                    Thing p = aUKS.ReferenceMatch(thePhrase, phrases);
                    if (p == null)
                    {
                        missPhrase.Add(phonemes);
                        missPhrase.Add(localMissWords);
                        Thing bestPhrase = phrases[0];
                        int bestScore = 0;
                        foreach (Thing phrase in phrases)
                        {
                            int score = Score(thePhrase, phrase);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestPhrase = phrase;
                            }
                        }
                        string bestPhraseText = bestPhrase.Label + " ";
                        foreach (Link l in bestPhrase.References)
                        {
                            Thing word = l.T;
                            if (word.Label[0] == 'p')
                            {
                                bestPhraseText += word.Label.Substring(2);
                            }
                            else
                            {
                                foreach (Link l1 in word.References)
                                {
                                    Thing phoneme = l1.T;
                                    bestPhraseText += phoneme.Label[2];
                                }
                                bestPhraseText += " ";
                            }
                        }
                        missPhrase.Add(bestPhraseText);
                    }

                    }
            }
            else
            {
                typedIn = true;
                //phonemesToFire = phonemes.Replace(" ", "");
                phonemesToFire += phonemes.Replace(" ", "") + "   ";
            }
        }
        private int Score (List<Thing> testPhrase, Thing phrase)
        {
            int retVal = 0;
            for (int i = 0; i < testPhrase.Count; i++)
            {
                for (int j = 0; j < phrase.References.Count; j++)
                {
                    if (testPhrase[i] == phrase.References[j].T)
                    {
                        retVal += 10;
                        retVal -= Math.Abs(j - i);
                    }
                }
            }
            return retVal;
        }

        public void DisableOutput()
        {
            Neuron n = GetNeuron("Enable");
            n.LastCharge = 0;
            SetNeuronValue("Enable", 0);
        }
        public void EnableOutput()
        {
            SetNeuronValue("Enable", 1);
        }

        //BELOW CAN BE USED TO GET PHONEMES FROM A WORD so the test phrases can be input as text and fed to Sallie as phonemes
        //It was pulled in from the web but seems to work for most cases.  
        public static string recoPhonemes;

        public static string GetPronunciationFromText(string MyWord, string Pron = null)
        {
            //this is a trick to figure out phonemes used by synthesis engine
            MyWord = MyWord.Trim();
            if (MyWord == null || MyWord == "") return "";
            if (MyWord.ToLower() == "a") return "ə";
            if (MyWord.ToLower() == "no") return "no";
            //txt to wav
            using (MemoryStream audioStream = new MemoryStream())
            {
                using (SpeechSynthesizer synth = new SpeechSynthesizer())
                {
                    if (synth == null)
                    {
                        MessageBox.Show("Could not open speech synthisizer.");
                        return "";
                    }
                    synth.SetOutputToWaveStream(audioStream);
                    PromptBuilder pb = new PromptBuilder();
                    if (Pron == null)
                    {
                        synth.Speak(MyWord);
                    }
                    else
                    {
                        pb.AppendTextWithPronunciation("Not Used", Pron);
                        synth.Speak(pb);
                    }
                    //synth.Speak(pb);
                    synth.SetOutputToNull();
                    audioStream.Position = 0;

                    //now wav to txt (for reco phonemes)
                    recoPhonemes = String.Empty;
                    GrammarBuilder gb = new GrammarBuilder(MyWord);
                    Grammar g = new Grammar(gb); //TODO the hard letters to recognize are 'g' and 'e'
                                                 //SrgsItem si = new SrgsItem();
                                                 //SrgsToken s = new SrgsToken("am");
                                                 //s.Pronunciation = "AE M";
                                                 //si.Add(s);
                                                 //s = new SrgsToken(MyWord);
                                                 //si.Add(s);
                                                 //SrgsRule sr = new SrgsRule("x", si);

                    //SrgsDocument sd = new SrgsDocument(sr);
                    //sd.PhoneticAlphabet = SrgsPhoneticAlphabet.Ups;
                    //Grammar g1 = new Grammar(sd);

                    //Grammar g = new DictationGrammar();
                    SpeechRecognitionEngine reco = new SpeechRecognitionEngine();
                    if (reco == null)
                    {
                        MessageBox.Show("Could not open speech recognition engine.");
                        return "";
                    }
                    reco.SpeechHypothesized += new EventHandler<SpeechHypothesizedEventArgs>(reco_SpeechHypothesized);
                    reco.SpeechRecognitionRejected += new EventHandler<SpeechRecognitionRejectedEventArgs>(reco_SpeechRecognitionRejected);
                    reco.UnloadAllGrammars(); //only use the one word grammar
                    reco.LoadGrammar(g);
                    reco.SetInputToWaveStream(audioStream);
                    RecognitionResult rr = reco.Recognize();
                    reco.SetInputToNull();
                    if (rr != null)
                    {
                        recoPhonemes = StringFromWordArray(rr.Words, WordType.Pronunciation);
                    }

                    //custom pronunciations don't seem to work so here are patches
                    recoPhonemes = recoPhonemes.Replace("e͡iɛm", "æm");

                    return recoPhonemes;
                }
            }
        }
        public static void reco_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            recoPhonemes = StringFromWordArray(e.Result.Words, WordType.Pronunciation);
        }

        public static void reco_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            recoPhonemes = StringFromWordArray(e.Result.Words, WordType.Pronunciation);
        }

        public static string StringFromWordArray(IReadOnlyCollection<RecognizedWordUnit> words, WordType type)
        {
            string text = "";
            foreach (RecognizedWordUnit word in words)
            {
                string wordText = "";
                if (type == WordType.Text || type == WordType.Normalized)
                {
                    wordText = word.Text;
                }
                else if (type == WordType.Lexical)
                {
                    wordText = word.LexicalForm;
                }
                else if (type == WordType.Pronunciation)
                {
                    wordText = word.Pronunciation;
                    //MessageBox.Show(word.LexicalForm);
                }
                else
                {
                    //throw new InvalidEnumArgumentException(String.Format("[0}: is not a valid input", type));
                }
                //Use display attribute

                if ((word.DisplayAttributes & DisplayAttributes.OneTrailingSpace) != 0)
                {
                    wordText += " ";
                }
                if ((word.DisplayAttributes & DisplayAttributes.TwoTrailingSpaces) != 0)
                {
                    wordText += "  ";
                }
                if ((word.DisplayAttributes & DisplayAttributes.ConsumeLeadingSpaces) != 0)
                {
                    wordText = wordText.TrimStart();
                }
                if ((word.DisplayAttributes & DisplayAttributes.ZeroTrailingSpaces) != 0)
                {
                    wordText = wordText.TrimEnd();
                }

                text += wordText;

            }
            return text;
        }
        public enum WordType
        {
            Text,
            Normalized = Text,
            Lexical,
            Pronunciation
        }
        public override void Initialize()
        {
            synth = new SpeechSynthesizer();
            if (synth == null)
            {
                MessageBox.Show("Speech Synthisizer could not be opened.");
                return;
            }

            // Configure the audio output.   
            synth.SetOutputToDefaultAudioDevice();
            synth.SpeakCompleted += Synth_SpeakCompleted;
            synth.SelectVoice("Microsoft Zira Desktop");
            minHeight = 4;
            minWidth = 6;
            ClearNeurons();
            na.GetNeuronAt(0).Label = "Enable";
            na.GetNeuronAt(0).AddSynapse(na.GetNeuronAt(0).Id, 1);
            AddLabel("Cancel");
            AddLabel("Validate");
            AddLabel("BabyTalk");

            if (FindModuleByName("AudibleUKS") is ModuleUKS2 nmkb)
            {
                nmkb.AddThing("Phoneme", "Audible");
            }
        }
    }
}
