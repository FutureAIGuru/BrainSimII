//
// Copyright (c) Charles Simon. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Speech.Synthesis;
using System.Windows;

namespace BrainSimulator.Modules
{
    public class ModuleSpeakWords : ModuleBase
    {
        SpeechSynthesizer synth = null;
        string phraseToSpeak = "";

        public override void Fire()
        {
            Init();  //be sure to leave this here
            if (synth == null) return;

            if (!mv.GetNeuronAt(0).Fired()) return;

            ModuleUKSN nmKB = (ModuleUKSN)FindModleu(typeof(ModuleUKSN));
            if (nmKB == null) return;
            IList<Thing> words = nmKB.GetChildren(nmKB.Labeled("Word"));
            bool paused = true;
            //TODO: replace this direct access into the KB with synapses...then we can eliminate the storage of the words in the things values.
            foreach (Thing word in words)
            {
                if (nmKB.Fired(word, 2, false))
                {
                    paused = false;
                    phraseToSpeak += " " + word.V.ToString();
                }
            }
            if (paused && phraseToSpeak != "")
            {
                ModuleSpeechIn msi = (ModuleSpeechIn)FindModleu(typeof(ModuleSpeechIn));
                if (msi != null)
                    msi.PauseRecognition(); //if there is a recognizer active
                synth.SpeakAsync(phraseToSpeak + ".");
                phraseToSpeak = "";
            }
        }

        private void Synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            // Restart speech recognition.  
            ModuleSpeechIn msi = (ModuleSpeechIn)FindModleu(typeof(ModuleSpeechIn));
            if (msi != null)
                msi.ResumeRecognition();
        }

        public override void SetUpAfterLoad()
        {
            base.SetUpAfterLoad();
            Init();
            Initialize();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {

            mv.GetNeuronAt(0).Label = "Enable";
            mv.GetNeuronAt(0).AddSynapse(mv.GetNeuronAt(0).Id, 1);

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
        }
    }
}
