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

using System.Speech.Recognition; //needed to get pronunciation from text
using System.IO;

namespace BrainSimulator.Modules
{
    public class ModuleSpeakWords : ModuleBase
    {
        SpeechSynthesizer synth = null;

        //any public variable you create here will automatically be stored with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            if (synth == null) Initialize();
            if (!na.GetNeuronAt(0).Fired()) return;
            Module2DKBN nmKB = (Module2DKBN)FindModuleByType(typeof(Module2DKBN));
            List<Thing> words = nmKB.GetChildren(nmKB.Labeled("Word"));
            bool paused = true;
            //TODO: replace this direct access into the KB with synapses...then we can eliminate the storage of the words in the things values.
            foreach(Thing word in words)
            {
                if (nmKB.FiredOutput(word,2))
                {
                    paused = false;
                    phraseToSpeak += " " + word.V.ToString();
                }
            }
            if (paused && phraseToSpeak != "")
            {
                ModuleSpeechIn msi = (ModuleSpeechIn)FindModuleByType(typeof(ModuleSpeechIn));
                if (msi != null)
                    msi.PauseRecognition(); //if there is a recognizer active
                synth.SpeakAsync(phraseToSpeak + ".");
                phraseToSpeak = "";

                //use this when we want to work with phonemes instead of words
                //string pron = GetPronunciationFromText("beggar").Trim();
                //pron = pron.Replace(" ", "ˌ");//teeny pauses between words helps "hello world" to pronounce properly
                //PromptBuilder pb1 = new PromptBuilder();
                //pb1.AppendTextWithPronunciation("not used", pron);
                //synth.Rate = -4;
                //synth.Speak(pb1);
                //string[] words = toSpeak.Split(' ');
                //PromptBuilder pb = new PromptBuilder();
                ////foreach (string p in words)
                ////    if (p != "")
                //string p = @"əəə";

                
            }
        }

        string phraseToSpeak = "";


        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            synth = new SpeechSynthesizer();

            // Configure the audio output.   
            synth.SetOutputToDefaultAudioDevice();
            synth.SpeakCompleted += Synth_SpeakCompleted;
            synth.SelectVoice("Microsoft Zira Desktop");

