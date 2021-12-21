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
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleBoundaryColor : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleBoundaryColor()
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

            Thing areaParent = uks.GetOrAddThing("CurrentlyVisible", "Visual");
            Thing colorParent = uks.GetOrAddThing("Color", "Visual");

            foreach (Thing area in areaParent.Children)
            {
                var values = uks.GetValues(area);
                float hue = values["Hue+"];
                float sat = values["Sat+"];
                float lum = values["Lum+"];

                Thing theColor = null;
                foreach(Thing color in colorParent.Children)
                {
                    var valuesc = uks.GetValues(color);
                    float huec = valuesc["Hue+"];
                    float satc = valuesc["Sat+"];
                    float lumc = valuesc["Lum+"];

                    //TODO add better match algorithm
                    if (huec == hue && Abs(satc - sat)< 0.2f && Abs(lumc - lum) < 0.2f)
                    {
                        theColor = color;
                        break;
                    }
                }
                if (theColor == null)
                {
                    theColor = uks.AddThing("c*", colorParent);
                    uks.SetValue(theColor, hue, "Hue",2);
                    uks.SetValue(theColor, sat, "Sat",2);
                    uks.SetValue(theColor, lum, "Lum",2);
                }
                area.AddReference(theColor);
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
            if (mv == null) return; //this is called the first time before the module actually exists
        }
    }
}
