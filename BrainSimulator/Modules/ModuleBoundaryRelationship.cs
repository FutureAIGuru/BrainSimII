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
    public class ModuleBoundaryRelationship : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleBoundaryRelationship()
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

            DeleteUnusedRelationships();

            foreach (Thing t1 in uks.Labeled("VisibleArea").Children)
            {
                var vals1 = uks.GetValues(t1);
                foreach (Thing t2 in uks.Labeled("VisibleArea").Children)
                {
                    if (t1 == t2) continue;

                    var vals2 = uks.GetValues(t2);
                    foreach (var pair1 in vals1)
                    {
                        var val2 = vals2[pair1.Key];
                        //hue is special because is an angle which wraps around so <&> not really defined
                        if (pair1.Key == "Hue+")
                        {
                            Angle diff = pair1.Value - val2;
                            diff = (diff.ToDegrees() + 180) % 360 - 180;
                            diff = diff.FromDegrees(diff);
                            if (Math.Abs(diff) < 10)
                                uks.AddRelationship(t1, t2, "=" + pair1.Key);
                            else
                                uks.AddRelationship(t1, t2, "!" + pair1.Key);
                        }
                        else if (pair1.Key.EndsWith("+"))
                        {
                            //TODO add tolerances
                            //value comparison
                            if (pair1.Value == val2)
                                uks.AddRelationship(t1, t2, "=" + pair1.Key);
                            else if (pair1.Value > val2)
                                uks.AddRelationship(t1, t2, ">" + pair1.Key);
                            else
                                uks.AddRelationship(t1, t2, "<" + pair1.Key);
                        }
                        else
                        {
                            //equality comparison
                            if (pair1.Value == val2)
                                uks.AddRelationship(t1, t2, "=" + pair1.Key);
                            else
                                uks.AddRelationship(t1, t2, "!" + pair1.Key);
                        }
                    }
                }
            }

            //if you want the dlg to update, use the following code whenever any parameter changes
            // UpdateDialog();
        }

        void DeleteUnusedRelationships()
        {
            Thing relationshipParent = uks.GetOrAddThing("Relationship", "Thing");
            for (int i = 0; i < relationshipParent.Children.Count; i++)
            {
                Thing relationshipType = relationshipParent.Children[i];
                for (int j = 0; j < relationshipType.Children.Count; j++)
                {
                    Thing child = relationshipType.Children[j];
                    if (child.References.Count != 1 || child.ReferencedBy.Count != 1)
                    {
                        uks.DeleteThing(child);
                        j--;
                    }
                }
            }
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
