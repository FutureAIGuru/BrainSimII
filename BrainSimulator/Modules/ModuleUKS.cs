//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace BrainSimulator.Modules
{
    public class ModuleUKS : ModuleBase
    {
        //This is the actual Universal Knowledge Store
        protected List<Thing> UKS = new List<Thing>();

        //This is a temporary copy of the UKS which used during the save and restore process to 
        //break circular links by storing index values instead of actual links Note the use of SThing instead of Thing
        public List<SThing> UKSTemp = new List<SThing>();

        public ModuleUKS()
        {
            allowMultipleDialogs = true;
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
        }

        //this is needed for the dialog treeview
        public List<Thing> GetTheUKS()
        {
            return UKS;
        }

        //this is used to format debug output 
        private string ArrayToString(Thing[] list)
        {
            string retVal = "";
            if (list == null) return ".";
            foreach (Thing t in list)
            {
                if (t == null) retVal += ".,";
                else retVal += t.ToString() + ",";
            }
            int index = retVal.LastIndexOf(",");
            if (index != -1)
                retVal = retVal.Remove(index, 1);
            return retVal;
        }

        public Thing ThingExists(Thing[] parents, Thing[] references = null)
        {
            Thing found = null;
            IList<Thing> things = GetChildren(parents[0]);
            foreach (Thing t in things)
            {
                bool referenceMissing = false;
                foreach (Thing t1 in references)
                {
                    if (t.References.FindFirst(x => x.T == t1) == null)
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
            //Debug.WriteLine("AddThing: " + label + " (" + ArrayToString(parents) + ") (" + ArrayToString(references) + ")");
            Thing newThing = new Thing { V = value };
            references = references ?? new Thing[0];
            for (int i = 0; i < parents.Length; i++)
            {
                if (parents[i] == null) return null;
                newThing.ParentsWriteable.Add(parents[i]);
                parents[i].ChildrenWriteable.Add(newThing);
                //parents[i].SetFired();
            }

            for (int i = 0; i < references.Length; i++)
            {
                if (references[i] == null) return null;
                newThing.AddReference(references[i]);
                //references[i].SetFired();
            }

            newThing.Label = label;
            UKS.Add(newThing);
            //newThing.SetFired();
            return newThing;
        }

        public virtual void DeleteThing(Thing t)
        {
            //Debug.Assert(UKS.Contains(t)); //if the thing is not in the UKS, it is free floating. Just let it go out of scope
            if (t.Children.Count != 0)
                return; //can't delete something with children...must delete all children first.
            foreach (Thing t1 in t.Parents)
            {
                t1.ChildrenWriteable.Remove(t);
            }
            foreach (Link l1 in t.References)
            {
                if (l1 is Relationship r)
                {
                    List<Link> reverseRefs = r.relationshipType.ReferencedByWriteable;
                    for (int i = reverseRefs.Count-1; i >= 0; i--)
                    {
                        if (reverseRefs[i] is Relationship rr1)
                        {
                            if (rr1.source == t && rr1.T == r.T)
                            {
                                reverseRefs.RemoveAt(i);
                            }
                        }
                    }
                }
                l1.T.ReferencedByWriteable.RemoveAll(v => v.T == t);
            }
            foreach (Link l1 in t.ReferencedBy)
            {
                if (l1 is Relationship r)
                {
                    List<Link> reverseRefs = r.relationshipType.ReferencedByWriteable;
                    for (int i = reverseRefs.Count - 1; i >= 0; i--)
                    {
                        if (reverseRefs[i] is Relationship rr1)
                        {
                            if (rr1.source == r.T && rr1.T == r.source)
                            {
                                reverseRefs.RemoveAt(i);
                            }
                        }
                    }
                }
                l1.T.ReferencesWriteable.RemoveAll(v => v.T == t);
            }
            UKS.Remove(t);
        }

        //returns a thing with the given label
        //2nd paramter defines UKS to search, null=search entire knowledge store
        public Thing Labeled(string label, IList<Thing> UKSt = null)
        {
            UKSt = UKSt ?? UKS; //if UKSt is null, search the entire UKS
            Thing retVal = null;
            for (int i = 0; i < UKSt.Count; i++)
            {
                Thing t = UKSt[i];
                if (t.Label == label)
                    return t;
            }
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

        public virtual Thing ReferenceMatch(List<Thing> refs, IList<Thing> UKSt = null)
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
        public virtual Thing Valued(object value, IList<Thing> UKSt = null, float toler = 0)
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
                    if (p >= 0)
                        UKS[i].ParentsWriteable.Add(UKS[p]);
                }
                foreach (Point p in UKSTemp[i].references)
                {
                    if (p.X < 0) break;
                    int hits = 0;
                    int misses = 0;
                    float weight = (float)p.Y;
                    if (weight != 0 && weight != 1)
                    {
                        hits = (int)(1000 / weight);
                        misses = 1000 - hits;
                    }
                    UKS[i].ReferencesWriteable.Add(new Link { T = UKS[(int)p.X], weight = weight, hits = hits, misses = misses });
                }
            }

            //rebuild all the reverse linkages
            foreach (Thing t in UKS)
            {
                foreach (Thing t1 in t.Parents)
                    t1.ChildrenWriteable.Add(t);
                foreach (Link l in t.References)
                {
                    Thing t1 = l.T;
                    t1.ReferencedByWriteable.Add(new Link { T = t, weight = l.weight });
                }
            }
        }

        //gets direct children
        public IList<Thing> GetChildren(Thing t)
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
        public void DeleteAllChilden(Thing t)
        {
            while (t.Children.Count > 0)
            {
                if (t.Children[0].Parents.Count == 1)
                {
                    DeleteAllChilden(t.Children[0]);
                    DeleteThing(t.Children[0]);
                }
                else
                {//this thing has multiple parents.
                    t.RemoveChild(t.Children[0]);
                }
            }
        }

        public Thing GetOrAddThing(string label, Thing parent)
        {
            if (parent == null)
                return null;
            Thing retVal = Labeled(label, parent.Children);
            if (retVal == null)
                retVal = AddThing(label, parent);
            return retVal; //if the thing already exists, return it and do not add a duplicate
        }
        public Thing GetOrAddThing(string label, string parentLabel)
        {
            Thing parent = Labeled(parentLabel);
            if (parent == null)
                return null;
            Thing retVal = Labeled(label, parent.Children);
            if (retVal == null)
                retVal = AddThing(label, parentLabel);
            return retVal; //if the thing already exists, return it and do not add a duplicate
        }

        public Thing AddThing(string label, string parentLabel)
        {
            Thing parent = Labeled(parentLabel);
            if (parent == null)
                return null;
            Thing retVal = AddThing(label, new Thing[] { Labeled(parentLabel) });
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
                    if (l.Value1 > bestWeight)
                    {
                        retVal = l.T;
                        bestWeight = l.Value1;
                    }
                }
            }
            return retVal;
        }

        public Thing SetValue(Thing t1, float value, string valueName, int digits)
        {
            Thing valueParent = GetOrAddThing("Value", "Thing");
            if (digits > 0)
                value = Utils.RoundToSignificantDigits(value, digits);
            for (int i = t1.References.Count - 1; i >= 0; i--)
            {
                if (t1.References[i].T.Label.StartsWith(valueName))
                    t1.RemoveReference(t1.References[i].T);
            }
            Thing retVal = GetOrAddThing(valueName + "'" + value, valueParent);
            t1.AddReference(retVal);
            return retVal;
        }
        public float GetValue(Thing t1, string valueName)
        {
            var values = GetValues(t1);
            if (values.ContainsKey(valueName))
                return values[valueName];
            return -1;
        }
        public Dictionary<string, float> GetChangedValues(Thing t1, Thing t2)
        {
            Dictionary<string, float> retVal = new Dictionary<string, float>();
            var values1 = GetValues(t1);
            var values2 = GetValues(t2);
            foreach (var v1 in values1)
            {
                if (values2.ContainsKey(v1.Key))
                {
                    float delta = values1[v1.Key] - values2[v1.Key];
                    if (delta != 0)
                        retVal.Add(v1.Key, delta);
                }
            }
            return retVal;
        }

        public Dictionary<string, float> GetValues(Thing t)
        {
            Dictionary<string, float> retVal = new Dictionary<string, float>();
            foreach (Link l in t.References)
            {
                int splitPoint = l.T.Label.IndexOf("'");
                if (splitPoint != -1)
                {
                    string name = l.T.Label.Substring(0, splitPoint);
                    if (l.T.HasAncestorLabeled("Value"))
                        name += "+";
                    if (float.TryParse(l.T.Label.Substring(splitPoint + 1), out float value))
                        retVal.Add(name, value);
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
            AddThing("SayRnd", "Action");
            AddThing("Sense", "Thing");
            AddThing("Visual", "Sense");
            AddThing("Touch", "Sense");
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
            AddThing("Event", "Thing");
            AddThing("Outcome", "Thing");
            AddThing("Positive", "Outcome");
            AddThing("Negative", "Outcome");
            AddThing("ModelThing", new Thing[] { });
        }
    }
}
