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
using System.Diagnostics;

namespace BrainSimulator.Modules
{
    public class ModuleBoundaryMotion : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleBoundaryMotion()
        {
            minHeight = 2;
            maxHeight = 500;
            minWidth = 2;
            maxWidth = 500;
        }


        //fill this method in with code which will execute
        //once for each cycle of the engine
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
            Thing currentlyVisibleParent = uks.GetOrAddThing("CurrentlyVisible", "Visual");
            Thing motionParent = uks.GetOrAddThing("Motion", "Visual");



            //Things which disappeared in the last cycle need special treatment to remove from mental model & relationships but not the known object
            foreach (Thing t in motionParent.Children)
            {
                if (t.Label.Contains("Disappeared"))
                {
                    Thing thingToRemove = t.Children[0];
                    for (int i = thingToRemove.Children.Count - 1; i >= 0; i--)
                    {
                        Thing child = thingToRemove.Children[i];
                        thingToRemove.RemoveChild(child);
                    }
                    uks.DeleteThing(thingToRemove);
                }
            }

            uks.DeleteAllChilden(motionParent);

            //build little lists of things which moved or appeared so we can also be left with things which things which disappeared
            List<Thing> matchedThings = new List<Thing>();
            List<Thing> movedThings = new List<Thing>();
            List<Thing> movedThingsNew = new List<Thing>();
            List<Thing> newThings = new List<Thing>();
            foreach (Thing visibleArea in currentlyVisibleParent.Children)
            {
                Thing matchedThing = null;
                foreach (Thing storedArea in mentalModel.Children)
                {
                    Debug.Assert(visibleArea.Children.Count == 1);
                    if (storedArea.Children.Count != 1) continue;
                    if (storedArea.Children[0] == visibleArea.Children[0]) //are these instances of the same object type?
                    {
                        var values = uks.GetValues(visibleArea);
                        var prevValues = uks.GetValues(storedArea);
                        Point c1 = new Point(uks.GetValue(visibleArea, "CtrX+"), uks.GetValue(visibleArea, "CtrY+"));
                        Point c2 = new Point(uks.GetValue(storedArea, "CtrX+"), uks.GetValue(storedArea, "CtrY+"));
                        float dist = (float)(c1 - c2).Length;
                        if (dist < (uks.GetValue(visibleArea, "Siz+") + uks.GetValue(storedArea, "Siz+")) / 2)
                        {
                            //these are probably the same object...is motion detected?
                            var changes = uks.GetChangedValues(visibleArea, storedArea);
                            if (changes.ContainsKey("Siz+") ||
                                changes.ContainsKey("CtrX+") ||
                                changes.ContainsKey("CtrY+") ||
                                changes.ContainsKey("Ang+"))
                            {
                                Thing t1 = uks.AddThing("Moved", motionParent);
                                t1.AddChild(storedArea);
                                foreach (var change in changes)
                                {
                                    uks.SetValue(t1, change.Value, change.Key, 0);
                                }
                                movedThings.Add(storedArea);
                                movedThingsNew.Add(visibleArea);
                            }
                            matchedThing = storedArea;
                            goto objectMatch;
                        }
                    }
                }
                //there is no matching object, this just appeared!
                newThings.Add(visibleArea);
                Thing m = uks.AddThing("Appeared*", motionParent);
                m.AddChild(visibleArea);
                continue;

            objectMatch:;
                if (matchedThing != null)
                {
                    matchedThings.Add(matchedThing);
                }
            }


            for (int i = mentalModel.Children.Count - 1; i >= 0; i--)
            {
                Thing t = mentalModel.Children[i];
                if (!matchedThings.Contains(t))
                {
                    Thing m = uks.AddThing("Disappeared*", motionParent);
                    m.AddChild(t);
                    mentalModel.RemoveChild(t);
                    Thing attn = uks.Labeled("ATTN");
                    if (attn != null)
                    {
                        attn.RemoveReference(t);
                    }
                }
            }

