﻿//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;

namespace BrainSimulator.Modules
{
    public class ModuleGraph : ModuleBase
    {
        string[] cols = { "in", "thing", "this", "parent", "child", "attrib", "allAttr", "anyAttr", "match", "next", "nMtch", "head", "alt", "recur", "out", "say", "0" };
        string theInPhrase = "";

        public override void Fire()
        {
            HandleSequenceSearch();
            HandleSpeechIn1();
            HandleVoiceRequest();
        }

        void AddSynapse(string source, int sourceRow, string dest, int destRow, float weight)
        {
            Neuron n1 = na.GetNeuronAt(Array.IndexOf(cols, source), sourceRow);
            Neuron n2 = na.GetNeuronAt(Array.IndexOf(cols, dest), destRow);
            if (n1 != null && n2 != null)
            {
                n1.AddSynapse(n2.Id, weight, theNeuronArray, false);
            }
        }
        //probably a bad thing to use both attribs and sequenceItems
        public void AddThing(string parent, string name, string[] attribs = null, string[] sequenceItems = null)
        {
            int parentRow = -1;
            int newRow = -1;
            for (int j = 0; j < na.Height; j++)
            {
                if (parent != "" && na.GetNeuronAt(0, j).Label.ToLower() == parent.ToLower()) parentRow = j;
                if (na.GetNeuronAt(0, j).Label == "")
                {
                    newRow = j;
                    break;
                }
            }
            if (newRow == -1) return;  //the module is full
            if (name == "") name = ".";
            na.GetNeuronAt(0, newRow).Label = name;
            if (name != "." && naOut != null)
                na.GetNeuronAt(Array.IndexOf(cols, "say"), newRow).AddSynapse(GetSpokenWord(name).Id, 1, theNeuronArray, false);

            if (parentRow != -1)
            {
                AddSynapse("child", parentRow, "alt", newRow, 1);
                AddSynapse("parent", newRow, "alt", parentRow, 1);
                //na.GetNeuronAt(Array.IndexOf(cols, "child"), parentRow).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "alt"), newRow).Id, 1, theNeuronArray, false);
                //na.GetNeuronAt(Array.IndexOf(cols, "parent"), newRow).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "alt"), parentRow).Id, 1, theNeuronArray, false);
            }

            //add any attributes
            for (int i = 0; attribs != null && i < attribs.Length; i++)
            {
                na.GetNeuronAt("'1'").AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "match"), newRow).Id, -(attribs.Length - 1), theNeuronArray, false);
                for (int j = 0; j < na.Height; j++)
                {
                    if (na.GetNeuronAt(0, j).Label == attribs[i])
                    {
                        na.GetNeuronAt(Array.IndexOf(cols, "attrib"), newRow).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "alt"), j).Id, 1, theNeuronArray, false);
                        na.GetNeuronAt(Array.IndexOf(cols, "allAttr"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "match"), newRow).Id, 1, theNeuronArray, false);
                        na.GetNeuronAt(Array.IndexOf(cols, "anyAttr"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "thing"), newRow).Id, 1, theNeuronArray, false);
                        na.GetNeuronAt(Array.IndexOf(cols, "attrib"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "alt"), newRow).Id, 1, theNeuronArray, false);

                        break;
                    }
                }
            }

            //add any sequence
            int curRow = newRow;
            int prevRow = newRow - 1;
            int nextRow = curRow + 1;
            Neuron nBeginning = na.GetNeuronAt(Array.IndexOf(cols, "in"), curRow);
            nBeginning.Label = name;
            for (int i = 0; sequenceItems != null && i < sequenceItems.Length && nextRow < na.Height; i++)
            {
                if (i != 0)
                    na.GetNeuronAt(Array.IndexOf(cols, "in"), curRow).Label = ".";


                for (int j = 0; j < na.Height; j++)
                {
                    if (na.GetNeuronAt(0, j).Label == sequenceItems[i])
                    {
                        //handle searching for a sequence
                        //the initial entry
                        na.GetNeuronAt(Array.IndexOf(cols, "in"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "match"), curRow).Id, 1, theNeuronArray, false);
                        na.GetNeuronAt(Array.IndexOf(cols, "in"), curRow).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "match"), curRow).Id, -1, theNeuronArray, false);
                        na.GetNeuronAt(Array.IndexOf(cols, "match"), curRow).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "in"), curRow).Id, 1, theNeuronArray, false);

                        //subsequent entries
                        if (prevRow >= 0)
                            na.GetNeuronAt(Array.IndexOf(cols, "thing"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "nMtch"), prevRow).Id, 1, theNeuronArray, false);
                        na.GetNeuronAt("nMtch").AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "in"), curRow).Id, -1, theNeuronArray, false);
                        na.GetNeuronAt(Array.IndexOf(cols, "thing"), curRow).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "nMtch"), curRow).Id, 1, theNeuronArray, false);
                        na.GetNeuronAt("'1'").AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "nMtch"), curRow).Id, -1, theNeuronArray, false);

                        na.GetNeuronAt(Array.IndexOf(cols, "nMtch"), curRow).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "in"), curRow).Id, -1, theNeuronArray, false);
                        na.GetNeuronAt(Array.IndexOf(cols, "thing"), curRow).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "head"), curRow).Id, 1, theNeuronArray, false);
                        na.GetNeuronAt(Array.IndexOf(cols, "head"), curRow).AddSynapse(nBeginning.Id, 1, theNeuronArray, false);
                        na.GetNeuronAt(Array.IndexOf(cols, "attrib"), curRow).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "alt"), j).Id, 1, theNeuronArray, false);

                        if (i != 0)
                            na.GetNeuronAt(Array.IndexOf(cols, "head"), curRow).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "in"), curRow).Id, -1, theNeuronArray, false);

                        if (i < sequenceItems.Length - 1)
                        {
                            //handle playing a sequence
                            na.GetNeuronAt(Array.IndexOf(cols, "next"), curRow).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "thing"), nextRow).Id, 1, theNeuronArray, false);
                            na.GetNeuronAt(Array.IndexOf(cols, "next"), curRow).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "in"), nextRow).Id, 1, theNeuronArray, false);

                            //handle searching
                            na.GetNeuronAt(Array.IndexOf(cols, "nMtch"), curRow).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "in"), nextRow).Id, 1, theNeuronArray, false);
                        }
                        else
                        {
                            //stop the playback
                            na.GetNeuronAt(Array.IndexOf(cols, "next"), curRow).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "next"), 0).Id, -1, theNeuronArray, false);

                            //searching...last element needs to match for "head" to work
                        }
                        break;
                    }
                }
                prevRow = curRow;
                curRow = nextRow;
                nextRow = curRow + 1;
            }
        }
        public void Test()
        {
            //AddThing("Attribute", "Size");
            //AddThing("Size", "Big");
            //AddThing("Size", "Little");
            AddThing("Attribute", "Color");
            AddThing("Color", "Red");
            AddThing("Color", "Blue");
            //AddThing("Attribute", "Shape");
            //AddThing("Shape", "Square");
            //AddThing("Shape", "Circle");
            //AddThing("Thing", "R-S", new string[] { "Red", "Square" });
            //AddThing("Thing", "R-C", new string[] { "Red", "Circle" });
            //AddThing("Thing", "B-B-C", new string[] { "Big", "Blue", "Circle" });
            //AddThing("Thing", "L-B-C", new string[] { "Little", "Blue", "Circle" });

            //AddThing("Attribute", "Digit");
            //AddThing("Digit", "1");
            //AddThing("Digit", "2");
            //AddThing("Digit", "3");
            //AddThing("Digit", "4");
            //AddThing("Digit", "5");
            //AddThing("Digit", "9");
            //AddThing("Digit", "point");
            //AddThing("Sequence", "Count", null, new string[] { "1", "2", "3" });
            //AddThing("Sequence", "Down", null, new string[] { "5", "4", "3", "2", "1" });
            //AddThing("Sequence", "pi", null, new string[] { "3", "point", "1", "4", "1", "5", "9" });



            AddThing("Attribute", "word");
            AddThing("word", "Mary");
            AddThing("word", "had");
            AddThing("word", "a");
            AddThing("word", "little");
            AddThing("word", "lamb");
            AddThing("Sequence", "M1", null, new string[] { "Mary", "had", "a", "little", "lamb" });

        }



        int FindRowByLabel(string label)
        {
            int retVal = -1;
            for (int i = 0; i < na.Height; i++)
            {
                if (na.GetNeuronAt(0, i).Label.ToLower() == label) return i;
            }
            return retVal;
        }

        bool phraseIsComplete = false;
        List<string> searchSequence = new List<string>();

        void HandleSequenceSearch()
        {
            if (searchSequence.Count == 0) return;
            if (searchSequence[0] == "nop")
            {
            }
            else if (searchSequence[0] == "clr")
            {
                na.GetNeuronAt("clr").SetValue(1);
            }
            else if (searchSequence[0] == "attrib")
            {
                na.GetNeuronAt("attrib").SetValue(1);
            }
            else if (searchSequence[0] == "attrib0")
            {
                na.GetNeuronAt("attrib").SetValue(0);
            }
            else if (searchSequence[0] == "match")
            {
                na.GetNeuronAt("match").SetValue(1);
            }
            else if (searchSequence[0] == "match0")
            {
                na.GetNeuronAt("match").SetValue(0);
            }
            else if (searchSequence[0] == "nMtch")
            {
                na.GetNeuronAt("nMtch").SetValue(1);
            }
            else if (searchSequence[0] == "next")
            {
                na.GetNeuronAt("next").SetValue(1);
            }
            else if (searchSequence[0] == "next0")
            {
                na.GetNeuronAt("next").SetValue(0);
            }
            else if (searchSequence[0] == "head")
            {
                na.GetNeuronAt("head").SetValue(1);
            }
            else
            {
                Neuron n = na.GetNeuronAt(searchSequence[0]);
                if (n != null && n.LastCharge == 0)
                    n.SetValue(1);
                else if (n != null)
                    n.SetValue(0);
            }
            searchSequence.RemoveAt(0);
        }


        void HandleVoiceRequest()
        {
            if (!phraseIsComplete) return;
            for (int i = 2; i < na.Height; i++)
                na.GetNeuronAt(0, i).SetValue(0);
            theInPhrase = theInPhrase.ToLower();
            theInPhrase = theInPhrase.Replace("a ", "").
                Replace("an ", "").
                Replace("the ", "").
                Replace("some ", "").
                Replace("is ", "").
                Replace("with ", "").
                Replace("which ", "").
                Replace("are ", "").
                Replace("containing ", "").
                Replace("  ", " ");
            string[] words = theInPhrase.Split(' ');


            //if (words[0] == "what")
            //{
            //    int row = FindRowByLabel(words[1]);
            //    if (words[1] == "pi" || words[1] == "mary")
            //    {
            //        if (words[1] == "mary" ) row = FindRowByLabel("M1");
            //        na.GetNeuronAt(0, row).SetValue(1);
            //        na.GetNeuronAt(Array.IndexOf(cols, "next"), 0).SetValue(1);
            //        prePend = words[1] + " is ";
            //    }
            //    else if (row != -1)
            //    {
            //        na.GetNeuronAt(0, row).SetValue(1);
            //        na.GetNeuronAt(Array.IndexOf(cols, "parent"), 0).SetValue(1);
            //        prePend = words[1] + " is a ";
            //    }
            //    else
            //    {
            //        toSpeak = "I don't know";
            //    }
            //}
            //if (words[0] == "name")
            //{
            //    PluralizationService ps = PluralizationService.CreateService(new CultureInfo("en-us"));
            //    if (words[1] == "things")
            //    {
            //        int row = FindRowByLabel(words[2]);
            //        if (row != -1)
            //        {
            //            na.GetNeuronAt(0, row).SetValue(1);
            //            na.GetNeuronAt(Array.IndexOf(cols, "attrib"), 0).SetValue(1);
            //            insertAnd = "and ";
            //            postPend = " are " + words[2];
            //        }

            //    }
            //    else
            //    {
            //        string sing = ps.Singularize(words[1]);
            //        int row = FindRowByLabel(sing);
            //        if (row != -1)
            //        {
            //            na.GetNeuronAt(0, row).SetValue(1);
            //            na.GetNeuronAt(Array.IndexOf(cols, "child"), 0).SetValue(1);
            //            insertAnd = "and ";
            //            postPend = " are " + words[1];
            //        }
            //        else
            //        {
            //            toSpeak = "I don't know";
            //        }
            //    }
            //}
            if (words[0] == "sequence")
            {
                if (words.Length == 2)
                { //say the sequence by name

                }
                else //earch for the sequence
                {
                    searchSequence.Add("match");
                    for (int i = 1; i < words.Length; i++)
                    {
                        searchSequence.Add(words[i]);
                        searchSequence.Add("nMtch");
                        if (i == 1)
                            searchSequence.Add("match0");
                        searchSequence.Add(words[i]);
                    }
                    searchSequence.Add("head");
                    searchSequence.Add("nop");
                    searchSequence.Add("nop");
                    searchSequence.Add("nop");
                    searchSequence.Add("nop");
                    searchSequence.Add("attrib");
                    searchSequence.Add("nop");
                    searchSequence.Add("next");
                    searchSequence.Add("nop");
                    searchSequence.Add("nop");
                    searchSequence.Add("nop");
                    searchSequence.Add("nop");
                    searchSequence.Add("nop");
                    searchSequence.Add("nop");
                    searchSequence.Add("nop");
                    searchSequence.Add("nop");
                    searchSequence.Add("nop");
                    searchSequence.Add("nop");
                    searchSequence.Add("nop");
                    searchSequence.Add("nop");
                    searchSequence.Add("nop");
                    searchSequence.Add("nop");
                    searchSequence.Add("nop");
                    searchSequence.Add("next0");
                    searchSequence.Add("clr");
                    searchSequence.Add("attrib0");
                }
            }
            if (words[0] == "add")
            {
                if (words.Length < 3) return;
                int rowParent = FindRowByLabel(words[1]);
                if (rowParent != -1)
                {
                    AddThing(words[1], words[2]);
                    //      toSpeak = words[1] + " " + words[2] + " added";
                }
                else
                {
                    //      toSpeak = "Category not found";
                }
            }
            theInPhrase = "";
            phraseIsComplete = false;
        }

        private void HandleSpeechIn1()
        {
            ModuleView naIn = MainWindow.theNeuronArray.FindAreaByLabel("ModuleSpeechIn");
            if (naIn == null) return;

            naIn.BeginEnum();
            for (Neuron n = naIn.GetNextNeuron(); n != null; n = naIn.GetNextNeuron())
            {
                if (!n.InUse()) break;
                if (n.LastCharge == 1)
                {
                    if (theInPhrase != "") theInPhrase += " ";
                    theInPhrase += n.Label;
                    return;
                }
            }
            if (theInPhrase != "")
                phraseIsComplete = true;
        }
        public Neuron GetSpokenWord(string word)
        {
            ModuleView naOut = MainWindow.theNeuronArray.FindAreaByLabel("ModuleSPeechOut");
            Neuron n = null;
            if (naOut != null)
            {
                for (int i = 0; i < naOut.NeuronCount; i++)
                {
                    Neuron n1 = naOut.GetNeuronAt(i);
                    if (n1.Label == word) return n1;
                    if (n1.Label == "")
                    {
                        n1.Label = word;
                        return n1;
                    }
                }
            }
            return n;
        }
        public override void Initialize()
        {
            ClearNeurons();
            Neuron n0 = na.GetNeuronAt(1, 1);
            n0.Label = "'1'";
            n0.AddSynapse(n0.Id, 1, theNeuronArray, false);
            n0.SetValue(1);
            na.GetNeuronAt(0, 1).Label = "clr";
            for (int i = 0; i < cols.Length; i++)
            {
                Neuron n = na.GetNeuronAt(i, 0);
                n.Label = cols[i];
            }
            //put in the vertical synapses for the columns which need them
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] == "this" || cols[i] == "this" ||
                    cols[i] == "attrib" || cols[i] == "parent" ||
                    cols[i] == "child" || cols[i] == "next" || cols[i] == "nMtch" || cols[i] == "head" ||
                    cols[i] == "anyAttr" || cols[i] == "allAttr" ||
                    cols[i] == "recur" || cols[i] == "head" ||
                    cols[i] == "match" ||
                    cols[i] == "say")
                {
                    Neuron n = na.GetNeuronAt(i, 0);
                    Neuron n1 = na.GetNeuronAt(i, 1);
                    n.AddSynapse(n1.Id, -1, theNeuronArray, false);
                    n0.AddSynapse(n1.Id, 1, theNeuronArray, false);
                    for (int j = 2; j < na.Height; j++)
                    {
                        n1.AddSynapse(na.GetNeuronAt(i, j).Id, -1, theNeuronArray, false);
                    }
                }
            }
            //make the clr neuron clear all the input neurons
            Neuron nClr = na.GetNeuronAt("clr");
            for (int j = 2; j < na.Height; j++)
            {
                nClr.AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "in"), j).Id, -1, theNeuronArray, false);
            }
            na.GetNeuronAt("recur").AddSynapse(nClr.Id, 1, theNeuronArray, false);

            //put in all the horizontal synapses
            for (int j = 2; j < na.Height; j++)
            {
                na.GetNeuronAt(Array.IndexOf(cols, "in"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "in"), j).Id, 1, theNeuronArray, false);
                na.GetNeuronAt(Array.IndexOf(cols, "in"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "thing"), j).Id, 10, theNeuronArray, false);
                na.GetNeuronAt(Array.IndexOf(cols, "thing"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "theNeuronArray"), j).Id, 1, theNeuronArray, false);
                na.GetNeuronAt(Array.IndexOf(cols, "thing"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "attrib"), j).Id, 1, theNeuronArray, false);
                na.GetNeuronAt(Array.IndexOf(cols, "thing"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "anyAttr"), j).Id, 1, theNeuronArray, false);
                na.GetNeuronAt(Array.IndexOf(cols, "thing"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "allAttr"), j).Id, 1, theNeuronArray, false);
                na.GetNeuronAt(Array.IndexOf(cols, "thing"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "parent"), j).Id, 1, theNeuronArray, false);
                na.GetNeuronAt(Array.IndexOf(cols, "thing"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "child"), j).Id, 1, theNeuronArray, false);
                na.GetNeuronAt(Array.IndexOf(cols, "thing"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "next"), j).Id, 1, theNeuronArray, false);


                na.GetNeuronAt(Array.IndexOf(cols, "theNeuronArray"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "out"), j).Id, 1, theNeuronArray, false);
                na.GetNeuronAt(Array.IndexOf(cols, "alt"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "out"), j).Id, 1, theNeuronArray, false);
                //na.GetNeuronAt(Array.IndexOf(cols, "out"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "in"), j).Id, -1, theNeuronArray, false);
                na.GetNeuronAt(Array.IndexOf(cols, "out"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "recur"), j).Id, 1, theNeuronArray, false);
                na.GetNeuronAt(Array.IndexOf(cols, "recur"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "in"), j).Id, 2, theNeuronArray, false); //2 because the out is suppressing
                na.GetNeuronAt(Array.IndexOf(cols, "match"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "thing"), j).Id, 1, theNeuronArray, false);



                na.GetNeuronAt(Array.IndexOf(cols, "out"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "say"), j).Id, 1, theNeuronArray, false);
                na.GetNeuronAt(Array.IndexOf(cols, "out"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "0"), j).Id, 1, theNeuronArray, false);
                na.GetNeuronAt(Array.IndexOf(cols, "0"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "say"), j).Id, -1, theNeuronArray, false);

                na.GetNeuronAt(Array.IndexOf(cols, "next"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "in"), j).Id, -1, theNeuronArray, false);
                na.GetNeuronAt(Array.IndexOf(cols, "next"), j).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "thing"), j).Id, -30, theNeuronArray, false);
            }
            //make som coluimns into an always-fire
            na.GetNeuronAt(Array.IndexOf(cols, "next"), 0).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "next"), 0).Id, 1, theNeuronArray, false);
            na.GetNeuronAt(Array.IndexOf(cols, "say"), 0).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "say"), 0).Id, 1, theNeuronArray, false);
            na.GetNeuronAt(Array.IndexOf(cols, "attrib"), 0).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "attrib"), 0).Id, 1, theNeuronArray, false);
            na.GetNeuronAt(Array.IndexOf(cols, "match"), 0).AddSynapse(na.GetNeuronAt(Array.IndexOf(cols, "match"), 0).Id, 1, theNeuronArray, false);

            //na.GetNeuronAt(0, 2).Label = "Attribute";
            //na.GetNeuronAt(0, 3).Label = "Thing";
            //na.GetNeuronAt(0, 4).Label = "Sequence";
            //na.GetNeuronAt("say").SetValue(1);
            //if (naOut != null)
            //{
            //    na.GetNeuronAt(Array.IndexOf(cols, "say"), 2).AddSynapse(GetSpokenWord("Attribute").Id, 1, theNeuronArray, false);
            //    na.GetNeuronAt(Array.IndexOf(cols, "say"), 3).AddSynapse(GetSpokenWord("Thing").Id, 1, theNeuronArray, false);
            //    na.GetNeuronAt(Array.IndexOf(cols, "say"), 4).AddSynapse(GetSpokenWord("Sequence").Id, 1, theNeuronArray, false);
            //}
            Test();
        }



    }
}
