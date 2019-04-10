using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Synthesis;

namespace BrainSimulator
{
    public class ModuleSpeechOut : ModuleBase
    {
        SpeechSynthesizer synth = null;

        string toSpeak = ""; //accumulates an array of words to speak
        string prePend = "";
        string postPend = "";
        string insertAnd = "";

        int anyNewWords = 0;

        public override void Fire()
        {
            Init();
            na.BeginEnum();
            for (Neuron n = na.GetNextNeuron(); n != null; n = na.GetNextNeuron())
            {
                if (n.LastCharge > .9)
                {
                    toSpeak += prePend + n.Label + " ";
                    prePend = "";
                    anyNewWords = 5;
                }
            }
            //builds up a phrase to be spoken, and passes it all at once to the synthisizer
            //this is much better than individual words
            anyNewWords--;
            if (anyNewWords == 0)
                if (toSpeak != "")
                {
                    toSpeak = toSpeak.Trim();
                    if (toSpeak.IndexOf(' ') != -1)
                        toSpeak = toSpeak.Insert(toSpeak.Trim().LastIndexOf(' '), " " + insertAnd);
                    toSpeak = toSpeak + " " + postPend;
                    //PauseRecognition();
                    synth.SpeakAsync(toSpeak + ".");
                    toSpeak = "";
                    prePend = "";
                    postPend = "";
                    insertAnd = "";

                }
        }


        protected override void Initialize()
        {
            synth = new SpeechSynthesizer();

            // Configure the audio output.   
            synth.SetOutputToDefaultAudioDevice();
            synth.SpeakCompleted += Synth_SpeakCompleted;
            synth.SelectVoice("Microsoft Zira Desktop");
            ModuleSpeechIn msi = (ModuleSpeechIn)FindModuleByType(typeof(ModuleSpeechIn));
            if (msi != null)
                msi.PauseRecognition(); //if there is a recognizer active
            synth.SpeakAsync("Speech synthisizer Initialized");

        }


        private void Synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            // Restart speech recognition.  
            ModuleSpeechIn msi = (ModuleSpeechIn)FindModuleByType(typeof(ModuleSpeechIn));
            if (msi != null)
                msi.ResumeRecognition();
        }

    }
}
