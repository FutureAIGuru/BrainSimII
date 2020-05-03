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
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleKBDebug : ModuleBase
    {
        [XmlIgnore]
        public List<string> history = new List<string>();

        int maxLength = 100;
        //display history of KB
        public override void Fire()
        {
            Init();  //be sure to leave this here
            string tempString = "";
            ModuleView naKB = theNeuronArray.FindAreaByLabel("ModuleUKS");
            if (naKB == null) return;
            for (int i = 0; i < naKB.NeuronCount; i++)
            {
                Neuron n = naKB.GetNeuronAt(i);
                if (n.Fired())
                {
                    tempString += " " + n.Label;
                }
            }
            if (tempString != "" && tempString != history.LastOrDefault()) 
            {
                lock (history)
                {
                    history.Add(tempString);
                }
            }
            tempString = ">>>>";
            ModuleView naKBOut = theNeuronArray.FindAreaByLabel("KBOut");
            if (naKBOut == null) return;
            for (int i = 0; i < naKBOut.NeuronCount; i++)
            {
                Neuron n = naKBOut.GetNeuronAt(i);
                if (n.Fired())
                {
                    tempString += " " + n.Label;
                }
            }
            if (tempString != ">>>>")
            {
                lock (history)
                {
                    history.Add(tempString);
                }
            }
            lock (history)
            {
                while (history.Count > maxLength) history.RemoveAt(0);
            }
            UpdateDialog();
        }

        public override void Initialize()
        {
            history.Clear();
        }
    }
}
