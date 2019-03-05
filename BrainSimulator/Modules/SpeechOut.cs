using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Synthesis;


namespace BrainSimulator
{

    public partial class NeuronArray
    {
        SpeechSynthesizer synth = null;

        string toSpeak = ""; //accumulates an array of words to speak

        public void SpeechOut(NeuronArea na)
        {
            // Initialize a new instance of the SpeechSynthesizer.  
            if (synth == null)
            {
                synth = new SpeechSynthesizer();

                // Configure the audio output.   
                synth.SetOutputToDefaultAudioDevice();
                synth.SelectVoice("Microsoft Zira Desktop");
                PauseRecognition();
                synth.SpeakAsync("Speech synthisizer Initialized");
                synth.SpeakCompleted += Synth_SpeakCompleted;
            }
            NeuronArea naIn = FindAreaByCommand("SpeechIn");
            NeuronArea naOut = FindAreaByCommand("SpeechOut");
            bool anyNewWords = false;
            naOut.BeginEnum();
            for (Neuron n = naOut.GetNextNeuron(); n != null; n = naOut.GetNextNeuron())
            {
                if (n.LastCharge > .9)
                {
                    //int diff = naOut.FirstNeuron - naIn.FirstNeuron;
                    //int found = dictionary.FindIndex(x => x.neuronIndex == n.Id - diff);
                    //if (found >= 0)
                    //{
                    //    toSpeak += " " + dictionary[found].theWord;
                    //    anyNewWords = true;
                    //}
                    toSpeak += n.Label + " ";
                    anyNewWords = true;
                }
            }
            //builds up a phrase to be spoken, and passes it all at once to the synthisizer
            //this is much better than individual words
            if (!anyNewWords)
                if (toSpeak != "")
                {
                    PauseRecognition();
                    synth.SpeakAsync(toSpeak + ".");
                    toSpeak = "";
                }
        }

        private void Synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            // Restart speech recognition.  
            ResumeRecognition();
        }

    }
}
