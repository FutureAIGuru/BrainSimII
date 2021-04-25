
//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Windows;

namespace BrainSimulator
{
    public partial class NeuronArrayView
    {
        struct BoundarySynapse
        {
            public int innerNeuronID;
            //public string outerNeuronName;
            public int outerNeuronID;
            public float weight;
            public Synapse.modelType model;
        }
        List<BoundarySynapse> boundarySynapsesOut = new List<BoundarySynapse>();
        List<BoundarySynapse> boundarySynapsesIn = new List<BoundarySynapse>();

        public void ClearSelection()
        {
            theSelection.selectedRectangles.Clear();
            targetNeuronIndex = -1;
        }

        //copy the selection to a clipboard
        public void CopyNeurons()
        {
            //get list of neurons to copy
            List<int> neuronsToCopy = theSelection.EnumSelectedNeurons();
            theSelection.GetSelectedBoundingRectangle(out int X1o, out int Y1o, out int X2o, out int Y2o);
            MainWindow.myClipBoard = new NeuronArray();
            NeuronArray myClipBoard;
            myClipBoard = MainWindow.myClipBoard;
            myClipBoard.Initialize((X2o - X1o + 1) * (Y2o - Y1o + 1), (Y2o - Y1o + 1),true);
            boundarySynapsesOut.Clear();
            boundarySynapsesIn.Clear();

            //copy the neurons
            foreach (int nID in neuronsToCopy)
            {
                int destId = GetClipboardId(X1o, Y1o, nID);
                //copy the source neuron to the clipboard
                Neuron sourceNeuron = MainWindow.theNeuronArray.GetNeuron(nID);
                Neuron destNeuron = sourceNeuron.Clone();
                destNeuron.Owner = myClipBoard;
                destNeuron.Id = destId;
                destNeuron.Label = sourceNeuron.Label;
                myClipBoard.SetNeuron(destId, destNeuron);
            }

            //copy the synapses (this is two-pass so we make sure all neurons exist prior to copying
            foreach (int nID in neuronsToCopy)
            {
                Neuron sourceNeuron = MainWindow.theNeuronArray.GetNeuron(nID);
                if (MainWindow.useServers)
                {
                    sourceNeuron.synapses = NeuronClient.GetSynapses(sourceNeuron.id);
                }

                int destId = GetClipboardId(X1o, Y1o, nID);
                Neuron destNeuron = myClipBoard.GetNeuron(destId);
                destNeuron.Owner = myClipBoard;
                if (sourceNeuron.Synapses != null)
                    foreach (Synapse s in sourceNeuron.Synapses)
                    {
                        //only copy synapses with both ends in the selection
                        if (neuronsToCopy.Contains(s.TargetNeuron))
                        {
                            destNeuron.AddSynapse(GetClipboardId(X1o, Y1o, s.TargetNeuron), s.Weight, s.model);
                        }
                        else
                        {
                            string targetName = MainWindow.theNeuronArray.GetNeuron(s.targetNeuron).label;
                            if (targetName != "")
                            {
                                boundarySynapsesOut.Add(new BoundarySynapse
                                {
                                    innerNeuronID = destNeuron.id,
                                    outerNeuronID = s.targetNeuron,
                                    weight = s.weight,
                                    model = s.model
                                });
                            }
                        }
                    }
                if (sourceNeuron.SynapsesFrom != null)
                    foreach (Synapse s in sourceNeuron.SynapsesFrom)
                    {
                        if (!neuronsToCopy.Contains(s.TargetNeuron))
                        {
                            string sourceName = MainWindow.theNeuronArray.GetNeuron(s.targetNeuron).label;
                            if (sourceName != "")
                            {
                                boundarySynapsesIn.Add(new BoundarySynapse
                                {
                                    innerNeuronID = destNeuron.id,
                                    outerNeuronID = s.targetNeuron,
                                    weight = s.weight,
                                    model = s.model
                                }); ;
                            }
                        }
                    }
            }

            //copy modules
            foreach (ModuleView mv in MainWindow.theNeuronArray.modules)
            {
                if (theSelection.NeuronInSelection(mv.FirstNeuron) > 0 && theSelection.NeuronInSelection(mv.LastNeuron) > 0)
                {
                    ModuleView newMV = new ModuleView()
                    {
                        FirstNeuron = GetClipboardId(X1o, Y1o, mv.FirstNeuron),
                        TheModule = mv.TheModule,
                        Color = mv.Color,
                        Height = mv.Height,
                        Width = mv.Width,
                        Label = mv.Label,
                        CommandLine = mv.CommandLine,
                    };

                    myClipBoard.modules.Add(newMV);
                }
            }
        }

