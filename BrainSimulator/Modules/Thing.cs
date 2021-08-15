//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  


using System;
using System.Collections.Generic;
using System.Windows;

namespace BrainSimulator
{
    //these are used so that Children can be readOnly. TODO: and References to help prevent broken 2-way links
    public static class IListExtensions
    {
        public static T FindFirst<T>(this IList<T> source, Func<T, bool> condition)
        {
            foreach (T item in source)
                if (condition(item))
                    return item;
            return default(T);
        }
        public static List<T> FindAll<T>(this IList<T> source, Func<T, bool> condition)
        {
            List<T> theList = new List<T>();
            foreach (T item in source)
                if (condition(item))
                    theList.Add(item);
            return theList;
        }
    }

    //a link is a weighted link to a thing
    public class Link
    {
        public Thing T;
        public float weight = 1;
        public int hits = 0;
        public int misses = 0;

        public override string ToString()
        {
            string retVal = T.Label + ":" + weight;
            return retVal;
        }
        //This is the (temporary) algorithm calculating the weight based on hits or misses
        public float Value()
        {
            float retVal = weight;
            if (hits != 0 && misses != 0)
            {
                float denom = misses;
                if (denom == 0) denom = .1f;
                retVal = hits / denom;
            }
            return retVal;
        }
    }

    //a thing is anything, physical object, attribute, word, action, etc.
    public class Thing
    {
        private List<Thing> parents = new List<Thing>(); //"is-a", others?
        private List<Thing> children = new List<Thing>(); //synapses to
        private List<Link> references = new List<Link>(); //synapses to "has", "is", others
        private List<Link> referencedBy = new List<Link>(); //synapses from

        private string label = ""; //this is just for convenience in debugging 
        object value;
        public int useCount = 0;
        public int currentReference = 0;



        public object V { get => value; set => this.value = value; }
        public string Label { get => label; set => label = value; }
        public IList<Thing> Parents { get => parents.AsReadOnly(); }
        public List<Thing> ParentsWriteable { get => parents; }
        public IList<Thing> Children { get => children.AsReadOnly(); }
        public List<Thing> ChildrenWriteable { get => children; }
        public IList<Link> References { get => references.AsReadOnly(); }
        public List<Link> ReferencesWriteable { get => references; }
        public IList<Link> ReferencedBy { get => referencedBy.AsReadOnly(); }
        public List<Link> ReferencedByWriteable { get => referencedBy; }

        public List<Thing> ReferencesAsThings
        {
            get
            {
                List<Thing> retVal = new List<Thing>();
                foreach (Link l in References)
                    retVal.Add(l.T);
                return retVal;
            }
        }


        public override string ToString()
        {
            string retVal = label + ":" + useCount;
            if (references.Count > 0)
            {
                retVal += " {";
                foreach (Link l in references)
                    retVal += l.T.label + ",";
                retVal += "}";
            }
            return retVal;
        }

        //add a reference from this thing to the specified thing
        public void AddReference(Thing t, float weight = 1)
        {
            if (t == null) return; //do not add null references
            //To prevent duplicates
            ReferencesWriteable.RemoveAll(v => v.T == t);
            t.ReferencedByWriteable.RemoveAll(v => v.T == this);
            ReferencesWriteable.Add(new Link { T = t, weight = weight });
            t.ReferencedByWriteable.Add(new Link { T = this, weight = weight });
        }

        public void InsertReferenceAt(int index, Thing t, float weight = 1)
        {
            if (t == null) return; //do not add null references
            if (index > References.Count) return;
            References.Insert(index, new Link { T = t, weight = weight });
            t.ReferencedBy.Add(new Link { T = this, weight = weight });
        }

        //(send a negative value to decrease a reference weight)
        public float AdjustReference(Thing t, float incr = 1)
        {
            //change any exisiting link or add a new one
            Link existingLink = References.FindFirst(v => v.T == t);
            if (existingLink == null)
            {
                AddReference(t, incr);
                return incr;
            }
            else
            {
                Link reverseLink = existingLink.T.referencedBy.Find(v => v.T == this);
                existingLink.weight += incr;
                if (incr > 0) existingLink.hits++;
                if (incr < 0) existingLink.misses++;
                reverseLink.weight = existingLink.weight;
                reverseLink.hits = existingLink.hits;
                reverseLink.misses = existingLink.misses;
                if (existingLink.weight < 0)
                {
                    //RemoveReference(existingLink.T);
                    return -1;
                }
                return existingLink.weight;
            }
        }

        public void RemoveReferenceAt(int i)
        {
            if (i < References.Count)
            {
                Link l = References[i];
                l.T.referencedBy.Remove(l);
                References.RemoveAt(i);
            }
        }

        public bool HasAncestorLabeled(string label)
        {
            foreach (Thing parent in parents)
            {
                if (parent.label == label)
                    return true;
                else if (parent.HasAncestorLabeled(label))
                    return true;
            }
            return false;
        }

