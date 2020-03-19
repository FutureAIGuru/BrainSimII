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
            if (nmKB == null) return;
            List<Thing> words = nmKB.GetChildren(nmKB.Labeled("Word"));
            bool paused = true;
            //TODO: replace this direct access into the KB with synapses...then we can eliminate the storage of the words in the things values.
            foreach(Thing word in words)
            {
                if (nmKB.Fired(word,2,false))
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



    }
}