        private int GetClipboardId(int X1o, int Y1o, int nID)
        {
            //get the row & col in the neuronArray
            MainWindow.theNeuronArray.GetNeuronLocation(nID, out int col, out int row);
            //get the destID in the clipboard
            int destId = MainWindow.myClipBoard.GetNeuronIndex(col - X1o, row - Y1o);
            return destId;
        }

        private int GetNeuronArrayId(int nID)
        {
            MainWindow.myClipBoard.GetNeuronLocation(nID, out int col, out int row);
            MainWindow.theNeuronArray.GetNeuronLocation(targetNeuronIndex, out int targetCol, out int targetRow);
            int destId = MainWindow.theNeuronArray.GetNeuronIndex(col + targetCol, row + targetRow);
            return destId;
        }


        public void CutNeurons()
        {
            CopyNeurons();
            DeleteSelection();
            DeleteModulesInSelection();
            ClearSelection();
            Update();
        }

        private void DeleteModulesInSelection()
        {
            for (int i = 0; i < MainWindow.theNeuronArray.modules.Count; i++)
            {
                ModuleView mv = MainWindow.theNeuronArray.modules[i];
                if (theSelection.NeuronInSelection(mv.FirstNeuron) > 0 && theSelection.NeuronInSelection(mv.LastNeuron) > 0)
                {
                    MainWindow.theNeuronArray.modules.RemoveAt(i);
                    i--;
                }
            }
        }

        public void PasteNeurons()
        {
            NeuronArray myClipBoard = MainWindow.myClipBoard;

            if (targetNeuronIndex == -1) return;
            if (myClipBoard == null) return;

            //We are pasting neurons from the clipboard.  
            //The arrays have different sizes so we may by row-col.

            //first check to see if the destination is claar and warn
            List<int> targetNeurons = new List<int>();
            for (int i = 0; i < myClipBoard.arraySize; i++)
            {
                if (myClipBoard.GetNeuron(i,true) != null)
                {
                    targetNeurons.Add(GetNeuronArrayId(i));
                }
            }

            MainWindow.theNeuronArray.GetNeuronLocation(targetNeuronIndex, out int col, out int row);
            if (col + myClipBoard.Cols > MainWindow.theNeuronArray.Cols ||
                row + myClipBoard.rows > MainWindow.theNeuronArray.rows)
            {
                MessageBoxResult result = MessageBox.Show("Paste would exceed neuron array boundary!", "Error", MessageBoxButton.OK);
                return;
            }

            if (!IsDestinationClear(targetNeurons, 0, true))
            {
                MessageBoxResult result = MessageBox.Show("Some desination neurons are in use and will be overwritten, continue?", "Continue", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No) return;
            }

            MainWindow.theNeuronArray.SetUndoPoint();
            //now paste the neurons
            for (int i = 0; i < myClipBoard.arraySize; i++)
            {
                if (myClipBoard.GetNeuron(i) != null)
                {
                    int destID = GetNeuronArrayId(i);
                    MainWindow.theNeuronArray.GetNeuron(destID).AddUndoInfo();
                    Neuron n = myClipBoard.GetCompleteNeuron(i,true);
                    n.Owner = myClipBoard;
                    n.synapses = myClipBoard.GetSynapsesList(i);

                    Neuron sourceNeuron = n.Clone();
                    sourceNeuron.id = destID;
                    while (sourceNeuron.label != "" && MainWindow.theNeuronArray.GetNeuron(sourceNeuron.label) != null)
                    {
                        int num = 0;
                        int digitCount = 0;
                        while (sourceNeuron.label != "" && Char.IsDigit(sourceNeuron.label[sourceNeuron.label.Length - 1]))
                        {
                            int.TryParse(sourceNeuron.label[sourceNeuron.label.Length - 1].ToString(), out int digit);
                            num = num + (int)Math.Pow(10, digitCount) * digit;
                            digitCount++;
                            sourceNeuron.label = sourceNeuron.label.Substring(0, sourceNeuron.label.Length - 1);
                        }
                        num++;
                        sourceNeuron.label = sourceNeuron.label + num.ToString();
                    }
                    sourceNeuron.Owner = MainWindow.theNeuronArray;
                    sourceNeuron.Label = sourceNeuron.label;
                    MainWindow.theNeuronArray.SetNeuron(destID, sourceNeuron);


                    foreach (Synapse s in n.Synapses)
                    {
                        MainWindow.theNeuronArray.GetNeuron(destID).
                            AddSynapseWithUndo(GetNeuronArrayId(s.TargetNeuron), s.Weight, s.model);
                    }
                }
            }

            //handle boundary synapses
            foreach (BoundarySynapse b in boundarySynapsesOut)
            {
                int sourceID = GetNeuronArrayId(b.innerNeuronID);
                Neuron targetNeuron = MainWindow.theNeuronArray.GetNeuron(b.outerNeuronID);
                if (targetNeuron != null)
                    MainWindow.theNeuronArray.GetNeuron(sourceID).AddSynapseWithUndo(targetNeuron.id, b.weight, b.model);
            }
            foreach (BoundarySynapse b in boundarySynapsesIn)
            {
                int targetID = GetNeuronArrayId(b.innerNeuronID);
                Neuron sourceNeuron = MainWindow.theNeuronArray.GetNeuron(b.outerNeuronID);
                if (sourceNeuron != null)
                    sourceNeuron.AddSynapseWithUndo(targetID, b.weight, b.model);
            }

            //paste modules
            foreach (ModuleView mv in myClipBoard.modules)
            {
                ModuleView newMV = new ModuleView()
                {
                    FirstNeuron = GetNeuronArrayId(mv.FirstNeuron),
                    TheModule = mv.TheModule,
                    Color = mv.Color,
                    Height = mv.Height,
                    Width = mv.Width,
                    Label = mv.Label,
                    CommandLine = mv.CommandLine,
                };

                MainWindow.theNeuronArray.modules.Add(newMV);
            }

            Update();
        }

