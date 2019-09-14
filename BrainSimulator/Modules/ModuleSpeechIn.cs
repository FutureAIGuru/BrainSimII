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
    public class ModuleSpeechIn : ModuleBase
    {
        SpeechRecognitionEngine recognizer = null;

        //keeps the temporary phrase so it can be recognized across multiple engine cycles
        private List<string> words = new List<string>();

        public override void Fire()
        {
            Init();

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
                    n.CurrentCharge = 1;
                    Debug.WriteLine("Fired Neuron for word: " + word);
                }

                words.RemoveAt(0);
            }

        }
        public override void Initialize()
        {
            na.BeginEnum();
            for (Neuron n = na.GetNextNeuron(); n != null; n = na.GetNextNeuron())
                n.Label = "";

            if (recognizer != null)
            {
                recognizer.RecognizeAsyncStop();
                recognizer.Dispose();
            }
            // Create an in-process speech recognizer for the en-US locale.  
            recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));

            Grammar gr = CreateGrammar();
            recognizer.LoadGrammar(gr);

            // Add a handler for the speech recognized event.  
            recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recognizer_SpeechRecognized);

            // Configure input to the speech recognizer.  
            recognizer.SetInputToDefaultAudioDevice();

            //// Start asynchronous, continuous speech recognition.  
            recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        private Grammar CreateGrammar()
        {

            //// Create and load a dictation grammar.  This is for many words and doesn't work very well
            //recognizer.LoadGrammar(new DictationGrammar());

            // create a small custom grammar for testing
            Choices digit = new Choices("1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "point");
            //Choices number = new Choices();
            //for (int i = 1; i < 200; i++)
            //    number.Add(i.ToString());
            Choices emotion = new Choices("ecstatic", "happy", "so-so", "OK", "sad", "unhappy");
            Choices timeOfDay = new Choices("morning", "afternoon", "evening", "night");
            Choices color = new Choices("black", "gray", "pink", "red", "blue", "green", "orange", "white");
            Choices shape = new Choices("square", "rectangle", "circle", "line");
            Choices direction = new Choices("right", "left", "forward", "backwards");
            Choices size = new Choices("big", "medium", "little");
            Choices action = new Choices("move", "turn");

            Choices command = new Choices("Computer", "Computer Say", "Computer what can you see?", "Computer What is behind you?", "Computer turn around", "Computer what attributes do you know", "Computer what is");
            Choices query = new Choices("what is", "add", "name", "this is");
            Choices article = new Choices("a", "an", "the", "some", "containing", "with", "which are");
            Choices words = new Choices("mary", "had", "a", "little", "lamb");

            PluralizationService ps = PluralizationService.CreateService(new CultureInfo("en-us"));

            string[] attribList = new string[] { "attributes", "sequences", "colors", "sizes", "shapes", "digits", "things" };
            string[] attribList1 = new string[attribList.Length];
            for (int i = 0; i < attribList.Length; i++)
                attribList1[i] = ps.Singularize(attribList[i]);

            List<GrammarBuilder> gb = new List<GrammarBuilder>();
            GrammarBuilder a = new GrammarBuilder("Computer");
            //a.Append(query, 0, 1);
            //a.Append(action, 0, 1);
            //a.Append(direction, 0, 1);
            //a.Append(article, 0, 1);
            //a.Append(sequence, 0, 1);
            //a.Append(new Choices(attribList), 0, 1);
            //a.Append(new Choices(attribList1), 0, 1);
            //a.Append(article, 0, 1);
            //a.Append(color, 0, 1);
            //a.Append(shape, 0, 1);
            //a.Append(size, 0, 1);
            a.Append(digit, 0, 4);
            //a.Append(words, 0, 5);
            gb.Add(a);

            //get the words from the grammar and label neurons
            string c = a.DebugShowPhrases;
            c = c.Replace((char)0x2018, ' ');
            c = c.Replace((char)0x2019, ' ');
            string[] c1 = c.Split(new string[] { "[", ",", "]", " " }, StringSplitOptions.RemoveEmptyEntries);
            c1 = c1.Distinct().ToArray();

            int i1 = 1;
            na.BeginEnum();
            for (Neuron n = na.GetNextNeuron(); n != null && i1 < c1.Length; i1++, n = na.GetNextNeuron())
                n.Label = c1[i1].ToLower();

            Choices choices = new Choices(gb.ToArray());
            Grammar gr = new Grammar((GrammarBuilder)choices);

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

    }
}
