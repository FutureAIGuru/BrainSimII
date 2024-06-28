//
// Copyright (c) Charles Simon. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;

namespace BrainSimulator.Modules
{
    public class ModuleBoundaryRelationship : ModuleBase
    {
        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleBoundaryRelationship()
        {
            minHeight = 2;
            maxHeight = 500;
            minWidth = 2;
            maxWidth = 500;
        }

        private ModuleUKS uks = null;
        private Thing prevAttnTarget = null;
        private Thing attnVisualTarget = null;

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            bool visualTargetChanged = false;
            Init();  //be sure to leave this here

            ModuleView naSource = theNeuronArray.FindModuleByLabel("UKS");
            if (naSource == null) return;
            uks = (ModuleUKS)naSource.TheModule;

            DeleteUnusedRelationships();

            Thing attn = uks.GetOrAddThing("ATTN", "Thing");
            Thing mentalModel = uks.GetOrAddThing("MentalModel", "Visual");

            if (attn.References.Count == 0) return;

            Thing newVisualTarget = attn.GetReferenceWithAncestor(mentalModel);
            if (newVisualTarget != attnVisualTarget)
            {
                visualTargetChanged = true;
            }
            if (visualTargetChanged)
            {
                prevAttnTarget = attnVisualTarget;
                attnVisualTarget = newVisualTarget;
            }

            if (prevAttnTarget != null && attnVisualTarget != null)
            {
                if (visualTargetChanged && mentalModel.Children.Count > 1)
                {
                    //add all the relationships to the uks
                    AddRelationshipsToUKS(prevAttnTarget, attnVisualTarget);
                }
            }

            UpdateDialog();
            return;
        }

        private void DeletePreviousRelationships(Thing t1, Thing t2)
        {
            t1.RemoveReference(t2);
            t2.RemoveReference(t1);
        }

        private void AddRelationshipsToUKS(Thing t1, Thing t2)
        {
            if (t1 == null || t2 == null) return;
            //DeletePreviousRelationships(t1, t2);
            var vals1 = uks.GetValues(t1);
            var vals2 = uks.GetValues(t2);
            foreach (var pair1 in vals1)
            {
                //if (!pair1.Key.Contains("Siz")) continue;
                var val2 = vals2[pair1.Key];
                //hue is special because it is an angle which wraps around so < & > are not really defined
                if (pair1.Key == "Hue+")
                {
                    Angle diff = (pair1.Value - val2) * 2 * Math.PI;
                    diff = (diff.ToDegrees() + 180) % 360 - 180;
                    diff = Angle.FromDegrees(diff);
                    if (Math.Abs(diff) < Angle.FromDegrees(10))
                        t1.AddRelationship(t2, uks.GetOrAddThing("=" + pair1.Key, "Relationship"));
                    else
                        t1.AddRelationship(t1, uks.GetOrAddThing("!" + pair1.Key, "Relationship"));
                }
                else if (pair1.Key.EndsWith("+"))
                {
                    //value comparison
                    if (Math.Abs(pair1.Value - val2) == 0) //TODO make this tolerance a parameter
                        t1.AddRelationship(t2, uks.GetOrAddThing("=" + pair1.Key, "Relationship"));
                    else if (pair1.Value > val2)
                        t1.AddRelationship(t2, uks.GetOrAddThing("<" + pair1.Key, "Relationship"));
                    else
                        t1.AddRelationship(t2, uks.GetOrAddThing(">" + pair1.Key, "Relationship"));
                }
                else
                {
                    //equality comparison
                    if (pair1.Value == val2)
                        t1.AddRelationship(t2, uks.GetOrAddThing("=" + pair1.Key.Replace("Saved", ""), "Relationship"));
                    else
                        t1.AddRelationship(t2, uks.GetOrAddThing("!" + pair1.Key.Replace("Saved", ""), "Relationship"));
                }
            }
        }

        private void DeleteUnusedRelationships()
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
            prevAttnTarget = null;
            attnVisualTarget = null;
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
            if (mv == null) return; //this is called the first time before the module actually exists
        }
    }
}