        public void ConnectFromHere()
        {
            if (targetNeuronIndex == -1) return;
            MainWindow.theNeuronArray.SetUndoPoint();
            Neuron targetNeuron = MainWindow.theNeuronArray.GetNeuron(targetNeuronIndex);
            List<int> neuronsInSelection = theSelection.EnumSelectedNeurons();
            for (int i = 0; i < neuronsInSelection.Count; i++)
            {
                targetNeuron.AddSynapseWithUndo(neuronsInSelection[i], lastSynapseWeight, lastSynapseModel);
            }
            Update();
        }

        public void ConnectToHere()
        {
            if (targetNeuronIndex == -1) return;
            MainWindow.theNeuronArray.SetUndoPoint();
            List<int> neuronsInSelection = theSelection.EnumSelectedNeurons();
            for (int i = 0; i < neuronsInSelection.Count; i++)
            {
                Neuron n = MainWindow.theNeuronArray.GetNeuron(neuronsInSelection[i]);
                n.AddSynapseWithUndo(targetNeuronIndex, lastSynapseWeight, lastSynapseModel);
            }
            Update();
        }


        public void DeleteSelection(bool deleteBoundarySynapses = true, bool allowUndo = true)
        {
            if (allowUndo)
                MainWindow.theNeuronArray.SetUndoPoint();
            List<int> neuronsToDelete = theSelection.EnumSelectedNeurons();
            foreach (int nID in neuronsToDelete)
            {
                Neuron n = MainWindow.theNeuronArray.GetNeuron(nID);
                if (deleteBoundarySynapses)
                {
                    foreach(Synapse s in n.synapsesFrom)
                    {
                        Neuron source = MainWindow.theNeuronArray.GetNeuron(s.targetNeuron);
                        if (source != null && theSelection.NeuronInSelection(source.id)==0)
                        {
                            source.DeleteSynapseWithUndo(n.id);
                        }
                    }
                }
                for (int i = 0; i < n.synapses.Count; i++)
                    n.DeleteSynapseWithUndo(n.synapses[i].targetNeuron);
                n.AddUndoInfo();
                n.CurrentCharge = 0;
                n.LastCharge = 0;
                n.Model = Neuron.modelType.IF;

                n.Label = "";
                n.Update();
            }
            DeleteModulesInSelection();
            Update();
        }

