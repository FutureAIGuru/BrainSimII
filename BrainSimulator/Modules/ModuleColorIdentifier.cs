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
    public class ModuleColorIdentifier : ModuleBase
    {

        //fill this method in with code which will execute
        //once for each cycle of the engine

        const int maxPatterns = 6;
        public ModuleColorIdentifier()
        {
            minHeight = maxPatterns;
            minWidth = 3;
        }
        public override string ShortDescription => "Decodes a set of colors from multi-leveled rgb input.";
        public override string LongDescription => "Looks for neurons labeled 'R0-7', 'G0-7', & 'B0-7'. Builds a list of most commonly-seen colors.";
        public override void Fire()
        {
            Init();  //be sure to leave this here

            if ((MainWindow.theNeuronArray.Generation % 10) == 0)
            {
                Neuron n = MainWindow.theNeuronArray.GetNeuron("READ");
                if (n != null)
                {
                    n.SetValue(1);
                }
            }

            int maxLevels = 20;
            int r = -1;
            int g = -1;
            int b = -1;
            for (int i = 0; i < maxLevels; i++)
            {
                Neuron n = MainWindow.theNeuronArray.GetNeuron("B" + i);
                if (n != null && n.Fired())
                {
                    b = i;
                    break;
                }
                else if (n == null) break;
            }
            for (int i = 0; i < maxLevels; i++)
            {
                Neuron n = MainWindow.theNeuronArray.GetNeuron("G" + i);
                if (n != null && n.Fired())
                {
                    g = i;
                    break;
                }
                else if (n == null) break;
            }
            for (int i = 0; i < maxLevels; i++)
            {
                Neuron n = MainWindow.theNeuronArray.GetNeuron("R" + i);
                if (n != null && n.Fired())
                {
                    r = i;
                    break;
                }
                else if (n == null) break;
            }

            //rgb represents the input pattern...do the matching
            if (b != -1 && g != -1 && r != -1)
            {
                int pattern = FindPattern(new int[] { r, g, b });
                if (pattern != -1)
                {
                    na.GetNeuronAt(pattern).SetValue(1);
                }
                else
                {
                    AddPattern(new int[] { r, g, b });
                }

            }
        }

        List<int[]> storedPatterns = new List<int[]>();
        List<int> counts = new List<int>();
        private int FindPattern(int[] values)
        {
            int retVal = -1;

            for (int i = 0; i < storedPatterns.Count; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    if (Math.Abs(values[j] - storedPatterns[i][j]) > 1) goto misMatch;
                }
                counts[i]++;
                return i;
            misMatch: continue;
            }
            return retVal;
        }
        private int AddPattern(int[] values)
        {
            //TODO add intensity
            //TODO is color between existing colors?
            //TODO recognize multiple ratios of the same components as the same color
            int retVal = -1;
            if (storedPatterns.Count < maxPatterns)
            {
                storedPatterns.Add(values);
                counts.Add(0);
            }
            else
            {
                //find the lowest of count
                int minIndex = Array.IndexOf(counts.ToArray(), counts.Min());
                counts[minIndex] = 0;
                storedPatterns[minIndex] = values;
            }
            return retVal;
        }

        public override void Initialize()
        {
            for (int i = 0; i < maxPatterns; i++)
            {
                AddLabel("P" + i);
            }
        }
    }
}
