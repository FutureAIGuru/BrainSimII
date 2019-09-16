
//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public partial class NeuronArrayView
    {
        public void ClearSelection()
        {
            theSelection.selectedRectangles.Clear();
            targetNeuronIndex = -1;
            Update();
        }

        //first selected neron must be the upper left of the overall selection area
        public void CopyNeurons()
        {
            Dictionary<int, int> oldNew = new Dictionary<int, int>();
            int index = 0;

            //build a mapping table so we can change target neurons in synapses
            theSelection.EnumSelectedNeurons();
            for (Neuron n = theSelection.GetSelectedNeuron(); n != null; n = theSelection.GetSelectedNeuron())
            {
                oldNew.Add(theSelection.selectedNeuronIndex, index);
                index++;
            }
            index = 0;

            theSelection.GetSelectedBoundingRectangle(out int X1o, out int Y1o, out int X2o, out int Y2o);
            myClipBoard = new NeuronArray((X2o - X1o+1) * (Y2o - Y1o+1), (Y2o - Y1o+1));
            theSelection.EnumSelectedNeurons();
            for (Neuron n = theSelection.GetSelectedNeuron(); n != null; n = theSelection.GetSelectedNeuron())
            {
                myClipBoard.neuronArray[index].CurrentCharge = n.CurrentCharge;
                myClipBoard.neuronArray[index].LastCharge = n.LastCharge;
                myClipBoard.neuronArray[index].Label = n.Label;
                myClipBoard.neuronArray[index].Model = n.Model;
                foreach (Synapse s in n.synapses)
                {
                    int newSynapse = -1;
                    if (oldNew.TryGetValue(s.TargetNeuron, out newSynapse))
                        myClipBoard.neuronArray[index].AddSynapse(newSynapse, s.Weight, myClipBoard);
                }
                index++;
            }
        }
        public void CutNeurons()
        {
            CopyNeurons();
            DeleteNeurons();
        }
        public void PasteNeurons(bool pasteSynapses = true)
        {
            if (targetNeuronIndex == -1) return;
            if (myClipBoard == null) return;
            //We are copying neuron values from one array to another.  
            //The arrays may have different sizes so we consider the source to be linear and handle the destination with an offset for each col.
            for (int i = 0; i < myClipBoard.arraySize; i++)
            {
                int neuron = MapNeuron(targetNeuronIndex, i);
                MainWindow.theNeuronArray.neuronArray[neuron].CurrentCharge =
                        myClipBoard.neuronArray[i].CurrentCharge;
                MainWindow.theNeuronArray.neuronArray[neuron].LastCharge =
                        myClipBoard.neuronArray[i].LastCharge;
                MainWindow.theNeuronArray.neuronArray[neuron].Label =
                        myClipBoard.neuronArray[i].Label;
                if (pasteSynapses)
                {
                    foreach (Synapse s in myClipBoard.neuronArray[i].synapses)
                    {
                        int synapseTarget = MapNeuron(targetNeuronIndex, s.TargetNeuron);
                        MainWindow.theNeuronArray.neuronArray[neuron].AddSynapse(synapseTarget, s.Weight, MainWindow.theNeuronArray);
                    }
                }
            }
            Update();
        }

        public void ConnectFromHere()
        {
            if (targetNeuronIndex == -1) return;
            //We are copying neuron values from one array to another.  
            //The arrays may have different sizes so we consider the source to be linear and handle the destination with an offset for each col.
            Neuron targetNeuron = MainWindow.theNeuronArray.neuronArray[targetNeuronIndex];
            theSelection.EnumSelectedNeurons();
            for (Neuron n = theSelection.GetSelectedNeuron(); n != null; n = theSelection.GetSelectedNeuron())
            {
                targetNeuron.AddSynapse(theSelection.selectedNeuronIndex, lastSynapseWeight, MainWindow.theNeuronArray);
            }
            Update();
        }
        public void ConnectToHere()
        {
            if (targetNeuronIndex == -1) return;
            //We are copying neuron values from one array to another.  
            //The arrays may have different sizes so we consider the source to be linear and handle the destination with an offset for each col.
            theSelection.EnumSelectedNeurons();
            for (Neuron n = theSelection.GetSelectedNeuron(); n != null; n = theSelection.GetSelectedNeuron())
            {
                n.AddSynapse(targetNeuronIndex, lastSynapseWeight, MainWindow.theNeuronArray);
            }
            Update();
        }
        private int MapNeuron(int firstTargetNeuron, int i)
        {
            return firstTargetNeuron + i % myClipBoard.rows + (int)(i / myClipBoard.rows) * MainWindow.theNeuronArray.rows;
        }

        public void DeleteNeurons(bool deleteSynapses = true)
        {
            theSelection.EnumSelectedNeurons();
            for (Neuron n = theSelection.GetSelectedNeuron(); n != null; n = theSelection.GetSelectedNeuron())
            {
                n.CurrentCharge = 0;
                n.LastCharge = 0;
                n.Model = Neuron.modelType.Std;
                if (deleteSynapses)
                    n.DeleteAllSynapes();
                n.Label = "";
            }
            Update();
        }

        //move the neurons from the selected area to the second (start point) and stretch all the synapses
        //!!!will fail if the source and destination areas overlap!!!
        public void MoveNeurons()
        {
            if (theSelection.selectedNeuronIndex == -1) return;
            theSelection.EnumSelectedNeurons();
            for (Neuron n = theSelection.GetSelectedNeuron(); n != null; n = theSelection.GetSelectedNeuron())
            {
                int offset = targetNeuronIndex - theSelection.selectedRectangles[0].FirstSelectedNeuron;
                int neuronNewLocation = theSelection.selectedNeuronIndex + offset;

                Neuron nNewLocation = MainWindow.theNeuronArray.neuronArray[neuronNewLocation];

                //copy the neuron attributes and delete them from the old neuron.
                nNewLocation.Label = n.Label;
                n.Label = "";
                nNewLocation.CurrentCharge = n.CurrentCharge;
                nNewLocation.LastCharge = n.LastCharge;
                n.SetValue(0);

                //for all the synapses going into or out of the selection area, stretch
                //for all the synapses entirely within the selection areay, move
                //don't use a foreach here because the body of the loop may delete a list entry
                for (int k = 0; k < n.synapses.Count; k++)
                {
                    Synapse s = n.Synapses[k];

                    if (theSelection.NeuronInSelection(s.TargetNeuron) > 0)
                    { //source & target are in selection
                        nNewLocation.AddSynapse(s.TargetNeuron + offset, s.Weight, MainWindow.theNeuronArray);
                        n.DeleteSynapse(s.TargetNeuron);
                        k--;
                    }
                    else
                    { //source is in selection
                        nNewLocation.AddSynapse(s.TargetNeuron, s.Weight, MainWindow.theNeuronArray);
                        n.DeleteSynapse(s.TargetNeuron);
                        k--;
                    }
                }
                //don't use a foreach here because the body of the loop may delete a list entry
                for (int k = 0; k < n.synapsesFrom.Count; k++)
                {
                    Synapse s = n.SynapsesFrom[k];
                    if (s.TargetNeuron != -1)
                    {
                        if (theSelection.NeuronInSelection(s.TargetNeuron) > 0)
                        { }
                        else
                        { //target is in selection
                            Neuron sourceNeuron = MainWindow.theNeuronArray.neuronArray[s.TargetNeuron];
                            sourceNeuron.AddSynapse(theSelection.selectedNeuronIndex + offset, s.Weight, MainWindow.theNeuronArray);
                            sourceNeuron.DeleteSynapse(theSelection.selectedNeuronIndex);
                            k--;
                        }
                    }
                }
            }
            Update();
        }

        public void StepAndRepeat(int source, int target, float weight)
        {
            int distance = target - source;
            theSelection.EnumSelectedNeurons();
            for (Neuron n = theSelection.GetSelectedNeuron(); n != null; n = theSelection.GetSelectedNeuron())
            {
                n.AddSynapse(theSelection.selectedNeuronIndex + distance, weight, MainWindow.theNeuronArray);
            }
            Update();
        }

        //this looks at the content of a rectangular selection and builds a recognition neuron for 
        //the current firing pattern... AND connections to a target array which shows the closest match pattern
        public void Learn()
        {
            if (theSelection.selectedRectangles[0] == null) return;

            //get the selected rectangle of source neurons
            theSelection.selectedRectangles[0].GetSelectedArea(out int X1, out int Y1, out int X2, out int Y2);
            int size = (X2 - X1) * (Y2 - Y1);
            int theSourceNeuron = X1 * dp.NeuronRows + Y1;
            int targetNeuronIndex = (X2 + 2) * dp.NeuronRows + Y1;

            //find the next target neuron in the target column
            Neuron n1 = MainWindow.theNeuronArray.neuronArray[theSourceNeuron];
            while (n1.FindSynapse(targetNeuronIndex) != null)
            {
                targetNeuronIndex++;
            }

            //count the neurons with value "1" so we know how strong to make the synapses
            int count = 0;
            theSelection.EnumSelectedNeurons();
            for (Neuron n = theSelection.GetSelectedNeuron(); n != null; n = theSelection.GetSelectedNeuron())
            {
                if (n.LastCharge >= .99) count++;
            }

            float posWeight = 1 / (float)count;
            float negWeight = -1 / (float)(size - count);

            //add the synapses to the target neuron; and the output pattern
            int targetNeuronIndex2 = targetNeuronIndex + dp.NeuronRows;
            Neuron targetNeuron2 = MainWindow.theNeuronArray.neuronArray[targetNeuronIndex2];
            Neuron targetNeuron = MainWindow.theNeuronArray.neuronArray[targetNeuronIndex];
            targetNeuron.AddSynapse(targetNeuronIndex2, 1, MainWindow.theNeuronArray);

            //add the weighted connections
            theSelection.EnumSelectedNeurons();
            for (Neuron n = theSelection.GetSelectedNeuron(); n != null; n = theSelection.GetSelectedNeuron())
            {
                if (n.LastCharge >= 1)
                {
                    n.AddSynapse(targetNeuronIndex, posWeight, MainWindow.theNeuronArray);
                    //build the output pattern
                    targetNeuron2.AddSynapse((X2 + 6) * dp.NeuronRows + theSelection.selectedNeuronIndex, 1, MainWindow.theNeuronArray);
                }
                else
                {
                    n.AddSynapse(targetNeuronIndex, negWeight, MainWindow.theNeuronArray);
                };
            }
            Update();
        }

        public void Hebbian()
        {
            if (theSelection.selectedRectangles[0] == null) return;
            //get the selected rectangle of source neurons
            int Y1, X1, Y2, X2;
            theSelection.selectedRectangles[0].GetSelectedArea(out X1, out Y1, out X2, out Y2);

            //add the synapses to the target neuron;
            for (int j = X1; j < X2; j++)
                for (int i = Y1; i < Y2; i++)
                {
                    int neuron = j * dp.NeuronRows + i;
                    //did this neuron fire in the last 3 generations?
                    Neuron n = MainWindow.theNeuronArray.neuronArray[neuron];
                //    if (n.LastFired >= MainWindow.theNeuronArray.Generation - 3)
                //    {
                //        foreach (Synapse s in n.synapses)
                //        {
                //            //if the target neuron just fired, strengthen the synapse 
                //            Neuron targetNeuron = MainWindow.theNeuronArray.neuronArray[s.TargetNeuron];
                //            if (targetNeuron.LastFired == MainWindow.theNeuronArray.Generation - 1)
                //            {
                //                float delta = 1 - s.Weight;
                //                delta *= 0.01f;
                //                s.Weight += delta;
                //            }
                //        }
                //    }
                }
        }
        //add negative synapses to all members of a selection so that when one neuron fires,
        //it prevents the others in the group from firing
        public void MutualSuppression()
        {
            List<Neuron> selectedNeurons = new List<Neuron>();
            theSelection.EnumSelectedNeurons();
            for (Neuron n = theSelection.GetSelectedNeuron(); n != null; n = theSelection.GetSelectedNeuron())
            {
                selectedNeurons.Add(n);
            }
            foreach (Neuron nSource in selectedNeurons)
            {
                theSelection.EnumSelectedNeurons();
                for (Neuron nTarget = theSelection.GetSelectedNeuron(); nTarget != null; nTarget = theSelection.GetSelectedNeuron())
                {
                    if (nSource != nTarget)
                        nSource.AddSynapse(theSelection.selectedNeuronIndex, -1, MainWindow.theNeuronArray);
                }
            }
            Update();
        }

    }
}