            na.GetNeuronAt(0).Label = "Enable";
            na.GetNeuronAt(0).AddSynapse(na.GetNeuronAt(0).Id, 1);
        }

        private void Synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            // Restart speech recognition.  
            ModuleSpeechIn msi = (ModuleSpeechIn)FindModuleByType(typeof(ModuleSpeechIn));
            if (msi != null)
                msi.ResumeRecognition();
        }


        //BELOW CAN BE USED TO GET PHONEMES FROM A WORD so the test phrases can be input as text and fed to Sallie as phonemes
        public static string recoPhonemes;

        public static string GetPronunciationFromText(string MyWord)
        {
            //this is a trick to figure out phonemes used by synthesis engine

            //txt to wav
            using (MemoryStream audioStream = new MemoryStream())
            {
                using (SpeechSynthesizer synth = new SpeechSynthesizer())
                {
                    synth.SetOutputToWaveStream(audioStream);
                    PromptBuilder pb = new PromptBuilder();
                    pb.AppendBreak(PromptBreak.ExtraSmall); //'e' wont be recognized if this is large, or non-existent?
                    //synth.Speak(pb);
                    synth.Speak(MyWord);
                    //synth.Speak(pb);
                    synth.SetOutputToNull();
                    audioStream.Position = 0;

                    //now wav to txt (for reco phonemes)
                    recoPhonemes = String.Empty;
                    GrammarBuilder gb = new GrammarBuilder(MyWord);
                    Grammar g = new Grammar(gb); //TODO the hard letters to recognize are 'g' and 'e'
                    SpeechRecognitionEngine reco = new SpeechRecognitionEngine();
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

        /*
         * Alphabetic
(excluding the standard characters a-z)
Symbol	decimal	hex	value
ɑ	593	0251	open back unrounded
ɐ	592	0250	open-mid schwa
ɒ	594	0252	open back rounded
æ	230	00E6	raised open front unrounded
ɓ	595	0253	vd bilabial implosive
ʙ	665	0299	vd bilabial trill
β	946	03B2	vd bilabial fricative
ɔ	596	0254	open-mid back rounded
ɕ	597	0255	vl alveolopalatal fricative
ç	231	00E7	vl palatal fricative
ɗ	599	0257	vd alveolar implosive
ɖ	598	0256	vd retroflex plosive
ð	240	00F0	vd dental fricative
ʤ	676	02A4	vd postalveolar affricate
ə	601	0259	schwa
ɘ	600	0258	close-mid schwa
ɚ	602	025A	rhotacized schwa
ɛ	603	025B	open-mid front unrounded
ɜ	604	025C	open-mid central
ɝ	605	025D	rhotacized open-mid central
ɞ	606	025E	open-mid central rounded
ɟ	607	025F	vd palatal plosive
ʄ	644	0284	vd palatal implosive
ɡ	609	0261	vd velar plosive
(but the IPA has ruled that an ordinary g is also acceptable)
ɠ	608	0260	vd velar implosive
ɢ	610	0262	vd uvular plosive
ʛ	667	029B	vd uvular implosive
ɦ	614	0266	vd glottal fricative
ɧ	615	0267	vl multiple-place fricative
ħ	295	0127	vl pharyngeal fricative
ɥ	613	0265	labial-palatal approximant
ʜ	668	029C	vl epiglottal fricative
ɨ	616	0268	close central unrounded
ɪ	618	026A	lax close front unrounded
ʝ	669	029D	vd palatal fricative
ɭ	621	026D	vd retroflex lateral
ɬ	620	026C	vl alveolar lateral fricative
ɫ	619	026B	velarized vd alveolar lateral
ɮ	622	026E	vd alveolar lateral fricative
ʟ	671	029F	vd velar lateral
ɱ	625	0271	vd labiodental nasal
ɯ	623	026F	close back unrounded
ɰ	624	0270	velar approximant
ŋ	331	014B	vd velar nasal
ɳ	627	0273	vd retroflex nasal
ɲ	626	0272	vd palatal nasal
ɴ	628	0274	vd uvular nasal
ø	248	00F8	front close-mid rounded
ɵ	629	0275	rounded schwa
ɸ	632	0278	vl bilabial fricative
θ	952	03B8	vl dental fricative
œ	339	0153	front open-mid rounded
ɶ	630	0276	front open rounded
ʘ	664	0298	bilabial click
ɹ	633	0279	vd (post)alveolar approximant
ɺ	634	027A	vd alveolar lateral flap
ɾ	638	027E	vd alveolar tap
ɻ	635	027B	vd retroflex approximant
ʀ	640	0280	vd uvular trill
ʁ	641	0281	vd uvular fricative
ɽ	637	027D	vd retroflex flap
ʂ	642	0282	vl retroflex fricative
ʃ	643	0283	vl postalveolar fricative
ʈ	648	0288	vl retroflex plosive
ʧ	679	02A7	vl postalveolar affricate
ʉ	649	0289	close central rounded
ʊ	650	028A	lax close back rounded
ʋ	651	028B	vd labiodental approximant
ⱱ	11377	2C71	voiced labiodental flap
ʌ	652	028C	open-mid back unrounded
ɣ	611	0263	vd velar fricative
ɤ	612	0264	close-mid back unrounded
ʍ	653	028D	vl labial-velar fricative
χ	967	03C7	vl uvular fricative
ʎ	654	028E	vd palatal lateral
ʏ	655	028F	lax close front rounded
ʑ	657	0291	vd alveolopalatal fricative
ʐ	656	0290	vd retroflex fricative
ʒ	658	0292	vd postalveolar fricative
ʔ	660	0294	glottal plosive
ʡ	673	02A1	vd epiglottal plosive
ʕ	661	0295	vd pharyngeal fricative
ʢ	674	02A2	vd epiglottal fricative
ǀ	448	01C0	dental click
ǁ	449	01C1	alveolar lateral click
ǂ	450	01C2	alveolar click
ǃ	451	01C3	retroflex click
Top of lists

Spacing diacritics and suprasegmentals
To study these, you may find it helpful to set your browser text size to Largest.
Symbol	decimal	hex	value
ˈ	712	02C8	(primary) stress mark
ˌ	716	02CC	secondary stress
ː	720	02D0	length mark NB: there is a bug in some versions of MS IExplorer that causes this character not to display. It is probably best to use a simple colon instead.
ˑ	721	02D1	half-length
ʼ	700	02BC	ejective
ʴ	692	02B4	rhotacized
ʰ	688	02B0	aspirated
ʱ	689	02B1	breathy-voice-aspirated
ʲ	690	02B2	palatalized
ʷ	695	02B7	labialized
ˠ	736	02E0	velarized
ˤ	740	02E4	pharyngealized
˞	734	02DE	rhotacized
Note the ready-made characters ɚ 602 025A (combining ə 601 0259 and ˞ 734 02DE) and ɝ 605 025D (combining ɜ 604 025C and ˞ 734 02DE).
Top of lists

Non-spacing diacritics and suprasegmentals
As you can see, several of these are unsatisfactory, particularly in smaller sizes. They are shown here with an appropriate supporting base character. When composing a text in HTML, enter the diacritic after the base character, thus (voiceless n, n̥) n&#805;. The browser automatically backspaces the diacritic, but by a constant amount, which may or may not produce a satisfactory result.
Symbol	decimal	hex	value
n̥ d̥	805	0325	voiceless
ŋ̊	778	030A	voiceless (use if character has descender)
b̤ a̤	804	0324	breathy voiced
t̪ d̪	810	032A	dental
s̬ t̬	812	032C	voiced
b̰ a̰	816	0330	creaky voiced
t̺ d̺	826	033A	apical
t̼ d̼	828	033C	linguolabial
t̻ d̻	827	033B	laminal
t̚	794	031A	not audibly released
ɔ̹	825	0339	more rounded
ẽ	771	0303	nasalized
ɔ̜	796	031C	less rounded
u̟	799	031F	advanced
e̠	800	0320	retracted
ë	776	0308	centralized
l̴ n̴	820	0334	velarized or pharyngealized
ɫ	619	026B	(ready-made combination, dark l)
e̽	829	033D	mid-centralized
e̝ ɹ̝	797	031D	raised
m̩ n̩ l̩	809	0329	syllabic
e̞ β̞	798	031E	lowered
e̯	815	032F	non-syllabic
e̘	792	0318	advanced tongue root
e̙	793	0319	retracted tongue root
ĕ	774	0306	extra-short
e̋	779	030B	extra high tone
é	769	0301	high tone
ē	772	0304	mid tone
è	768	0300	low tone
ȅ	783	030F	extra low tone
x͜x	860	035C	tie bar below
x͡x	865	0361	tie bar above
Arrows
Symbol	decimal	hex	value
↓	8595	2193	downstep
↑	8593	2191	upstep
→	8594	2192	(becomes, is realized as — not recognized by the IPA)
↗	8599	2197	global rise
↘	8600	2198	global fall
         * */

    }
}
