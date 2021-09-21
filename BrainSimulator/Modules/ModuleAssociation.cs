//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleAssociation : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleAssociation()
        {
            minHeight = 2;
            maxHeight = 500;
            minWidth = 2;
            maxWidth = 500;
        }


        ModuleUKS uks = null;

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            ModuleView naSource = theNeuronArray.FindModuleByLabel("UKS");
            if (naSource == null) return;
            uks = (ModuleUKS)naSource.TheModule;

            Thing mentalModel = uks.GetOrAddThing("MentalModel", "Visual");
            Thing wordParent = uks.GetOrAddThing("Word", "Audible");
            Thing attn = uks.GetOrAddThing("ATTN", "Thing");
            Thing attnVisualTarget = attn.GetReferenceWithAncestor(mentalModel);
            Thing attnAudibleTarget = attn.GetReferenceWithAncestor(wordParent);

            AssociateWordsWithVisuals(attnAudibleTarget);

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();
        }



        private IList<Link> GetBestReferences(Thing t, bool useRefereenceBy = false)
        {
            IList<Link> retVal = new List<Link>();
            IList<Link> references = t.References;
            if (useRefereenceBy) references = t.ReferencedBy;
            float bestValue = 0;
            foreach (Link l in references)
            {
                if (l.Value1 > bestValue)
                {
                    bestValue = l.Value1;
                }
            }
            foreach (Link l in references)
            {
                if (l.Value1 >= bestValue * 0.8f) //TODO perhaps add close matches?
                    retVal.Add(l);
            }
            return retVal;
        }

        private void AssociateWordsWithVisuals(Thing attnAudibleTarget)
        {
            if (attnAudibleTarget != null)
            {
                //IList<Thing> recentlyFiredWords = uks.Labeled("Word").DescendentsWhichFired;
                List<Thing> recentlyFiredWords = new List<Thing>();
                recentlyFiredWords.Add(attnAudibleTarget);
                IList<Thing> recentlyFiredRelationships = uks.Labeled("Relationship").DescendentsWhichFired;
                IList<Thing> recentlyFiredVisuals = uks.Labeled("Visual").DescendentsWhichFired;


                //set the hits
                foreach (Thing word in recentlyFiredWords)
                {
                    foreach (Thing relationshipType in recentlyFiredRelationships)
                    {
                        word.AdjustReference(relationshipType);
                    }
                    foreach (Thing visual in recentlyFiredVisuals)
                    {
                        if (!visual.HasAncestor(uks.GetOrAddThing("MentalModel", "Visual")))
                            word.AdjustReference(visual);
                    }
                }

                //set the misses for relationships
                foreach (Thing relationshipType in recentlyFiredRelationships)
                {
                    foreach (Link l in relationshipType.ReferencedBy)
                    {
                        Thing word = l.T;
                        if (word.HasAncestor(uks.Labeled("Word")))
                        {
                            if (!recentlyFiredWords.Contains(word))
                            {
                                word.AdjustReference(relationshipType, -1);
                            }
                        }
                    }
                }

                //set the misses for properties
                foreach (Thing visual in recentlyFiredVisuals)
                {
                    foreach (Link l in visual.ReferencedBy)
                    {
                        Thing word = l.T;
                        if (word.HasAncestor(uks.Labeled("Word")))
                        {
                            if (!recentlyFiredWords.Contains(word))
                            {
                                word.AdjustReference(visual, -1);
                            }
                        }
                    }
                }
            }
        }

        public Thing GetBestAssociation(Thing t)
        {
            if (uks == null)
            {
                ModuleView naSource = theNeuronArray.FindModuleByLabel("UKS");
                if (naSource == null) return null;
                uks = (ModuleUKS)naSource.TheModule;
            }
            IList<Thing> words = uks.Labeled("Word").Children;
            List<Thing> properties = uks.Labeled("Color").Children.ToList();
            properties.AddRange(uks.Labeled("Area").Children.ToList());
            properties = properties.OrderBy(x => x.Label).ToList();
            IList<Thing> relationships = uks.Labeled("Relationship").Children;
            relationships = relationships.OrderBy(x => x.Label.Substring(1)).ToList();

            float[,] values = GetAssociations();
            int row = -1;
            row = properties.IndexOf(t);
            if (row == -1)
            {
                row = relationships.IndexOf(t);
                if (row != -1) row += properties.Count;
            }
            if (row == -1) return null;

            float max = 0;
            int bestCol = -1;
            Thing best = null;
            for (int i = 0; i < values.GetLength(1); i++)
            {
                if (values[row, i] > max)
                {
                    max = values[row, i];
                    best = words[i];
                    bestCol = i;
                }
            }
            if (bestCol != -1)
            {
                for (int i = 0; i < values.GetLength(0); i++)
                {
                    if (values[i, bestCol] > max)
                    {
                        return null;
                    }
                }
            }
            return best;
        }

        public float[,] GetAssociations()
        {
            IList<Thing> words = uks.Labeled("Word").Children;
            List<Thing> properties = uks.Labeled("Color").Children.ToList();
            properties.AddRange(uks.Labeled("Area").Children.ToList());
            properties = properties.OrderBy(x => x.Label).ToList();
            IList<Thing> relationships = uks.Labeled("Relationship").Children;
            relationships = relationships.OrderBy(x => x.Label.Substring(1)).ToList();


            //collect all the values in a single spot
            float[,] values = new float[properties.Count + relationships.Count, words.Count];

            int row = 0;
            foreach (Thing property in properties)
            {
                for (int i = 0; i < words.Count; i++)
                {
                    Link l = words[i].HasReference(property);
                    if (l != null)
                    {
                        values[row, i] = l.Value1;
                    }
                }
                row++;
            }
            foreach (Thing relationship in relationships)
            {
                for (int i = 0; i < words.Count; i++)
                {
                    Link l = words[i].HasReference(relationship);
                    if (l != null)
                    {
                        values[row, i] = l.Value1;
                    }
                }
                row++;
            }
            return values;
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
            if (na == null) return; //this is called the first time before the module actually exists
        }
    }
}
