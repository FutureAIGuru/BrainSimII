//
// Copyright (c) Charles Simon. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//


using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;


namespace BrainSimulator
{
    //these are used so that Children can be readOnly. This prevents programmers from accidentally doing a (e.g.) Child.Add() which will not handle reverse links properly
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
            string retVal = T.Label + ":" + Value1.ToString("f3");
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

        public float Value1
        {
            get
            {
                float val = hits*hits / (float)(misses+1);
                if (float.IsNaN(val))
                    val = -1;
                return val;
            }
        }
    }

    public class Relationship : Link
    {
        //A relationship consists of a source, a target, and a type.
        //The source is usually implied as the owner and the target is T of the inherited link
        public Thing relationshipType;
        public Thing source;
        public override string ToString()
        {
            return source.Label + "->" + relationshipType.Label + "->" + T.Label;
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
        public long lastFired = 0;


        public object V { get => value; set => this.value = value; }
        public string Label
        {
            get
            {
                return label;
            }

            set
            { //This code allows you to put a * at the end of a label and it will auto-increment
                string newLabel = value;
                if (newLabel.EndsWith("*"))
                {
                    int greatestValue = -1;
                    string baseString = newLabel.Substring(0, newLabel.Length - 1);
                    foreach (Thing parent in parents)
                    {
                        foreach (Thing t in parent.Children)
                        {
                            if (t.Label.StartsWith(baseString))
                            {
                                if (int.TryParse(t.Label.Substring(baseString.Length), out int theVal))
                                {
                                    if (theVal > greatestValue)
                                        greatestValue = theVal;
                                }

                            }
                        }
                    }
                    greatestValue++;
                    newLabel = baseString + greatestValue.ToString();
                }
                label = newLabel;
            }
        }
        public IList<Thing> Parents { get => parents.AsReadOnly(); }
        public List<Thing> ParentsWriteable { get => parents; }
        public IList<Thing> Children { get => children.AsReadOnly(); }
        public List<Thing> ChildrenWriteable { get => children; }
        public IList<Link> References { get => references.AsReadOnly(); }
        public List<Link> ReferencesWriteable { get => references; }
        public IList<Link> ReferencedBy { get => referencedBy.AsReadOnly(); }
        public List<Link> ReferencedByWriteable { get => referencedBy; }


        //recursively gets all descendents
        public IEnumerable<Thing> Descendents
        {
            get
            {
                foreach (Thing t in Children)
                {
                    foreach (Thing t1 in t.Descendents)
                        yield return t1;
                    yield return t;
                }
            }
        }

        public IList<Thing> DescendentsWhichFired
        {
            get
            {
                IList<Thing> retVal = new List<Thing>();
                long best = 0;
                foreach (Thing t in Descendents)
                {
                    if (t.lastFired > best)
                    {
                        best = t.lastFired;
                        retVal.Clear();
                    }
                    if (t.lastFired == best)
                    {
                        retVal.Add(t);
                    }
                }
                return retVal;
            }
        }


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

        public void SetFired(Thing t = null)
        {
            if (t != null) t.lastFired = MainWindow.theNeuronArray.Generation;
            else lastFired = MainWindow.theNeuronArray.Generation;
        }

        //add a reference from this thing to the specified thing
        public Link AddReference(Thing t, float weight = 1)
        {
            if (t == null) return null; //do not add null references or duplicates
            ReferencesWriteable.RemoveAll(v => v.T == t);
            t.ReferencedByWriteable.RemoveAll(v => v.T == this);

            Link newLink = new Link { T = t, weight = weight };
            ReferencesWriteable.Add(newLink);
            t.ReferencedByWriteable.Add(new Link { T = this, weight = weight });
            //SetFired();
            //SetFired(t);
            return newLink;
        }

        public void InsertReferenceAt(int index, Thing t, float weight = 1)
        {
            if (t == null) return; //do not add null references
            if (index > References.Count) return;
            References.Insert(index, new Link { T = t, weight = weight });
            t.ReferencedBy.Add(new Link { T = this, weight = weight });
            //SetFired();
            //SetFired(t);
        }

        public Link HasReference(Thing t)
        {
            foreach (Link L in References)
                if (L.T == t) return L;
            return null;
        }
        public Thing HasReferenceWithParent(Thing t)
        {
            foreach (Link L in References)
                if (L.T.parents.Contains(t)) return L.T;
            return null;
        }
        public Thing HasReferenceByWithParent(Thing t)
        {
            foreach (Link L in ReferencedBy)
                if (L.T.parents.Contains(t)) return L.T;
            return null;
        }

        //(send a negative value to decrease a reference weight)
        public float AdjustReference(Thing t, float incr = 1)
        {
            //change any exisiting link or add a new one
            Link existingLink = References.FindFirst(v => v.T == t);
            if (existingLink == null && incr > 0)
            {
                existingLink = AddReference(t, incr);
            }
            if (existingLink != null)
            {
                Link reverseLink = existingLink.T.referencedBy.Find(v => v.T == this);
                existingLink.weight += incr;
                if (incr > 0) existingLink.hits++;
                if (incr < 0) existingLink.misses++;
                reverseLink.weight = existingLink.weight;
                reverseLink.hits = existingLink.hits;
                reverseLink.misses = existingLink.misses;
                if (existingLink is Relationship r)
                {
                    //TODO adjust the weight of relationshipType revers link
                }
                if (existingLink.weight < 0)
                {
                    //RemoveReference(existingLink.T);
                    return -1;
                }
                return existingLink.weight;
            }
            return 0;
        }

        public void RemoveReferenceAt(int i)
        {
            if (i < References.Count)
            {
                Link l = References[i];
                l.T.referencedBy.Remove(l);
                References.RemoveAt(i);
            }
            //SetFired();
        }

        public Relationship AddRelationship(Thing t2, Thing relationshipType)
        {
            if (t2 == null || relationshipType == null)
                return null;

            relationshipType.SetFired();
            Relationship r = HasRelationship(t2, relationshipType);
            if (r != null)
            {
                AdjustReference(r.T);
                return r;
            }
            r = new Relationship { T = t2, source = this, relationshipType = relationshipType };
            ReferencesWriteable.Add(r);
            Relationship rRev = new Relationship { T = this, source = t2, relationshipType = relationshipType };
            t2.ReferencedByWriteable.Add(rRev);
            relationshipType.ReferencedByWriteable.Add(r);
            return r;
        }

        public Relationship HasRelationship(Thing t2, Thing relationshipType)
        {
            Relationship retVal = null;
            foreach (Link L in References)
            {
                if (L is Relationship r)
                {
                    if ((r.relationshipType == relationshipType || relationshipType == null) &&
                        (r.T == t2 || t2 == null))
                    {
                        retVal = r;
                        break;
                    }
                }
            }
            return retVal;
        }
        public List<Thing> GetRelationshipsByTarget(Thing t2)
        {
            List<Thing> retVal = new List<Thing>();
            foreach (Link l in References)
            {
                if (l is Relationship r)
                {
                    if (r.T == t2)
                    {
                        retVal.Add(r.relationshipType);
                    }
                }
            }
            return retVal;
        }
        public List<Link> GetRelationshipsByType(Thing t1, Thing relationshipType, int recursionDepth = 0)
        {
            if (t1 == null) t1 = this;
            List<Link> retVal = new List<Link>();
            do
            {
                foreach (Link l in t1.References)
                {
                    if (l is Relationship r)
                    {
                        if (relationshipType == null || r.relationshipType == relationshipType)
                        {
                            retVal.Add(l);
                        }
                    }
                }
                if (recursionDepth > 0)
                {
                    foreach (Link l in retVal)
                    {
                        retVal.AddRange(GetRelationshipsByType(l.T, relationshipType, recursionDepth - 1));
                    }
                }
                recursionDepth--;
            } while (recursionDepth >= 0);
            return retVal;
        }


        public void RemoveRelationship(Thing t2, Thing relationshipType)
        {
            RemoveReference(t2);
        }


        public bool HasAncestorLabeled(string label)
        {
            for (int i = 0; i < parents.Count; i++)
            {
                Thing parent = parents[i];
                if (parent.label == label)
                    return true;
                else if (parent.HasAncestorLabeled(label))
                    return true;
            }
            return false;
        }
        public bool HasAncestor(Thing t)
        {
            for (int i = 0; i < parents.Count; i++)
            {
                Thing parent = parents[i];
                //    foreach (Thing parent in parents)
                //{
                if (parent == t)
                    return true;
                else if (parent.HasAncestor(t))
                    return true;
            }
            return false;
        }

        public void RemoveReferencesWithAncestor(Thing t)
        {
            if (t == null) return;
            for (int i = 0; i < references.Count; i++)
            {
                if (references[i].T.HasAncestor(t))
                {
                    RemoveReference(references[i].T);
                    i--;
                }
            }
        }
        
        //returns all the matching refrences
        public List<Link> GetReferencesWithAncestor(Thing t)
        {
            List<Link> retVal = new List<Link>();
            for (int i = 0; i < references.Count; i++)
            {
                if (references[i].T.HasAncestor(t))
                {
                    retVal.Add(references[i]);
                }
            }
            return retVal.OrderBy(x => -x.Value1).ToList();
        }

        //returns the best matching reference
        public Thing GetReferenceWithAncestor(Thing t)
        {
            List<Link> refs =  GetReferencesWithAncestor(t);
            if (refs.Count > 0)
                return refs[0].T;
            return null;
        }


        public List<Link> GetReferencedByWithAncestor(Thing t)
        {
            List<Link> retVal = new List<Link>();
            for (int i = 0; i < referencedBy.Count; i++)
            {
                if (referencedBy[i].T.HasAncestor(t))
                {
                    retVal.Add(referencedBy[i]);
                }
            }
            return retVal.OrderBy(x => -x.Value1).ToList();
        }
        public void RemoveReference(Thing t)
        {
            if (t == null) return;
            foreach (Link l in References)
            {
                if (l is Relationship r)
                {
                    r.relationshipType.referencedBy.RemoveAll(v => v.T == r.source);
                }
            }
            references.RemoveAll(v => v.T == t);
            t.referencedBy.RemoveAll(v => v.T == this);
            //SetFired();
            //SetFired(t);
        }
        public void RemoveReferencedBy(Thing t)
        {
            if (t == null) return;
            referencedBy.RemoveAll(v => v.T == t);
            t.references.RemoveAll(v => v.T == this);
            //SetFired();
            //SetFired(t);
        }

        public void AddParent(Thing t)
        {
            if (t == null) return;
            if (!parents.Contains(t))
            {
                parents.Add(t);
                t.children.Add(this);
            }
            //SetFired();
            //SetFired(t);
        }
        public void RemoveParent(Thing t)
        {
            parents.Remove(t);
            t.children.Remove(this);
            //SetFired();
            //SetFired(t);
        }

        public void AddChild(Thing t)
        {
            children.Add(t);
            t.parents.Add(this);
            //SetFired();
            //SetFired(t);
        }
        public void RemoveChild(Thing t)
        {
            t.parents.Remove(this);
            children.Remove(t);
            //SetFired();
            //SetFired(t);
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

        public Thing Clone()
        {
            Thing t = this;
            Thing t1 = new Thing()
            {
                V = t.V,
                Label = t.Label,
            };
            //            foreach (Thing t2 in t.Parents)
            //                t1.AddParent(t2);
            foreach (Thing t2 in t.Children)
                t1.AddChild(t2);
            foreach (Link l in t.References)
            {
                //                Link l1 = new Link { T = l.T, hits = l.hits, misses = l.misses, weight = l.weight };
                t1.AddReference(l.T);
            }
            foreach (Link l in t.ReferencedBy)
            {
                //    Link l1 = new Link { T = l.T, hits = l.hits, misses = l.misses, weight = l.weight };
                l.T.AddReference(t);
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
