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
            minHeight = 3;
            minWidth = 3;
        }
        public override string ShortDescription => "Decodes a set of colors from multi-leveled rgb input.";
        public override string LongDescription => "Add synapses to from various input sources to 'P0'. The system will automatically add "+
            "input synapses from all labeled neurons below the input synapses added.";


        int waitingForInput = 0;
        public override void Fire()
        {
            Init();  //be sure to leave this here

            //Do any hebbian synapse adjustment

            if ((MainWindow.theNeuronArray.Generation % 32) == 0)
            {
                waitingForInput = 4; //countdown tries to read
            }
            if (waitingForInput != 0 && (MainWindow.theNeuronArray.Generation % 4) == 0)
            {
                waitingForInput--;
                if (!(GetNeuron("RdOut") is Neuron n1))
                    return;
                n1.CurrentCharge = 1;
                n1.Update();
                for (int i = 0; i < na.Height; i++)
                {
                    Neuron n = na.GetNeuronAt(0, i);
                    if (n.LastFired > MainWindow.theNeuronArray.Generation - 4)
                    {
                        waitingForInput = 0;
                        //adjust the incoming synapse weights
                        int firedCount = 0;
                        for (int j = 0; j < n.synapsesFrom.Count; j++)
                        {
                            Synapse s = n.synapsesFrom[j];
                            Neuron nSource = MainWindow.theNeuronArray.GetNeuron(s.TargetNeuron);
                            if (nSource.LastFired > MainWindow.theNeuronArray.Generation - 4)
                            {
                                firedCount++;
                            }
                        }
                        if (firedCount == 0) continue;
                        //the target pos weight = 1/the number of neurons firing so if they all fire
                        //the output will fire on the first try
                        float targetWeightPos = 1 / (float)firedCount;
                        //the target neg weight = 
                        float targetWeightNeg = -0.167f - targetWeightPos;
                        for (int j = 0; j < n.synapsesFrom.Count; j++)
                        {
                            Synapse s = n.synapsesFrom[j];
                            Neuron nSource = MainWindow.theNeuronArray.GetNeuron(s.TargetNeuron);
                            if (nSource.LastFired > MainWindow.theNeuronArray.Generation - 4)
                            {
                                nSource.AddSynapse(n.id, (s.weight + targetWeightPos) / 2);
                            }
                            else
                            {
                                nSource.AddSynapse(n.id, (s.weight + targetWeightNeg) / 2);
                            }
                        }

                        goto NeuronFired;
                    }
                }
                //if we get here, no neuron has fired yet
                if (waitingForInput == 0)
                {
                    //if we've tried enough, learn a new pattern
                    //1) select the neuron to use 2) fire it
                    //is there an unused neuron?
                    //decide what to forget
                    //fire the selected neuron
                    if (na.GetNeuronAt(0, 0) is Neuron n2)
                    {
                        n2.CurrentCharge = 1;
                        n2.Update();
                        n1.CurrentCharge = 1;
                        n1.Update();
                    }
                }
            NeuronFired:
                {
                }
            }
        }

        private void AddIncomingSynapses()
        {
            na.GetNeuronAt(1, 0).Label = "NewData";
            na.GetNeuronAt(2, 0).Label = "RdOut";
            for (int i = 0; i < na.Height; i++)
            {
                Neuron n1 = na.GetNeuronAt(0, i);
                if (i != 0)
                    n1.Clear();
                n1.Label = "P" + i;
            }
            if (GetNeuron("P0") is Neuron n)
            {
                foreach (Synapse s in n.SynapsesFrom)
                {
                    Neuron nSource = MainWindow.theNeuronArray.GetNeuron(s.TargetNeuron);
                    for (int j = 0; j < na.Height; j++)
                    {
                        Neuron nTarget = na.GetNeuronAt(0, j);
                        nSource.AddSynapse(nTarget.id, 0f);
                        MainWindow.theNeuronArray.GetNeuronLocation(nSource.id, out int col, out int row);
                        while (MainWindow.theNeuronArray.GetNeuron(col, ++row).Label != "")
                        {
                            MainWindow.theNeuronArray.GetNeuron(col, row).AddSynapse(nTarget.id, 0f);
                        }
                    }
                }
            }
            MainWindow.Update();
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (na == null) return;
            AddIncomingSynapses();
        }


        public override void Initialize()
        {
            Init();
            AddIncomingSynapses();
        }
    }
}
