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
    public class ModuleAttention : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;

        //This module directs a ImageZoom module to intersting locations

        Random rand = new Random();
        int autoAttention = 0;

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleAttention()
        {
            minHeight = 2;
            maxHeight = 5;
            minWidth = 2;
            maxWidth = 5;
        }

        ModuleUKS uks = null;
        Thing currentAttention = null;

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            ModuleView naSource = theNeuronArray.FindModuleByLabel("UKS");
            if (naSource == null) return;
            uks = (ModuleUKS)naSource.TheModule;
            Thing mentalModel = uks.GetOrAddThing("MentalModel", "Visual");
            if (mentalModel.Children.Count == 0) return;


            if (autoAttention > 0)
            {
                autoAttention--;
                return;
            }
            if (autoAttention < 0) return;

            //if an object moved, it gets attention
            Thing currentMotionParent = uks.GetOrAddThing("Motion", "Visual");
            Thing newAttention = null;
            foreach (Thing t in currentMotionParent.Children)
            {
                if (t.Label.StartsWith("Moved"))
                {
                    newAttention = t.Children[0];
                    break;
                }
            }


            if (newAttention != null)
            {
                currentAttention = newAttention;
            }
            else
            {
                currentAttention = mentalModel.Children[rand.Next(mentalModel.Children.Count)];
                currentAttention.useCount++;
            }

            AddAtttention(currentAttention);

            foreach (Link l in currentAttention.References)
            {
                if (l is Relationship r) { }
                // r.relationshipType.SetFired();
                else
                    l.T.SetFired();
            }

        }

        private void AddAtttention(Thing t)
        {
            Thing attn = uks.GetOrAddThing("ATTN", "Thing");
            Thing parent = t.Parents[t.Parents.Count - 1];
            attn.RemveReferencesWithAncestor(uks.Labeled("Visual"));
            attn.AddReference(t);
            currentAttention = t;
            UpdateDialog();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            autoAttention = 0;
            currentAttention = null;
            if (uks != null)
            {
                Thing attn = uks.GetOrAddThing("ATTN", "Thing");
                uks.DeleteAllChilden(attn);
            }
        }

        public void SetAttention(Thing t,int delay = 10)
        {
            if (t == null)
            {
                if (autoAttention > -2)
                    autoAttention = 0;
                return;
            }
            if (autoAttention != -2)
                autoAttention = delay;
            AddAtttention(t);
            t.SetFired();
        }

        public void SetEnable(bool enable)
        {
            if (!enable) 
                autoAttention = -2;
            else if (autoAttention == -2) 
                autoAttention = 0;
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