        private bool IsDestinationClear(List<int> neuronsToMove, int offset, bool flagOverlap = false)
        {
            bool retVal = true;
            foreach (int id in neuronsToMove)
            {
                if (flagOverlap || !neuronsToMove.Contains(id + offset))
                {
                    Neuron n = MainWindow.theNeuronArray.GetNeuron(id + offset);
                    if (n.InUse())
                    {
                        retVal = false;
                        break;
                    }
                }
            }
            return retVal;
        }

        //move the neurons from the selected area to the second (start point) and stretch all the synapses
        public void MoveNeurons(bool dragging = false)
        {
            if (theSelection.selectedNeuronIndex == -1) return;
            if (theSelection.selectedRectangles.Count == 0) return;

            List<int> neuronsToMove = theSelection.EnumSelectedNeurons();
            int maxCol = 0;
            int maxRow = 0;
            MainWindow.theNeuronArray.GetNeuronLocation(theSelection.selectedRectangles[0].FirstSelectedNeuron, out int col0, out int row0);
            foreach (int id in neuronsToMove)
            {
                MainWindow.theNeuronArray.GetNeuronLocation(id, out int tcol, out int trow);
                if (maxCol < tcol - col0) maxCol = tcol - col0;
                if (maxRow < trow - row0) maxRow = trow - row0;
            }

            int offset = targetNeuronIndex - theSelection.selectedRectangles[0].FirstSelectedNeuron;
            if (offset == 0) return;

            MainWindow.theNeuronArray.GetNeuronLocation(targetNeuronIndex, out int col, out int row);
            if (col + maxCol >= MainWindow.theNeuronArray.Cols ||
                row + maxRow >= MainWindow.theNeuronArray.rows ||
                row < 0 || 
                col < 0)
            {
                if (!dragging)
                     MessageBox.Show("Move would exceed neuron array boundary!", "Error", MessageBoxButton.OK);
                return;
            }

            if (!IsDestinationClear(neuronsToMove, offset))
            {
                MessageBoxResult result = MessageBox.Show("Some destination neurons are in use and will be overwritten, continue?\n\nYou can also right-click the final destination neuron and select 'Move Here.'", "Continue", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No) return;
            }

            if (!dragging)
            {
                MainWindow.theNeuronArray.SetUndoPoint();
                MainWindow.theNeuronArray.AddSelectionUndo();
            }
            //change the order of copying to keep from overwriting ourselves
            if (offset > 0) neuronsToMove.Reverse();
            foreach (int source in neuronsToMove)
            {
                Neuron sourceNeuron = MainWindow.theNeuronArray.GetNeuron(source);
                Neuron destNeuron = MainWindow.theNeuronArray.GetNeuron(source + offset);
                MoveOneNeuron(sourceNeuron, destNeuron);
                if (MainWindow.arrayView.IsShowingSnapses(source))
                {
                    MainWindow.arrayView.RemoveShowSynapses(source);
                    MainWindow.arrayView.AddShowSynapses(source + offset);
                }
            }

            foreach (ModuleView mv in MainWindow.theNeuronArray.modules)
            {
                if (theSelection.NeuronInSelection(mv.FirstNeuron) > 0 && theSelection.NeuronInSelection(mv.LastNeuron) > 0)
                {
                    mv.FirstNeuron += offset;
                }
            }

            try
            {
                targetNeuronIndex = -1;
                foreach(NeuronSelectionRectangle nsr in theSelection.selectedRectangles)
                {
                    nsr.FirstSelectedNeuron += offset;
                }
                Update();
            }
            catch { }
        }

