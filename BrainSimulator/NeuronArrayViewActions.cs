
//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System.Collections.Generic;
using System.Windows;

namespace BrainSimulator
{
    public partial class NeuronArrayView
    {
        public void ClearSelection()
        {
            theSelection.selectedRectangles.Clear();
            targetNeuronIndex = -1;
            //Update();
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
            myClipBoard.Initialize((X2o - X1o + 1) * (Y2o - Y1o + 1), (Y2o - Y1o + 1));

            //copy the neurons
            foreach (int nID in neuronsToCopy)
            {
                int destId = GetClipboardId(X1o, Y1o, nID);
                //copy the source neuron to the clipboard
                Neuron sourceNeuron = MainWindow.theNeuronArray.GetNeuron(nID);
                Neuron destNeuron = sourceNeuron.Clone();
                destNeuron.Owner = myClipBoard;
                destNeuron.Id = destId;
                myClipBoard.SetNeuron(destId, destNeuron);
            }
            //copy the synapses (this is two-pass so we make sure all neurons exist prior to copying
            foreach (int nID in neuronsToCopy)
            {
                Neuron sourceNeuron = MainWindow.theNeuronArray.GetNeuron(nID);
                int destId = GetClipboardId(X1o, Y1o, nID);
                Neuron destNeuron = myClipBoard.GetNeuron(destId);
                destNeuron.Owner = myClipBoard;
                if (sourceNeuron.Synapses != null)
                    foreach (Synapse s in sourceNeuron.Synapses)
                    {
                        //only copy synapses with both ends in the selection
                        if (neuronsToCopy.Contains(s.TargetNeuron))
                        {
                            destNeuron.AddSynapse(GetClipboardId(X1o, Y1o, s.TargetNeuron), s.Weight, s.isHebbian);
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

        public void PasteNeurons(bool pasteSynapses = true)
        {
            NeuronArray myClipBoard;
            myClipBoard = MainWindow.myClipBoard;

            if (targetNeuronIndex == -1) return;
            if (myClipBoard == null) return;
            //We are pasting neurons from the clipboard.  
            //The arrays have different sizes so we may by row-col.

            //first check to see if the destination is claar and warn
            List<int> targetNeurons = new List<int>();
            for (int i = 0; i < myClipBoard.arraySize; i++)
            {
                if (myClipBoard.GetNeuron(i) != null)
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
                MessageBoxResult result = MessageBox.Show("Some desination is are in use and will be overwritten, continue?", "Continue", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No) return;
            }

            //now past the neurons
            for (int i = 0; i < myClipBoard.arraySize; i++)
            {
                if (myClipBoard.GetNeuron(i) != null)
                {
                    int destID = GetNeuronArrayId(i);

                    Neuron sourceNeuron = myClipBoard.GetNeuron(i).Clone();
                    sourceNeuron.id = destID;
                    MainWindow.theNeuronArray.SetNeuron(destID, sourceNeuron);
                    //MainWindow.theNeuronArray.GetNeuron(destID).Id = destID;
                    if (pasteSynapses)
                    {
                        foreach (Synapse s in myClipBoard.GetNeuron(i).Synapses)
                        {
                            MainWindow.theNeuronArray.GetNeuron(destID).AddSynapse(GetNeuronArrayId(s.TargetNeuron), s.Weight);
                        }
                    }
                }
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
            Neuron targetNeuron = MainWindow.theNeuronArray.GetNeuron(targetNeuronIndex);
            List<int> neuronsInSelection = theSelection.EnumSelectedNeurons();
            for (int i = 0; i < neuronsInSelection.Count; i++)
            {
                targetNeuron.AddSynapse(neuronsInSelection[i], lastSynapseWeight, lastSynapseHebbian);
            }
            Update();
        }

        public void ConnectToHere()
        {
            if (targetNeuronIndex == -1) return;
            List<int> neuronsInSelection = theSelection.EnumSelectedNeurons();
            for (int i = 0; i < neuronsInSelection.Count; i++)
            {
                Neuron n = MainWindow.theNeuronArray.GetNeuron(neuronsInSelection[i]);
                n.AddSynapse(targetNeuronIndex, lastSynapseWeight,lastSynapseHebbian);
            }
            Update();
        }


        public void DeleteSelection(bool deleteSynapses = true)
        {
            List<int> neuronsToDelete = theSelection.EnumSelectedNeurons();
            foreach (int nID in neuronsToDelete)
            {
                Neuron n = MainWindow.theNeuronArray.GetNeuron(nID);
                n.CurrentCharge = 0;
                n.LastCharge = 0;
                n.Model = Neuron.modelType.IF;
                if (deleteSynapses)
                {
                    n.DeleteAllSynapes();
                }
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
        public void MoveNeurons()
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


            MainWindow.theNeuronArray.GetNeuronLocation(targetNeuronIndex, out int col, out int row);
            if (col + maxCol >= MainWindow.theNeuronArray.Cols ||
                row + maxRow >= MainWindow.theNeuronArray.rows)
            {
                MessageBoxResult result = MessageBox.Show("Move would exceed neuron array boundary!", "Error", MessageBoxButton.OK);
                return;
            }

            int offset = targetNeuronIndex - theSelection.selectedRectangles[0].FirstSelectedNeuron;

            if (!IsDestinationClear(neuronsToMove, offset))
            {
                MessageBoxResult result = MessageBox.Show("Some desination is are in use and will be overwritten, continue?", "Continue", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No) return;
            }

            //change the order of copying to to keep from overwriting ourselves
            if (offset > 0) neuronsToMove.Reverse();
            foreach (int source in neuronsToMove)
            {
                Neuron sourceNeuron = MainWindow.theNeuronArray.GetNeuron(source);
                Neuron destNeuron = MainWindow.theNeuronArray.GetNeuron(source + offset);
                MoveOneNeuron(sourceNeuron, destNeuron);
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
                theSelection.selectedRectangles.Clear();
                Update();
            }
            catch { }
        }

        public void MoveOneNeuron(Neuron n, Neuron nNewLocation)
        {
            //copy the neuron attributes and delete them from the old neuron.
            n.Copy(nNewLocation);
            MainWindow.theNeuronArray.SetCompleteNeuron(nNewLocation);

            //for all the synapses going out this neuron, change to going from new location
            //don't use a foreach here because the body of the loop may delete a list entry
            for (int k = 0; k < n.Synapses.Count; k++)
            {
                Synapse s = n.Synapses[k];
                if (s.targetNeuron != n.id)
                    nNewLocation.AddSynapse(s.targetNeuron, s.weight, s.isHebbian);
                else
                    nNewLocation.AddSynapse(nNewLocation.id, s.weight, s.isHebbian);

            }

            //for all the synapses coming into this neuron, change the synapse target to new location
            for (int k = 0; k < n.SynapsesFrom.Count; k++)
            {
                Synapse reverseSynapse = n.SynapsesFrom[k]; //(from synapses are sort-of backward
                if (reverseSynapse.targetNeuron != -1)
                {
                    Neuron sourceNeuron = MainWindow.theNeuronArray.GetNeuron(reverseSynapse.targetNeuron);
                    sourceNeuron.DeleteSynapse(n.id);
                    if (sourceNeuron.id != n.id)
                        sourceNeuron.AddSynapse(nNewLocation.id, reverseSynapse.weight, reverseSynapse.isHebbian);
                }
            }
            n.Clear();
        }

        public void StepAndRepeat(int source, int target, float weight)
        {
            int distance = target - source;
            theSelection.EnumSelectedNeurons();
            for (Neuron n = theSelection.GetSelectedNeuron(); n != null; n = theSelection.GetSelectedNeuron())
            {
                n.AddSynapse(theSelection.selectedNeuronIndex + distance, weight, MainWindow.theNeuronArray, true);
            }
            Update();
        }
    }
}
