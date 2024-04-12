//
// Copyright (c) Charles Simon. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Speech.Recognition; //needed to get pronunciation from text
using System.Speech.Synthesis;
using System.Windows;

namespace BrainSimulator.Modules
{
    public class ModuleSpeakPhonemes : ModuleBase
    {
        public ModuleSpeakPhonemes()
        {
            minHeight = 8;
            minWidth = 8;
        }

        SpeechSynthesizer synth = null;
        string phraseToSpeak = "";

        public override void Fire()
        {
            Init();  //be sure to leave this here
            if (synth == null) return;

            bool paused = true;
            for (int i = 1; i < mv.NeuronCount; i++)
            {
                Neuron n = mv.GetNeuronAt(i);
                if (n.Fired())
                {
                    if (n.Label.Length == 1)
                    {
                        phraseToSpeak += n.Label;
                        paused = false;
                    }
                    if (n.Synapses.Count == 0)
                    {
                        //if a neuron fired and it has no connection, connect it to the knowledge store
                        //connection to KB 
                        ModuleUKSN nmKB = (ModuleUKSN)FindModleu(typeof(ModuleUKSN));
                        if (nmKB != null)
                        {
                            string label = "pn" + n.Label;
                            IList<Thing> phonemes = nmKB.Labeled("Phoneme").Children;
                            Thing pn = nmKB.Labeled(label, phonemes);
                            if (pn == null) //this should always be null
                            {
                                pn = nmKB.AddThing(label, nmKB.Labeled("Phoneme"), pn);
                            }
                            Neuron n1 = nmKB.GetNeuron(pn);
                            if (n1 != null)
                            {
                                n.AddSynapse(n1.Id, 1);
                                n1.SetValue(1);
                            }
                        }
                    }
                }
            }
            if (phonemesToFire != "")
            {
                char c = phonemesToFire[0];
                bool fired = false;
                for (int i = 0; i < mv.NeuronCount; i++)
                {
                    Neuron n = mv.GetNeuronAt(i);
                    if (n.Label == c.ToString())
                    {
                        n.SetValue(1);
                        fired = true;
                        break;
                    }
                }
                if (!fired)
                    Utils.Noop();
                phonemesToFire = phonemesToFire.Substring(1);
            }

            if (paused && phraseToSpeak != "")
            {
                if (dlg != null)
                    ((ModuleSpeakPhonemesDlg)dlg).SetLabel(phraseToSpeak);

                if (mv.GetNeuronAt("Enable").Fired())
                {
                    ModuleSpeechIn msi = (ModuleSpeechIn)FindModleu(typeof(ModuleSpeechIn));
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
                        pb1.StartStyle(new PromptStyle(PromptRate.ExtraSlow));
                    }

                    pb1.AppendTextWithPronunciation("not used", phraseToSpeak);
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
            InitVoice();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        static public List<string> combiners = new List<string>();
        public override void Initialize()
        {
            foreach (Neuron n in mv.Neurons)
                n.Clear();
            InitVoice();

            mv.GetNeuronAt(0).Label = "Enable";
            mv.GetNeuronAt(0).AddSynapse(mv.GetNeuronAt(0).Id, 1);
            AddLabel("BabyTalk");
            AddLabel("Vowels");

            //this is used to eliminate unused phonemes (many not used by english SR) and to find phonemes which can be combined
            string wordList = "red blue green orange A B C D 1 2 3 4 a an the say see big little near far this is line dot I you Sallie byebye mama dada cat dog toy boy girl go to stop no good turn right left all gone ";
            string phonemesInUse = GetPronunciationFromText(wordList);
            int x = (int)phonemesInUse[0];
            phonemesInUse = phonemesInUse.Replace(" ", ".");
            for (int index = phonemesInUse.IndexOf("\u0361"); index > 0; index = phonemesInUse.IndexOf("\u0361", index + 1))
            {
                string s = phonemesInUse.Substring(index - 1, 3);
                if (!combiners.Contains(s)) combiners.Add(s);
            }

            foreach (string phonemes in vowels1)
            {
                if (phonemes != "")
                {
                    if (phonemesInUse.Contains(phonemes[0]))
                        AddLabel(phonemes[0].ToString());
                    if (phonemes.Length > 1 && phonemesInUse.Contains(phonemes[1])) AddLabel(phonemes[1].ToString());
                }
            }
            AddLabel("Conson.");
            foreach (string phonemes in consonants1)
            {
                if (phonemes != "")
                {
                    if (phonemesInUse.Contains(phonemes[0]))
                        AddLabel(phonemes[0].ToString());
                    if (phonemes.Length > 1 && phonemesInUse.Contains(phonemes[1])) AddLabel(phonemes[1].ToString());
                }
            }

        }

        private void InitVoice()
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
            minHeight = 10;
            minWidth = 5;
        }

        private void Synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            // Restart speech recognition.  
            ModuleSpeechIn msi = (ModuleSpeechIn)FindModleu(typeof(ModuleSpeechIn));
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
        public void FirePhonemes(string phonemes)
        {
            typedIn = true;
            phonemesToFire = phonemes.Replace(" ", "");
        }

        //BELOW CAN BE USED TO GET PHONEMES FROM A WORD so the test phrases can be input as text and fed to Sallie as phonemes
        //It was pulled in from the web but seems to work for most cases.   https://stackoverflow.com/questions/49519428/how-to-get-pronunciation-phonemes-corresponding-to-a-word-using-c
        public static string recoPhonemes;

        public static string GetPronunciationFromText(string MyWord, string Pron = null)
        {
            //this is a trick to figure out phonemes used by synthesis engine
            if (MyWord == null | MyWord == "") return "";
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
                    //txtRecoPho.Text = recoPhonemes;
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

    }
}