        public void MoveOneNeuron(Neuron n, Neuron nNewLocation, bool addUndo = true)
        {
            if (addUndo)
            {
                n.AddUndoInfo();
                nNewLocation.AddUndoInfo();
            }
            if(MainWindow.useServers)
            {
                n.synapses = NeuronClient.GetSynapses(n.id);
                n.synapsesFrom = NeuronClient.GetSynapsesFrom(n.id);
            }

            //copy the neuron attributes and delete them from the old neuron.
            n.Copy(nNewLocation);
            MainWindow.theNeuronArray.SetCompleteNeuron(nNewLocation);
            if (FiringHistory.NeuronIsInFiringHistory(n.id))
            {
                FiringHistory.RemoveNeuronFromHistoryWindow(n.id);
                FiringHistory.AddNeuronToHistoryWindow(nNewLocation.id);
            }

            //for all the synapses going out this neuron, change to going from new location
            //don't use a foreach here because the body of the loop may delete a list entry
            for (int k = 0; k < n.Synapses.Count; k++)
            {
                Synapse s = n.Synapses[k];
                if (addUndo)
                {
                    if (s.targetNeuron != n.id)
                        nNewLocation.AddSynapseWithUndo(s.targetNeuron, s.weight, s.model);
                    else
                        nNewLocation.AddSynapseWithUndo(nNewLocation.id, s.weight, s.model);
                    n.DeleteSynapseWithUndo(n.synapses[k].targetNeuron);
                }
                else
                {
                    if (s.targetNeuron != n.id)
                        nNewLocation.AddSynapse(s.targetNeuron, s.weight, s.model);
                    else
                        nNewLocation.AddSynapse(nNewLocation.id, s.weight, s.model);
                    n.DeleteSynapse(n.synapses[k].targetNeuron);
                }
            }

            //for all the synapses coming into this neuron, change the synapse target to new location
            for (int k = 0; k < n.SynapsesFrom.Count; k++)
            {
                Synapse reverseSynapse = n.SynapsesFrom[k]; //(from synapses are sort-of backward
                if (reverseSynapse.targetNeuron != -1) //?
                {
                    Neuron sourceNeuron = MainWindow.theNeuronArray.GetNeuron(reverseSynapse.targetNeuron);
                    sourceNeuron.DeleteSynapseWithUndo(n.id);
                    if (sourceNeuron.id != n.id)
                        if (addUndo)
                        {
                            sourceNeuron.AddSynapseWithUndo(nNewLocation.id, reverseSynapse.weight, reverseSynapse.model);
                        }
                        else
                        {
                            sourceNeuron.AddSynapse(nNewLocation.id, reverseSynapse.weight, reverseSynapse.model);
                        }
                }
            }

            n.Clear();
        }

        public void StepAndRepeat(int source, int target, float weight, Synapse.modelType model)
        {
            int distance = target - source;
            theSelection.EnumSelectedNeurons();
            for (Neuron n = theSelection.GetSelectedNeuron(); n != null; n = theSelection.GetSelectedNeuron())
            {
                n.AddSynapseWithUndo(theSelection.selectedNeuronIndex + distance, weight, model);
            }
            Update();
        }

        private Random rand;
        public void CreateRandomSynapses(int synapsesPerNeuron)
        {
            MainWindow.theNeuronArray.SetUndoPoint();
            MainWindow.thisWindow.SetProgress(0, "Allocating Random Synapses");
            List<int> neuronsInSelection = theSelection.EnumSelectedNeurons();
            for (int i = 0; i < neuronsInSelection.Count; i++)
            {
                if (MainWindow.thisWindow.SetProgress(100f * i / (float)neuronsInSelection.Count, "")) break;
                Neuron n = MainWindow.theNeuronArray.GetNeuron(neuronsInSelection[i]);
                if (rand == null) rand = new Random();
                for (int j = 0; j < synapsesPerNeuron; j++)
                {
                    int targetNeuron = neuronsInSelection[rand.Next(neuronsInSelection.Count-1)];
                    float weight = (rand.Next(521) / 1000f) - .2605f;
                    n.AddSynapseWithUndo(targetNeuron, weight, Synapse.modelType.Fixed);
                }
            }
            MainWindow.thisWindow.SetProgress(100, "");
            Update();
        }
        public void MutualSuppression()
        {
            MainWindow.theNeuronArray.SetUndoPoint();
            MainWindow.thisWindow.SetProgress(0, "Allocating Synapses");

            List<int> neuronsInSelection = theSelection.EnumSelectedNeurons();
            for (int i = 0; i < neuronsInSelection.Count; i++) 
            {
                if (MainWindow.thisWindow.SetProgress(100 * i / (float)neuronsInSelection.Count, "")) break; 
                Neuron n = MainWindow.theNeuronArray.GetNeuron(neuronsInSelection[i]);
                foreach (Neuron n1 in theSelection.Neurons)
                {
                    if (n.id != n1.id)
                    {
                        n.AddSynapseWithUndo(n1.id, -1, Synapse.modelType.Fixed);
                    }
                }
            }
            MainWindow.thisWindow.SetProgress(100, "");
            Update();
        }
    }
}
