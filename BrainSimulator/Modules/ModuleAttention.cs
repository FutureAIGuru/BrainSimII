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


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleAttention()
        {
            minHeight = 2;
            maxHeight = 5;
            minWidth = 2;
            maxWidth = 5;
        }


        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            Point offset = new System.Windows.Point(rand.NextDouble(), rand.NextDouble());
            //SetNeuronValue("ImageZoom", "X", (float)offset.X);
            //SetNeuronValue("ImageZoom", "Y", (float)offset.Y);
            //SetNeuronValue("ImageZoom", "Scale", (float)rand.NextDouble());
            SetNeuronValue("ImageZoom", "X", 0);
            SetNeuronValue("ImageZoom", "Y", 0);
            SetNeuronValue("ImageZoom", "Scale", 0);
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
