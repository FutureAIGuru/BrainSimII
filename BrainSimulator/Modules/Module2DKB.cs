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

namespace BrainSimulator
{
    //TODO: longer term...turn this into a neuron model (with multiple types of synapses)

    public class Link
    {
        public Thing T;
        public float weight=1;
    }

    public class Thing
    {
        private string label = ""; //this is just for convenience in debugging and should not be used
        private List<Thing> parents = new List<Thing>(); //"is-a"?
        private List<Thing> children = new List<Thing>(); //synapses to
        private List<Link> references = new List<Link>(); //synapses to //"has-a"?
        private List<Link> referencedBy = new List<Link>(); //synapses from
        object value;

        public object V { get => value; set => this.value = value; }
        public string Label { get => label; set => label = value; }
        public List<Thing> Parents { get => parents; }
        public List<Thing> Children { get => children; set => children = value; }
        public List<Link> References { get => references; set => references = value; }
        public List<Link> ReferencedBy { get => referencedBy; set => referencedBy = value; }

        public void AddReference(Thing t, float weight = 1)
        {
            //change any 
            References.RemoveAll(v => v.T == t);
            t.ReferencedBy.RemoveAll(v => v.T == this);
            References.Add(new Link { T = t, weight = weight });
            t.ReferencedBy.Add(new Link { T = this, weight = weight });
        }

        //(send a negative value to decrease a reference)
        public void AdjustReference(Thing t, float incr = 1)
        {
            //change any exisiting link or add a new one
            Link existingLink = References.Find(v => v.T == t);
            if (existingLink == null)
            {AddReference(t,incr); }
            else { existingLink.weight += incr; }
        }

        public void RemoveReference(Thing t)
        {
            References.RemoveAll(v => v.T == t);
            t.ReferencedBy.RemoveAll(v =>v.T == this);
        }

        public void AddParent(Thing t)
        {
            parents.Add(t);
            t.Children.Add(this);
        }
        public void RemoveParent(Thing t)
        {
            Parents.Remove(t);
            t.Children.Remove(this);
        }
    }

    //this is the KB which is used to store and retrieve the KB in XML
    //it eliminates circular references and makes things much more compact
    public class SThing
    {
        public string label = ""; //this is just for convenience in debugging and should not be used
        public List<int> parents = new List<int>(); //"is-a"?
        public List<Point> references = new List<Point>(); //we're casting this as a point because it is small in xml
        //x is the index of the link, y is the confidence
        object value;
        public object V { get => value; set => this.value = value; }
    }


    public class Module2DKB : ModuleBase
    {
        private List<Thing> KB = new List<Thing>();
        public List<SThing> KB1 = new List<SThing>();

        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
        }
        public Thing AddThing(string label, Thing[] parents, object value = null, Thing[] references = null)
        {
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

            KB.Add(newThing);
            return newThing;
        }

        public void DeleteThing(Thing t)
        {
            if (t.Children.Count != 0) return; //can't delete something with children...must delete all children first.
            foreach (Thing t1 in t.Parents)
                t1.Children.Remove(t);
            foreach (Link l1 in t.References)
                l1.T.ReferencedBy.RemoveAll (v=>v.T == t);
            foreach (Link l1 in t.ReferencedBy)
                l1.T.References.RemoveAll(v=>v.T==t);
            KB.Remove(t);
        }

        public Thing Labeled(string label, List<Thing> KBt = null)
        {
            KBt = KBt ?? KB;
            Thing retVal = null;
            retVal = KBt.Find(t => t.Label == label);
            return retVal;
        }
        public Thing Valued(object value, List<Thing> KBt = null,float toler = 0)
        {
            KBt = KBt ?? KB;
            foreach (Thing t in KBt)
            {
                if (t.V is PointPlus p1 && value is PointPlus p2)
                {
                    if (p1.Near(p2, toler)) return t;
                }
                else
                {
                    if (t.V != null && t.V.Equals(value))
                    {
                        return t;
                    }
                }
            }
            return null;
        }

        public List<Thing> HavingParent(Thing parent)
        {
            if (parent == null) return null;

            return parent.Children;
        }

        public override void SetUpBeforeSave()
        {
            base.SetUpBeforeSave();
            KB1.Clear();
            foreach (Thing t in KB)
            {
                SThing st = new SThing()
                {
                    label = t.Label,
                    V = t.V
                };
                foreach (Thing t1 in t.Parents)
                {
                    st.parents.Add(KB.FindIndex(x => x == t1));
                }
                foreach (Link l in t.References)
                {
                    Thing t1 = l.T;
                    st.references.Add(new Point(KB.FindIndex(x => x == t1),l.weight));
                }
                KB1.Add(st);
            }
        }
        public override void SetUpAfterLoad()
        {
            base.SetUpAfterLoad();
            KB.Clear();
            foreach (SThing st in KB1)
            {
                Thing t = new Thing()
                {
                    Label = st.label,
                    V = st.V
                };
                KB.Add(t);
            }
            for (int i = 0; i < KB1.Count; i++)
            {
                foreach (int p in KB1[i].parents)
                {
                    KB[i].Parents.Add(KB[p]);
                }
                foreach (Point p in KB1[i].references)
                {
                    KB[i].References.Add(new Link { T = KB[(int)p.X],weight=(float)p.Y });
                }
            }

            //rebuild all the reverse linkages
            foreach (Thing t in KB)
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

        public override void Initialize()
        {
            //create an intial structure
            KB.Clear();
            KB1.Clear();
            AddThing("ROOT", new Thing[] { });
            AddThing("Color", new Thing[] { Labeled("ROOT") });
            AddThing("Red", new Thing[] { Labeled("Color") }, Colors.Red);
            AddThing("Green", new Thing[] { Labeled("Color") }, Colors.Green);
            AddThing("Blue", new Thing[] { Labeled("Color") }, Colors.Blue);
            AddThing("Shape", new Thing[] { Labeled("ROOT") });
            AddThing("Point", new Thing[] { Labeled("Shape") });
            AddThing("PossiblePoint", new Thing[] { Labeled("Point") });
            AddThing("Segment", new Thing[] { Labeled("Shape") });
            AddThing("Visible", new Thing[] { Labeled("ROOT") });
            AddThing("Partial", new Thing[] { Labeled("ROOT") });
            AddThing("Word", new Thing[] { Labeled("ROOT") });
        }
        public override void ShowDialog() //delete this function if it isn't needed
        {
            base.ShowDialog();
        }
    }


}
