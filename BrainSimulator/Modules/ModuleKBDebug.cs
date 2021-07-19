//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Collections.Generic;
using System.Linq;
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
            ModuleView naKB = theNeuronArray.FindModuleByLabel("Module2DKB");
            if (naKB == null) return;
            for (int x = 0; x < naKB.Width; x+= 2)
                for (int y = 0; y < naKB.Height; y++)
                {
                    Neuron n = naKB.GetNeuronAt(x,y);
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
            for (int x = 1; x < naKB.Width; x += 2)
                for (int y = 0; y < naKB.Height; y++)
                {
                    Neuron n = naKB.GetNeuronAt(x, y);
                    if (n.Fired())
                    {
                        Neuron n1 = naKB.GetNeuronAt(x-1, y);
                        tempString += " " + n1.Label;
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
