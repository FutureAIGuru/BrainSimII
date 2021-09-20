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
    public class ModuleBoundaryKnown : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleBoundaryKnown()
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

            Thing knownObjectParent = uks.GetOrAddThing("KnownObject", "Visual");
            Thing currentlyVisibleParent = uks.GetOrAddThing("CurrentlyVisible", "Visual");

            //List<Thing> visibleKnownThings = new List<Thing>();
            foreach (Thing area in currentlyVisibleParent.Children)
            {
                Thing knownObject1 = null;
                foreach(Thing knownObject in knownObjectParent.Children)
                {
                    Thing knownObjectShape = knownObject.HasReferenceWithParent(uks.Labeled("Area"));
                    Thing areaShape = area.HasReferenceWithParent(uks.Labeled("Area"));
                    Thing knownObjectColor = knownObject.HasReferenceWithParent(uks.Labeled("Color"));
                    Thing areaColor = area.HasReferenceWithParent(uks.Labeled("Color"));
                    //if critical properties match, object is already known
                    if (knownObjectShape == areaShape && knownObjectColor == areaColor)
                    {
                        knownObject1 = knownObject;
                        break;
                    }
                }

                if (knownObject1 == null)
                {
                    //The object is unknown, add it to known objects for future reference
                    knownObject1= area.Clone();
                    knownObject1.AddParent(knownObjectParent);
                    knownObject1.Label = "KnownShape*";
                }
                //visibleKnownThings.Add(knownObject1);
                uks.DeleteAllChilden(area);
                area.AddChild(knownObject1);
                //knownObject1.Label = area.Label;
            }

            //if you want the dlg to update, use the following code whenever any parameter changes
            // UpdateDialog();
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
