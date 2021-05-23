//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace BrainSimulator
{
    public partial class NeuronArray : NeuronHandler
    {

        /// UNDO Handling 
        /// 
        //these lists keep track of changed things for undo
        List<UndoPoint> undoList = new List<UndoPoint>(); //checkpoints for multi-undos 
        List<SynapseUndo> synapseUndoInfo = new List<SynapseUndo>();
        List<NeuronUndo> neuronUndoInfo = new List<NeuronUndo>();
        List<SelectUndo> selectionUndoInfo = new List<SelectUndo>();
        List<ModuleUndo> moduleUndoInfo = new List<ModuleUndo>();

        //Undoes everything back to the last undo point.
        public void Undo()
        {
            if (undoList.Count == 0) return;
            int synapsePoint = undoList.Last().synapsePoint;
            int neuronPoint = undoList.Last().neuronPoint;
            int selectionPoint = undoList.Last().selectionPoint;
            int modulePoint = undoList.Last().modulePoint;
            undoList.RemoveAt(undoList.Count - 1);

            while (moduleUndoInfo.Count > modulePoint)
                UndoModule();
            while (selectionUndoInfo.Count > selectionPoint)
                UndoSelection();
            while (neuronUndoInfo.Count > neuronPoint)
                UndoNeuron();
            while (synapseUndoInfo.Count > synapsePoint)
                UndoSynapse();
        }

        public void SetUndoPoint()
        {
            //only add if this is different from the latest
            if (undoList.Count > 0 &&
                undoList.Last().synapsePoint == synapseUndoInfo.Count &&
                undoList.Last().neuronPoint == neuronUndoInfo.Count &&
                undoList.Last().selectionPoint == selectionUndoInfo.Count &&
                undoList.Last().modulePoint == moduleUndoInfo.Count
                ) return;
            undoList.Add(new UndoPoint
            {
                synapsePoint = synapseUndoInfo.Count,
                neuronPoint = neuronUndoInfo.Count,
                selectionPoint = selectionUndoInfo.Count,
                modulePoint = moduleUndoInfo.Count
            });
        }
        public bool UndoPossible()
        {
            return undoList.Count != 0;
        }
        struct SynapseUndo
        {
            public int source, target;
            public float weight;
            public bool newSynapse;
            public bool delSynapse;
            public Synapse.modelType model;
        }
        struct NeuronUndo
        {
            public Neuron previousNeuron;
            public bool neuronIsShowingSynapses;
        }
        struct SelectUndo
        {
            public Selection selectionState;
        }
        struct ModuleUndo
        {
            public int index;
            public ModuleView moduleState;
        }
        struct UndoPoint
        {
            public int synapsePoint;
            public int neuronPoint;
            public int selectionPoint;
            public int modulePoint;
        }

 
        public void AddSynapseUndo(int source, int target, float weight, Synapse.modelType model, bool newSynapse, bool delSynapse)
        {
            SynapseUndo s;
            s = new SynapseUndo
            {
                source = source,
                target = target,
                weight = weight,
                model = model,
                newSynapse = newSynapse,
                delSynapse = delSynapse,
            };
            synapseUndoInfo.Add(s);
        }
        public void AddNeuronUndo(Neuron n)
        {
            Neuron n1 = n.Copy();
            neuronUndoInfo.Add(new NeuronUndo { previousNeuron = n1, neuronIsShowingSynapses = MainWindow.arrayView.IsShowingSnapses(n1.id) });
        }
        public void AddSelectionUndo()
        {
            SelectUndo s1 = new SelectUndo();
            s1.selectionState = new Selection();
            foreach (SelectionRectangle nsr in MainWindow.arrayView.theSelection.selectedRectangles)
            {
                SelectionRectangle nsr1 = new SelectionRectangle(nsr.FirstSelectedNeuron, nsr.Height, nsr.Width);
                s1.selectionState.selectedRectangles.Add(nsr1);
            }
            selectionUndoInfo.Add(s1);
        }
        public void AddModuleUndo(int index, ModuleView currentModule)
        {
            ModuleUndo m1 = new ModuleUndo();
            m1.index = index;
            if (currentModule == null)
                m1.moduleState = null;
            else
                m1.moduleState = new ModuleView()
                {
                    Width = currentModule.Width,
                    Height = currentModule.Height,
                    FirstNeuron = currentModule.FirstNeuron,
                    Color = currentModule.Color,
                    CommandLine = currentModule.CommandLine,
                    Label = currentModule.Label
                };
            moduleUndoInfo.Add(m1);
        }

        private void UndoNeuron()
        {
            if (neuronUndoInfo.Count == 0) return;
            NeuronUndo n = neuronUndoInfo.Last();
            neuronUndoInfo.RemoveAt(neuronUndoInfo.Count - 1);
            Neuron n1 = n.previousNeuron.Copy();
            if (n1.Label != "") RemoveLabelFromCache(n1.Id);
            if (n1.label != "") AddLabelToCache(n1.Id, n1.label);

            if (n.neuronIsShowingSynapses)
                MainWindow.arrayView.AddShowSynapses(n1.id);
            n1.Update();
        }
        private void UndoSelection()
        {
            MainWindow.arrayView.theSelection.selectedRectangles.Clear();
            foreach (SelectionRectangle nsr in selectionUndoInfo.Last().selectionState.selectedRectangles)
            {
                SelectionRectangle nsr1 = new SelectionRectangle(nsr.FirstSelectedNeuron, nsr.Height, nsr.Width);
                MainWindow.arrayView.theSelection.selectedRectangles.Add(nsr1);
            }
            selectionUndoInfo.RemoveAt(selectionUndoInfo.Count - 1);
        }
        private void UndoModule()
        {
            if (moduleUndoInfo.Count == 0) return;
            ModuleUndo m1 = moduleUndoInfo.Last();
            if (m1.moduleState == null)
            {
                ModuleView.DeleteModule(m1.index);
            }
            else
            {
                if (m1.index == -1) //the module was just deleted
                {
                    ModuleView mv = new ModuleView
                    {
                        Width = m1.moduleState.Width,
                        Height = m1.moduleState.Height,
                        FirstNeuron = m1.moduleState.FirstNeuron,
                        Color = m1.moduleState.Color,
                        CommandLine = m1.moduleState.CommandLine,
                        Label = m1.moduleState.Label,
                    };
                    // modules.Add(mv);
                    ModuleView.CreateModule(mv.Label, mv.CommandLine, Utils.IntToColor(mv.Color), mv.FirstNeuron, mv.Width, mv.Height);
                }
                else
                {
                    modules[m1.index].Width = m1.moduleState.Width;
                    modules[m1.index].Height = m1.moduleState.Height;
                    modules[m1.index].FirstNeuron = m1.moduleState.FirstNeuron;
                    modules[m1.index].Color = m1.moduleState.Color;
                    modules[m1.index].CommandLine = m1.moduleState.CommandLine;
                    modules[m1.index].Label = m1.moduleState.Label;
                }
            }
            moduleUndoInfo.RemoveAt(moduleUndoInfo.Count - 1);
        }
        private void UndoSynapse()
        {
            if (synapseUndoInfo.Count == 0) return;
            SynapseUndo s = synapseUndoInfo.Last();
            synapseUndoInfo.RemoveAt(synapseUndoInfo.Count - 1);

            Neuron n = GetNeuron(s.source);
            if (s.newSynapse) //the synapse was added so delete it
            {
                n.DeleteSynapse(s.target);
            }
            else if (s.delSynapse) //the synapse was deleted so add it back
            {
                n.AddSynapse(s.target, s.weight, s.model);
            }
            else //weight/type changed 
            {
                n.AddSynapse(s.target, s.weight, s.model);
            }
            n.Update();
        }
    }
}
