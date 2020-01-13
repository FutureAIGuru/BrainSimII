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
    public class Module2DKB : ModuleBase
    {
        protected List<Thing> UKS = new List<Thing>();

        public List<SThing> KB1 = new List<SThing>();

        public override string ShortDescription { get => "Knowledge Base for storing linked knowledge data"; }
        public override string LongDescription
        {
            get => "This module uses no neurons but can be called directly by other modules.\n\r" +
"Within the KB, everything is a 'Thing' (see the source code for the 'Thing' object). Things may have parents, children, " +
"references to other Things, and a 'value' which can be " +
"any .NET object (with Color and Point being implemented). " +
"It can search by value with an optional tolerance. A reference to another thing is done with a 'Link' " +
"which is a thing with an attached weight which can be examined and/or modified.\n\r " +
"Note that the Knowledge Base is a bit like a neural network its own right if we consider a node to be a neuron " +
"and a link to be a synapse.";
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
        }

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
        //2nd paramter defines KB to search, null=search entire knowledge base
        public Thing Labeled(string label, List<Thing> KBt = null)
        {
            KBt = KBt ?? UKS;
            Thing retVal = null;
            retVal = KBt.Find(t => t.Label == label);
            if (retVal != null) retVal.useCount++;

            return retVal;
        }

        //returns the first thing it encounters which with a given value or null if none is found
        //the 2nd paramter defines the KB to search (e.g. list of children)
        //if it is null, it searches the entire KB,
        //the 3re paramter defines the tolerance for spatial matches
        //if it is null, an exact match is required
        public virtual Thing Valued(object value, List<Thing> KBt = null, float toler = 0)
        {
            KBt = KBt ?? UKS;
            foreach (Thing t in KBt)
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

        //returns a list of all things which share the given parent thing
        public virtual List<Thing> HavingParent(Thing parent)
        {
            if (parent == null) return null;

            return parent.Children;
        }

        //these two functions transform the KB into an structure which can be serialized/deserialized
        public override void SetUpBeforeSave()
        {
            base.SetUpBeforeSave();
            KB1.Clear();
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
                KB1.Add(st);
            }
        }
        public override void SetUpAfterLoad()
        {
            base.SetUpAfterLoad();
            UKS.Clear();
            foreach (SThing st in KB1)
            {
                Thing t = new Thing()
                {
                    Label = st.label,
                    V = st.V,
                    useCount = st.useCount
                };
                UKS.Add(t);
            }
            for (int i = 0; i < KB1.Count; i++)
            {
                foreach (int p in KB1[i].parents)
                {
                    UKS[i].Parents.Add(UKS[p]);
                }
                foreach (Point p in KB1[i].references)
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

        public List<Thing> GetChildren(Thing t)
        {
            if (t == null) return new List<Thing>();
            return t.Children;
        }

        public void AddThing (string label, string parentLabel)
        {
            AddThing(label, new Thing[] { Labeled(parentLabel) });
        }

        public override void Initialize()
        {
            //create an intial structure with some test data
            UKS.Clear();
            KB1.Clear();
            AddThing("ROOT", new Thing[] { });
            AddThing("Sense", "ROOT");
            AddThing("Color","Sense");
            AddThing("Shape","Sense");
            AddThing("Point","Shape");
            AddThing("Dot","Shape");
            AddThing("TempP","Point");
            AddThing("Segment","Shape");
            AddThing("Word","Sense");
            AddThing("Phrase","Sense");
            AddThing("NoWord","Word");
            AddThing("Action","ROOT");
            AddThing("NoAction","Action");
            AddThing("Go","Action");
            AddThing("Stop","Action");
            AddThing("RTurn","Action");
            AddThing("LTurn","Action");
            AddThing("UTurn","Action");
            AddThing("Say","Action");
            AddThing("Attn","Action");
            AddThing("Situation","ROOT");
            AddThing("Outcome","ROOT");
            AddThing("Positive","Outcome");
            AddThing("Negative","Outcome");
        }
    }
}