        public void RemoveReference(Thing t)
        {
            references.RemoveAll(v => v.T == t);
            t.referencedBy.RemoveAll(v => v.T == this);
        }

        public void AddParent(Thing t)
        {
            if (t == null) return;
            if (!parents.Contains(t))
            {
                parents.Add(t);
                t.children.Add(this);
            }
        }
        public void RemoveParent(Thing t)
        {
            parents.Remove(t);
            t.children.Remove(this);
        }

        public void AddChild(Thing t)
        {
            children.Add(t);
            t.parents.Add(this);
        }
        public void RemoveChild(Thing t)
        {
            t.parents.Remove(this);
            children.Remove(t);
        }
        public void RemoveAllChildren()
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                RemoveChild(children[i]);
            }
        }
        public float Distance(Thing t, bool ordered = false)
        {
            float retVal = 0;
            if (t == this) return -1;

            if (ordered)
            {
                for (int i = 0; i < Math.Min(References.Count, t.references.Count); i++)
                {
                    Link l = References[i];
                    Link lt = t.References[i];
                    if (l.T == lt.T) retVal++;
                }
                if (References.Count > 0)
                    retVal /= Math.Max(References.Count, t.References.Count);
            }
            else
            {
                for (int i = 0; i < References.Count; i++)
                {
                    Link l = References[i];
                    if (t.References.FindFirst(x => x.T == l.T) != null) retVal++;
                }
                if (References.Count > 0)
                    retVal /= Math.Max(References.Count, t.References.Count);
            }
            return retVal;
        }
        public Thing IsSibling(Thing t)
        {
            foreach (Thing parent in parents)
            {
                if (t.parents.Contains(parent))
                    return parent;
            }
            return null;
        }
        private static int CompareByWeight(Link l1, Link l2)
        {
            return (l1.weight > l2.weight) ? -1 : 1;
        }
        public List<Link> FindSimilar(IList<Thing> KB, bool ordered, int maxCount = 10)
        {
            List<Link> retVal = new List<Link>();
            foreach (Thing t in KB)
            {
                float distance = Distance(t, ordered);
                if (distance > 0 && t != this)
                    retVal.Add(new Link { weight = distance, T = t });
            }
            retVal.Sort(CompareByWeight);
            if (retVal.Count > maxCount)
                retVal.RemoveRange(maxCount, retVal.Count - maxCount);
            return retVal;
        }

        public Thing Clone(Thing t)
        {
            Thing t1 = new Thing()
            {
                V = t.V,
                Label = t.Label,
            };
            foreach (Thing t2 in t.Parents)
                t1.Parents.Add(t2);
            foreach (Thing t2 in t.Children)
                t1.Children.Add(t2);
            foreach (Link l in t.References)
            {
                Link l1 = new Link { T = l.T, hits = l.hits, misses = l.misses, weight = l.weight };
                t1.References.Add(l1);
            }
            foreach (Link l in t.ReferencedBy)
            {
                Link l1 = new Link { T = l.T, hits = l.hits, misses = l.misses, weight = l.weight };
                t1.ReferencedBy.Add(l1);
            }
            return t1;
        }


        public enum ReferenceDirection { reference, referenceBy };

        public Thing MostLikelyReference(ReferenceDirection rd, Thing parent = null)
        {
            Link retVal = null;
            if (rd == ReferenceDirection.reference)
            {
                foreach (Link l in References)
                {
                    if (l.weight <= 0) continue;
                    if (parent != null && l.T.parents[0] != parent) continue;
                    float strength = l.weight;
                    if (l.hits > 0 && l.misses > 0)
                    {
                        strength *= (float)l.hits / (float)l.misses; //TODO subject to revision
                    }
                    if (retVal == null || retVal.weight < strength)
                        retVal = l;
                }
            }
            else
            {
                foreach (Link l in ReferencedBy)
                {
                    if (l.weight <= 0) continue;
                    if (parent != null && l.T.parents[0] != parent) continue;
                    float strength = l.weight;
                    if (l.hits > 0 && l.misses > 0)
                    {
                        strength *= (float)l.hits / (float)l.misses; //TODO subject to revision
                    }
                    if (retVal == null || retVal.weight < strength)
                        retVal = l;
                }

            }
            if (retVal == null) return null;
            return retVal.T;
        }
    }

    //this is a modification of Thing which is used to store and retrieve the KB in XML
    //it eliminates circular references by replacing Thing references with int indexed into an array and makes things much more compact
    public class SThing
    {
        public string label = ""; //this is just for convenience in debugging and should not be used
        public List<int> parents = new List<int>(); //"is-a"?
        public List<Point> references = new List<Point>(); //we're casting this as a point because it is small in xml
        //x is the index of the link, y is the weight
        object value;
        public object V { get => value; set => this.value = value; }
        public int useCount;
    }
}
