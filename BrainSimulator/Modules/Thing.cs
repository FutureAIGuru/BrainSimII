//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BrainSimulator
{
    //a link is a weighted link to a thing
    public class Link
    {
        public Thing T;
        public float weight = 1;
    }

    //a thing is anything, physical object, attribute, word, action, etc.
    public class Thing
    {
        private string label = ""; //this is just for convenience in debugging 
        private List<Thing> parents = new List<Thing>(); //"is-a", others?
        private List<Thing> children = new List<Thing>(); //synapses to
        private List<Link> references = new List<Link>(); //synapses to "has", "is", others
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
            { AddReference(t, incr); }
            else { existingLink.weight += incr; }
        }

        public void RemoveReference(Thing t)
        {
            References.RemoveAll(v => v.T == t);
            t.ReferencedBy.RemoveAll(v => v.T == this);
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

    //this is a modification of Thing which is used to store and retrieve the KB in XML
    //it eliminates circular references by replacing Thing references with int indexed into an array and makes things much more compact
    public class SThing
    {
        public string label = ""; //this is just for convenience in debugging and should not be used
        public List<int> parents = new List<int>(); //"is-a"?
        public List<Point> references = new List<Point>(); //we're casting this as a point because it is small in xml
        //x is the index of the link, y is the confidence
        object value;
        public object V { get => value; set => this.value = value; }
    }
}
