//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Windows;
using System.Diagnostics;

namespace BrainSimulator.Modules
{
    public class ModuleUKS : ModuleBase
    {
        //This is the actual Universal Knowledge Store
        protected List<Thing> UKS = new List<Thing>();

        //This is a temporary copy of the UKS which used during the save and restore process to 
        //break circular links by storing index values instead of actual links Note the use of SThing instead of Thing
        public List<SThing> UKSTemp = new List<SThing>();

        public override string ShortDescription { get => "Universal Knowledge Store for storing linked knowledge data"; }
        public override string LongDescription
        {
            get => "This module uses no neurons but can be called directly by other modules.\n\r" +
"Within the Knoweldge Store, everything is a 'Thing' (see the source code for the 'Thing' object). Things may have parents, children, " +
"references to other Things, and a 'value' which can be " +
"any .NET object (with Color and Point being implemented). " +
"It can search by value with an optional tolerance. A reference to another thing is done with a 'Link' " +
"which is a thing with an attached weight which can be examined and/or modified.\n\r " +
"Note that the Knowledge store is a bit like a neural network its own right if we consider a node to be a neuron " +
"and a link to be a synapse.";
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
        }

        //this is needed for the dialog treeview
        public List<Thing> GetTheKB()
        {
            return UKS;
        }

        //this is used to format debug output 
        private string ArrayToString(Thing[] list)
        {
            string retVal = ",";
            if (list == null) return ".";
            foreach (Thing t in list)
            {
                if (t == null) retVal += ".,";
                else retVal += t.ToString() + ",";
            }
            return retVal;
        }

        public Thing ThingExists(Thing[] parents, Thing[] references = null)
        {
            Thing found = null;
            List<Thing> things = GetChildren(parents[0]);
            foreach (Thing t in things)
            {
                bool referenceMissing = false;
                foreach (Thing t1 in references)
                {
                    if (t.References.Find(x => x.T == t1) == null)
                    {
                        referenceMissing = true;
                        break;
                    }
                }
                if (!referenceMissing)
                    return t;
            }
            return found;
        }

        public virtual Thing AddThing(string label, Thing parent, object value = null, Thing[] references = null)
        {
            return AddThing(label, new Thing[] { parent }, value, references);
        }
        public virtual Thing AddThing(string label, Thing[] parents, object value = null, Thing[] references = null)
        {
            Debug.WriteLine("AddThing: " + label + " (" + ArrayToString(parents) + ") (" + ArrayToString(references) + ")");
            Thing newThing = new Thing { Label = label, V = value };
            references = references ?? new Thing[0];
            for (int i = 0; i < parents.Length; i++)
            {
                if (parents[i] == null) return null;
                newThing.Parents.Add(parents[i]);
                parents[i].Children.Add(newThing);
            }

            for (int i = 0; i < references.Length; i++)
            {
                if (references[i] == null) return null;
                newThing.AddReference(references[i]);
            }

            UKS.Add(newThing);
            return newThing;
        }

        public virtual void DeleteThing(Thing t)
        {
            if (t.Children.Count != 0) return; //can't delete something with children...must delete all children first.
            foreach (Thing t1 in t.Parents)
                t1.Children.Remove(t);
            foreach (Link l1 in t.References)
                l1.T.ReferencedBy.RemoveAll(v => v.T == t);
            foreach (Link l1 in t.ReferencedBy)
                l1.T.References.RemoveAll(v => v.T == t);
            UKS.Remove(t);
        }

        //returns a thing with the given label
        //2nd paramter defines UKS to search, null=search entire knowledge store
        public Thing Labeled(string label, List<Thing> UKSt = null)
        {
            UKSt = UKSt ?? UKS; //if UKSt is null, search the entire UKS
            Thing retVal = null;
            retVal = UKSt.Find(t => t.Label == label);
            //if (retVal != null) retVal.useCount++;
            return retVal;
        }

        //returns the first thing it encounters which with a given value or null if none is found
        //the 2nd paramter defines the UKS to search (e.g. list of children)
        //if it is null, it searches the entire UKS,
        //the 3rd paramter defines the tolerance for spatial matches
        //if it is null, an exact match is required
        public virtual Thing ChildrenMatch(List<Thing> refs, List<Thing> UKSt = null)
        {
            UKSt = UKSt ?? UKS;
            foreach (Thing t in UKSt)
            {
                if (t.Children.Count == refs.Count)
                {
                    for (int i = 0; i < refs.Count; i++)
                    {
                        if (t.Children[i] != refs[i])
                            goto nextThing;
                    }
                    t.useCount++;
                    return t;
                nextThing:;
                }
            }
            return null;
        }

        public virtual Thing ReferenceMatch(List<Thing> refs, List<Thing> UKSt = null)
        {
            UKSt = UKSt ?? UKS;
            foreach (Thing t in UKSt)
            {
                if (t.References.Count == refs.Count)
                {
                    for (int i = 0; i < t.References.Count; i++)
                    {
                        if (t.References[i].T != refs[i])
                            goto nextThing;
                    }
                    //t.useCount++;
                    return t;
                nextThing:;
                }
            }
            return null;
        }

        //returns the first thing it encounters which with a given value or null if none is found
        //the 2nd paramter defines the UKS to search (e.g. list of children)
        //if it is null, it searches the entire UKS,
        //the 3rd paramter defines the tolerance for spatial matches
        //if it is null, an exact match is required
        public virtual Thing Valued(object value, List<Thing> UKSt = null, float toler = 0)
        {
            UKSt = UKSt ?? UKS;
            foreach (Thing t in UKSt)
            {
                if (t == null) continue;
                if (t.V is PointPlus p1 && value is PointPlus p2)
                {
                    if (p1.Near(p2, toler))
                    {
                        t.useCount++;
                        return t;
                    }
                }
                else
                {
                    if (t.V != null && t.V.Equals(value))
                    {
                        t.useCount++;
                        return t;
                    }
                }
            }
            return null;
        }


