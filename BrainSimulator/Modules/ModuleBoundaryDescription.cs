//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleBoundaryDescription : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleBoundaryDescription()
        {
            minHeight = 2;
            maxHeight = 500;
            minWidth = 2;
            maxWidth = 500;
        }

        ModuleUKS uks = null;
        List<string> words = new List<string>();
        string currentWord = "";

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            ModuleView naSource = theNeuronArray.FindModuleByLabel("UKS");
            if (naSource == null) return;
            uks = (ModuleUKS)naSource.TheModule;

            ModuleView attnSource = theNeuronArray.FindModuleByLabel("Attention");
            if (attnSource == null) return;
            ModuleAttention m = (ModuleAttention)attnSource.TheModule;


            Thing mentalModelParent = uks.GetOrAddThing("MentalModel", "Visual");
            Thing wordParent = uks.GetOrAddThing("Word", "Audible");
            Thing attn = uks.GetOrAddThing("ATTN", "Thing");

            if (words.Count > 0)
            {
                if (currentWord != "")
                {
                    attn.RemoveReferencesWithAncestor(wordParent);
                }
                currentWord = words[0];
                //is this a reference to a visible object? does it contain a digit?
                if (words[0].IndexOfAny(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }) != -1)
                {
                    Thing t = uks.Labeled(words[0], mentalModelParent.Children);
                    if (t != null)
                    {
                        if (naSource != null)
                        {
                            m.SetAttention(t, -1);
                        }
                    }
                }
                else
                {
                    //process a single word & add it to the current phrase
                    currentWord = char.ToUpper(currentWord[0]) + currentWord.Substring(1);
                    Thing theWord = uks.GetOrAddThing("w" + currentWord, wordParent);
                    attn.AddReference(theWord);
                }
                //delete the word from the top of the list
                words.RemoveAt(0);
            }
            else 
            {
                if (attn.HasReferenceWithParent(wordParent) != null)
                {
                    attn.RemoveReferencesWithAncestor(wordParent);
                    m.SetAttention(null, 0);
                }
            }



            InnerMonologue();

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();
        }


        [XmlIgnore]
        public string descriptionString;
        Thing prevAttn = null;
        private void InnerMonologue()
        {
            Thing attn = uks.GetOrAddThing("ATTN", "Thing");
            Thing curAttn = attn.GetReferenceWithAncestor(uks.Labeled("Visual"));
            if (prevAttn == curAttn) return;
            if (curAttn == null) return;
            string newDescriptionString = "";

            ModuleView na = theNeuronArray.FindModuleByLabel("Association");
            if (na == null) return;
            ModuleAssociation ma = (ModuleAssociation)na.TheModule;

            foreach (Link l in curAttn.References)
            {
                Thing word = ma.GetBestAssociation(l.T);
                if (word != null)
                    newDescriptionString += word.Label + " ";
            }
            newDescriptionString += "\r";

            if (prevAttn != null)
            {
                var relationShips = prevAttn.GetRelationshipsByTarget(curAttn);
                foreach (Thing relationshipType in relationShips)
                {
                    Thing word = ma.GetBestAssociation(relationshipType);
                    if (word != null)
                        newDescriptionString += word.Label + " ";
                }
            }

            if (newDescriptionString != "" && newDescriptionString != descriptionString)
                descriptionString = newDescriptionString;
            prevAttn = curAttn;
        }

        [XmlIgnore]
        public string descriptionStringIn { get; set; }
        //commented-out code can add phrases to the UKS
        //Thing recentPhrase = null;
        public void SetDescription(string description)
        {
            if (uks == null) return;

            descriptionStringIn = description;
            ModuleView naImage = theNeuronArray.FindModuleByLabel("ImageFile");
            if (naImage == null) return;
            ModuleImageFile mif = (ModuleImageFile)naImage.TheModule;


            //Thing phraseParent = uks.GetOrAddThing("Phrase", "Audible");
            //Thing thePhrase = uks.AddThing("ph*", phraseParent);

            words = description.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            //TODO put words into phrase objects
            //foreach (string word in words1)
            //{

            //    string word1 = char.ToUpper(word[0]) + word.Substring(1);
            //    words.Add(word1);
            //    Thing wordParent = uks.GetOrAddThing("Word", "Audible");
            //    Thing theWord = uks.GetOrAddThing("w" + word1, wordParent);
            //    thePhrase.AddReference(theWord);
            //}

            //remove the phrase if it duplicates an existing phrase.
            //Thing matchingStoredPhrase = uks.ReferenceMatch(thePhrase.ReferencesAsThings, phraseParent.Children);
            //recentPhrase = matchingStoredPhrase;
            //if (matchingStoredPhrase != thePhrase)
            //{
            //    uks.DeleteThing(thePhrase);
            //}
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }
    }
}
