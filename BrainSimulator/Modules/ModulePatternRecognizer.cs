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
    public class ModulePatternRecognizer : ModuleBase
    {

        const int maxPatterns = 6;
        public ModulePatternRecognizer()
        {
            minHeight = 3;
            minWidth = 3;
        }

        float targetWeightPos = 0;
        float targetWeightNeg = 0;
        int maxTries = 4;


        //fill this method in with code which will execute
        //once for each cycle of the engine

        int waitingForInput = 0;
        public override void Fire()
        {
            Init();  //be sure to leave this here

            //Do any hebbian synapse adjustment
            if (GetNeuron("Learning") is Neuron nlearing && nlearing.Fired())
            {
                for (int i = 0; i < mv.Height; i++)
                {
                    if (mv.GetNeuronAt(0, i) is Neuron n)
                    {
                        if (n.LastFired > MainWindow.theNeuronArray.Generation - 4)
                        {
                            waitingForInput = 0;
                            //adjust the incoming synapse weights
                            int firedCount = 0;
                            for (int j = 0; j < n.synapsesFrom.Count; j++)
                            {
                                Synapse s = n.synapsesFrom[j];
                                if (s.model != Synapse.modelType.Fixed)
                                {
                                    Neuron nSource = MainWindow.theNeuronArray.GetNeuron(s.TargetNeuron);
                                    if (nSource.LastFired > MainWindow.theNeuronArray.Generation - 4)
                                    {
                                        firedCount++;
                                    }
                                }
                            }
                            //if no inputs fired, things might not work so skip for now
                            if (firedCount == 0)
                                continue;


                            for (int j = 0; j < n.synapsesFrom.Count; j++)
                            {
                                Synapse s = n.synapsesFrom[j];
                                if (s.model != Synapse.modelType.Fixed)
                                {
                                    Neuron nSource = MainWindow.theNeuronArray.GetNeuron(s.TargetNeuron);
                                    if (nSource.LastFired > MainWindow.theNeuronArray.Generation - 4)
                                    {
                                        nSource.AddSynapse(n.id, (s.weight + targetWeightPos) / 2, s.model);
                                    }
                                    else
                                    {
                                        nSource.AddSynapse(n.id, (s.weight + targetWeightNeg) / 2, s.model);
                                    }
                                }
                            }

                            //clear partial charges from others
                            for (int k = 0; k < mv.Height; k++)
                            {
                                if (mv.GetNeuronAt(0, k) is Neuron n5)
                                {
                                    n5.currentCharge = 0;
                                    n5.Update();
                                }
                            }
                            break;
                        }
                    }
                }

                Neuron nRdOut = GetNeuron("RdOut");
                if ((MainWindow.theNeuronArray.Generation % 36) == 0)
                {
                    waitingForInput = maxTries; //countdown tries to read
                                         //fire rdOut
                    nRdOut.currentCharge = 1;
                    nRdOut.Update();
                }
                if (waitingForInput > 1 && (MainWindow.theNeuronArray.Generation % 6) == 0)
                {
                    waitingForInput--;
                    nRdOut.currentCharge = 1;
                    nRdOut.Update();
                }
                //if we get here, no neuron has fired yet
                if (waitingForInput == 1 && (MainWindow.theNeuronArray.Generation % 6) == 0)
                {
                    waitingForInput--;
                    //if we've tried enough, learn a new pattern
                    //1) select the neuron to use 2) fire it

                    long oldestFired = long.MaxValue;
                    Neuron oldestNeuron = null;
                    for (int i = 0; i < mv.Height; i++)
                    {
                        if (mv.GetNeuronAt(0, i) is Neuron n2)
                        {
                            if (n2.LastFired < oldestFired)
                            {
                                oldestFired = n2.LastFired;
                                oldestNeuron = n2;
                            }
                        }
                    }
                    if (oldestNeuron != null)
                    {
                        oldestNeuron.CurrentCharge = 1;
                        oldestNeuron.Update();
                        //zero out the old synapses
                        List<Synapse> prevSynapses = new List<Synapse>();
                        for (int j = 0; j < oldestNeuron.synapsesFrom.Count; j++)
                        {
                            prevSynapses.Add(oldestNeuron.synapsesFrom[j]);
                        }
                        oldestNeuron.DeleteAllSynapes(false);
                        for (int j = 0; j < prevSynapses.Count; j++)
                        {
                            float theWeight = prevSynapses[j].weight;
                            if (prevSynapses[j].model != Synapse.modelType.Fixed)
                                theWeight = 0;
                            theNeuronArray.GetNeuron(prevSynapses[j].targetNeuron).AddSynapse(
                                oldestNeuron.id, theWeight, prevSynapses[j].model);
                        }
                    }
                }
            }
            else //not learning
            {
                Neuron nRdOut = GetNeuron("RdOut");
                for (int i = 0; i < mv.Height; i++)
                {
                    if (mv.GetNeuronAt(0, i) is Neuron n)
                    {
                        if (n.LastFired > MainWindow.theNeuronArray.Generation - 4)
                        {
                            waitingForInput = 0;
                        }
                    }
                }

                            if ((MainWindow.theNeuronArray.Generation % 36) == 0)
                {
                    waitingForInput = maxTries; //countdown tries to read
                    nRdOut.currentCharge = 1;
                    nRdOut.Update();
                }
                if (waitingForInput > 1 && (MainWindow.theNeuronArray.Generation % 6) == 0)
                {
                    waitingForInput--;
                    nRdOut.currentCharge = 1;
                    nRdOut.Update();
                }
            }
        }

        private void GetTargetWeights(int firedCount)
        {
            //the target pos weight = reciprocal of the number of neurons firing so if they all fire
            //the output will fire on the first try
            targetWeightPos = .000001f + 1f / (float)(firedCount);
            //the target neg weight
            targetWeightNeg = -0.008f;

            int bestErrorCount = 0;
            string bestResult = "";
            for (float targetWeightNegTrial = 0; targetWeightNegTrial < .4f; targetWeightNegTrial += .0001f)
            {
                int w = 0;
                string result = "";
                int k;
                for (k = 0; k < 10; k++) //bit errors
                {
                    float charge = 0;
                    for (w = 0; w < 40; w++) //generations
                    {
                        charge += (firedCount - k) * targetWeightPos -
                            (k) * targetWeightNegTrial;
                        if (charge >= 1)
                        {
                            if (!result.Contains(":" + w.ToString()))
                            {
                                result += " " + k + ":" + w;
                                goto resultFound;
                            }
                            else
                            {
                                result += " " + k + ":" + w + "XX ";
                                goto duplicateFound;
                            }
                        }
                    }
                    //we never got a hit
                    if (k > bestErrorCount)// && !result.Contains("XX"))
                    {
                        bestErrorCount = k;
                        bestResult = targetWeightNegTrial + " : " + result;
                        targetWeightNeg = -targetWeightNegTrial;
                    }
                    break;
                resultFound:
                    if (k > bestErrorCount)// && !result.Contains("XX"))
                    {
                        bestErrorCount = k;
                        bestResult = targetWeightNeg + " : " + result;
                        targetWeightNeg = -targetWeightNegTrial;
                    }
                    continue;
                }
            duplicateFound:
                if (k > bestErrorCount)// && !result.Contains("XX"))
                {
                    bestErrorCount = k;
                    bestResult = targetWeightNeg + " : " + result;
                    targetWeightNeg = -targetWeightNegTrial;
                }
                continue;
            }
        }

        private void AddIncomingSynapses()
        {
            Neuron nLearning = mv.GetNeuronAt(2, 1);
            nLearning.Label = "Learning";
            nLearning.AddSynapse(nLearning.id, 1);
            Neuron nRdOut = mv.GetNeuronAt(2, 0);
            nRdOut.Label = "RdOut";
            for (int i = 0; i < mv.Height; i++)
            {
                Neuron n1 = mv.GetNeuronAt(0, i);
                if (i != 0)
                    n1.Clear();
                n1.Label = "P" + i;
            }
            if (GetNeuron("P0") is Neuron n)
            {
                foreach (Synapse s in n.SynapsesFrom)
                {
                    Neuron nSource = MainWindow.theNeuronArray.GetNeuron(s.TargetNeuron);
                    if (nSource.Label != "")
                    {
                        for (int j = 0; j < mv.Height; j++)
                        {
                            Neuron nTarget = mv.GetNeuronAt(0, j);
                            nSource.AddSynapse(nTarget.id, 0f, Synapse.modelType.Hebbian2);
                            MainWindow.theNeuronArray.GetNeuronLocation(nSource.id, out int col, out int row);
                            while (MainWindow.theNeuronArray.GetNeuron(col, ++row).Label != "")
                            {
                                MainWindow.theNeuronArray.GetNeuron(col, row).AddSynapse(nTarget.id, 0f, Synapse.modelType.Hebbian2);
                            }
                        }
                    }
                }
            }
            //add mutual suppression
            for (int i = 0; i < mv.Height; i++)
                for (int j = 0; j < mv.Height; j++)
                {
                    if (j != i)
                    {
                        Neuron nSource = mv.GetNeuronAt(0, i);
                        Neuron nTarget = mv.GetNeuronAt(0, j);
                        nSource.AddSynapse(nTarget.id, -1);
                    }
                }

            //add latch column
            for (int i = 0; i < mv.Height; i++)
            {
                Neuron nSource = mv.GetNeuronAt(0, i);
                Neuron nTarget = mv.GetNeuronAt(1, i);
                nSource.AddSynapse(nTarget.id, .9f);
                nRdOut.AddSynapse(nTarget.id, -1);                
            }

            if (GetNeuron("P0") is Neuron nP0)
                GetTargetWeights(nP0.synapsesFrom.Count(x => x.model == Synapse.modelType.Hebbian2) / 2);
            MainWindow.Update();
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return;
            AddIncomingSynapses();
        }


        public override void Initialize()
        {
            Init();
            AddIncomingSynapses();
        }
    }
}