        //these two functions transform the UKS into an structure which can be serialized/deserialized
        //by translating object references into array indices, all the problems of circular references go away
        public override void SetUpBeforeSave()
        {
            base.SetUpBeforeSave();
            UKSTemp.Clear();
            foreach (Thing t in UKS)
            {
                SThing st = new SThing()
                {
                    label = t.Label,
                    V = t.V,
                    useCount = t.useCount
                };
                foreach (Thing t1 in t.Parents)
                {
                    st.parents.Add(UKS.FindIndex(x => x == t1));
                }
                foreach (Link l in t.References)
                {
                    Thing t1 = l.T;
                    if (l.hits != 0 && l.misses != 0) l.weight = l.hits / (float)l.misses;
                    st.references.Add(new Point(UKS.FindIndex(x => x == t1), l.weight));
                }
                UKSTemp.Add(st);
            }
        }
        public override void SetUpAfterLoad()
        {
            base.SetUpAfterLoad();
            UKS.Clear();
            foreach (SThing st in UKSTemp)
            {
                Thing t = new Thing()
                {
                    Label = st.label,
                    V = st.V,
                    useCount = st.useCount
                };
                UKS.Add(t);
            }
            for (int i = 0; i < UKSTemp.Count; i++)
            {
                foreach (int p in UKSTemp[i].parents)
                {
                    UKS[i].Parents.Add(UKS[p]);
                }
                foreach (Point p in UKSTemp[i].references)
                {
                    int hits = 0;
                    int misses = 0;
                    float weight = (float)p.Y;
                    if (weight != 0 && weight != 1)
                    {
                        hits = (int)(1000 / weight);
                        misses = 1000 - hits;
                    }
                    UKS[i].References.Add(new Link { T = UKS[(int)p.X], weight = weight, hits = hits, misses = misses });
                }
            }

            //rebuild all the reverse linkages
            foreach (Thing t in UKS)
            {
                foreach (Thing t1 in t.Parents)
                    t1.Children.Add(t);
                foreach (Link l in t.References)
                {
                    Thing t1 = l.T;
                    t1.ReferencedBy.Add(new Link { T = t, weight = l.weight });
                }
            }
        }

        //gets direct children
        public List<Thing> GetChildren(Thing t)
        {
            if (t == null) return new List<Thing>();
            return t.Children;
        }

        //recursively gets all descendents
        public IEnumerable<Thing> GetAllChildren(Thing T)
        {
            foreach (Thing t in T.Children)
            {
                foreach (Thing t1 in GetAllChildren(t))
                    yield return t1;
                yield return t;
            }
        }

        public Thing AddThing(string label, string parentLabel)
        {
            Thing retVal = Labeled(label);
            //if (retVal == null)
                retVal = AddThing(label, new Thing[] { Labeled(parentLabel) });
            return retVal;
        }

        public Thing AddThing(string label, Thing parent)
        {
            return AddThing(label, new Thing[] { parent });
        }

        public Thing FindBestReference(Thing t, Thing parent = null)
        {
            if (t == null) return null;
            Thing retVal = null;
            float bestWeight = -100;
            foreach (Link l in t.References)
            {
                if (parent == null || l.T.Parents[0] == parent)
                {
                    if (l.weight > bestWeight)
                    {
                        retVal = l.T;
                        bestWeight = l.weight;
                    }
                }
            }
            return retVal;
        }

        public override void Initialize()
        {
            //create an intial structure with some test data
            UKS.Clear();
            UKSTemp.Clear();
            AddThing("Thing", new Thing[] { });
            AddThing("Action", "Thing");
            AddThing("NoAction", "Action");
            AddThing("Stop", "Action");
            AddThing("Utterance", "Action");
            AddThing("SpeakPhn", "Action");
            AddThing("Vowel", "SpeakPhn");
            AddThing("Consonant", "SpeakPhn");
            AddThing("End", "Action");
            AddThing("Go", "Action");
            AddThing("RTurn", "Action");
            AddThing("LTurn", "Action");
            AddThing("UTurn", "Action");
            AddThing("Say", "Action");
            AddThing("Push", "Action");
            AddThing("SayRnd", "Action");
            AddThing("Attn", "Action");
            AddThing("Sense", "Thing");
            AddThing("Visual", "Sense");
            AddThing("Color", "Visual");
            AddThing("Shape", "Visual");
            AddThing("Landmark", "Visual");
            AddThing("Motion", "Visual");
            AddThing("SSegment", "Shape");
            AddThing("Point", "Shape");
            AddThing("Segment", "Shape");
            AddThing("Audible", "Sense");
            AddThing("Word", "Audible");
            AddThing("Phoneme", "Audible");
            AddThing("Phrase", "Audible");
            AddThing("ShortTerm", "Phrase");
            AddThing("phTemp", "ShortTerm");
            AddThing("NoWord", "Word");
            AddThing("Relation", "Thing");
            AddThing("Bigger", "Relation");
            AddThing("Closer", "Relation");
            AddThing("Event", "Thing");
            AddThing("Outcome", "Thing");
            AddThing("Positive", "Outcome");
            AddThing("Negative", "Outcome");
            AddThing("ModelThing", new Thing[] { });
        }
    }
}