            if (motionParent.Children.Count > 1)
            {
                //analyze motion
                //if only a few objects moved, they moved
                //if all objects moved in same direction, it's becaue POV turned
                //if all objects moved in directions radiating to or from a point, POV is moving toward or away from that point.
                //TODO if all objects moved in same direction but varying magnitudes, POV has moved sideways...further objects move less

                //calculate the motion vector for all associated objects
                List<Segment> motions = new List<Segment>();
                foreach (Thing t in motionParent.Children)
                {
                    if (t.Label.StartsWith("Moved")) //ignore appeared/disappeared
                    {
                        var values = uks.GetValues(t);
                        PointPlus p = new PointPlus(0, (float)0);
                        if (values.ContainsKey("CtrX++"))
                            p.X = values["CtrX++"];
                        if (values.ContainsKey("CtrY++"))
                            p.Y = values["CtrY++"];
                        Segment s = new Segment();
                        s.P1 = p; //This is a relative motion value
                        values = uks.GetValues(t.Children[0]);
                        p = new PointPlus(0, (float)0);
                        if (values.ContainsKey("CtrX+"))
                            p.X = values["CtrX+"];
                        if (values.ContainsKey("CtrY+"))
                            p.Y = values["CtrY+"];
                        s.P2 = p; //This is an absolute position
                        motions.Add(s);
                    }
                }

                if (motions.Count > 1)
                {
                    //do most move the same?  ANGULAR POV MOTION
                    //TODO: use clustering to get most likely motions
                    PointPlus pMotion = new PointPlus(motions[0].P1);

                    int count = 0;
                    foreach (Segment s in motions)
                    {
                        if (s.P1.Near(pMotion, .1f))
                            count++;
                    }
                    if (count > motions.Count - 2) //only two objects can be exceptions
                    {
                        for (int i = 0; i < motions.Count; i++)
                        {
                            if (motions[i].P1.Near(pMotion, .1f))
                            {
                                motions.RemoveAt(i);
                                Thing t = motionParent.Children[i];
                                uks.DeleteAllChilden(t);
                                uks.DeleteThing(t);
                                i--;
                            }
                            else
                            {
                                //an object has its own motion too...subtract off the POV motion
                                Thing t = motionParent.Children[i];
                                float x = uks.GetValue(t, "CtrX+");
                                float y = uks.GetValue(t, "CtrY+");
                                x -= motions[i].P1.X;
                                y -= motions[i].P1.Y;
                                uks.SetValue(t, x, "CtrX", 3);
                                uks.SetValue(t, y, "CtrY", 3);
                            }
                        }
                        Thing POVMotion = uks.AddThing("POVMotion*", "Motion");
                        uks.SetValue(POVMotion, pMotion.X, "CtrX", 3);
                        uks.SetValue(POVMotion, pMotion.Y, "CtrY", 3);
                    }
                    else
                    { //this is not a rotation...is it a motion toward/away?
                      //TODO there are no exceptions allowed
                        foreach (var m in motions)
                        {
                            m.P1 += m.P2;
                        }
                        List<PointPlus> intersections = new List<PointPlus>();
                        for (int i = 0; i < motions.Count; i++)
                            for (int j = i + 1; j < motions.Count; j++)
                            {
                                Utils.FindIntersection(motions[i].P1, motions[i].P2, motions[j].P1, motions[j].P2, out Point intersection);
                                intersections.Add(intersection);
                            }

                        //testing clustering...
                        //TODO try all this with points of interest rather than centers
                        double[][] rawData = new double[intersections.Count][];
                        for (int i = 0; i < intersections.Count; i++)
                                rawData[i] = new double[2] { intersections[i].X, intersections[i].Y };
                        int[] clusters = KMeansClustering.Cluster(rawData, 2);
                        //now ask if there is a single cluster with most of the data

                        PointPlus aveIntersection = new PointPlus();
                        for (int i = 0; i < intersections.Count; i++)
                        {
                            aveIntersection.X += intersections[i].X;
                            aveIntersection.Y += intersections[i].Y;
                        }
                        aveIntersection.X /= intersections.Count;
                        aveIntersection.Y /= intersections.Count;
                        float dist = 0;
                        float dir = 0;
                        for (int i = 0; i < intersections.Count; i++)
                        {
                            dist += (intersections[i] - aveIntersection).R;
                            //moving toward or away?
                            dir += (motions[i].P1 - intersections[i]).R - (motions[i].P2 - intersections[i]).R;
                        }
                        dist /= intersections.Count;
                        dir /= intersections.Count;
                        if (dist < 10) // ???
                        {
                            uks.DeleteAllChilden(motionParent);
                            Thing POVMotion = uks.AddThing("POVMotion*", "Motion");
                            uks.SetValue(POVMotion, aveIntersection.X, "CtrX", 3);
                            uks.SetValue(POVMotion, aveIntersection.Y, "CtrY", 3);
                            uks.SetValue(POVMotion, dir, "CtrZ", 3);
                        }
                    }
                }
            }

            //transfer currently visible objects to previously visible 
            Debug.Assert(movedThings.Count == movedThingsNew.Count);
            for (int i = 0; i < movedThings.Count; i++)
            {
                uks.SetValue(movedThings[i], uks.GetValue(movedThingsNew[i], "CtrX+"), "CtrX", 0);
                uks.SetValue(movedThings[i], uks.GetValue(movedThingsNew[i], "CtrY+"), "CtrY", 0);
                uks.SetValue(movedThings[i], uks.GetValue(movedThingsNew[i], "Siz+"), "Siz", 0);
                uks.SetValue(movedThings[i], uks.GetValue(movedThingsNew[i], "Ang+"), "Ang", 0);
            }

            foreach (var t in newThings)
            {
                t.AddParent(mentalModel); //TODO, this may leave multiple things in the mental model with the same label
            }

            //This hack puts predictable labels on things in the mental model so we can select them more easily in the description
            //Area0-n from top->bottom, left->right
            List<sortable> vals = new List<sortable>();
            foreach (Thing storedArea in mentalModel.Children)
            {
                var values = uks.GetValues(storedArea);
                vals.Add(new sortable { t = storedArea, x = values["CtrX+"],y=values["CtrY+"], });
            }
            vals = vals.OrderBy(w => w.y * 1000 + w.x).ToList();
            for (int i = 0; i < vals.Count; i++)
            {
                vals[i].t.Label = "Area" + i;
            }

            //for debugging:
            //if there is a new image, cancel the existing relationships
            ModuleImageFile mif = (ModuleImageFile)FindModuleByName("ImageFile");
            if (mif == null) return;
            string curPath = mif.GetFilePath();
            if (curPath != prevPath)
            {
                foreach (Thing t in mentalModel.Children)
                {
                    var refs = t.GetRelationshipsByType(null,null);
                    foreach (Link ref1 in refs)
                        t.RemoveReference(ref1.T);
                }
                prevPath = curPath;
            }

            //if you want the dlg to update, use the following code whenever any parameter changes
            // UpdateDialog();
        }
        public class sortable { public Thing t; public float x, y; }
        private string prevPath = "";

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
