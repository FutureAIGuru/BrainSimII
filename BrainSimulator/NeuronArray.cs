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
        public string networkNotes = "";
        public bool hideNotes = false;
        public long Generation = 0;
        public int EngineSpeed = 250;
        public bool EngineIsPaused = false;
        public int arraySize;
        public int rows;

        public int lastFireCount = 0;
        internal List<ModuleView> modules = new List<ModuleView>();
        public DisplayParams displayParams;

        //these have nothing to do with the NeuronArray but are here so it will be saved and restored with the network
        private bool showSynapses = false;
        public bool ShowSynapses { get => showSynapses; set => showSynapses = value; }
        public int Cols { get => arraySize / rows; }

        public List<ModuleView> Modules
        {
            get { return modules; }
        }

        private Dictionary<int, string> labelCache = new Dictionary<int, string>();
        public void AddLabelToCache(int nID, string label)
        {
            try
            {
                labelCache.Add(nID, label);
            }
            catch
            {
                labelCache[nID] = label;
            }
        }
        public void RemoveLabelFromCache(int nID)
        {
            try
            {
                labelCache.Remove(nID);
            }
            catch { };
        }
        public string GetLabelFromCache(int nID)
        {
            if (labelCache.ContainsKey(nID))
                return labelCache[nID];
            else
                return "";
        }

        public void ClearLabelCache()
        {
            labelCache.Clear();
        }

        public List<string> GetValuesFromLabelCache()
        {
            return labelCache.Values.ToList();
        }
        public List<int> GetKeysFromLabelCache()
        {
            return labelCache.Keys.ToList();
        }


        private int refractoryDelay = 0;
        public int RefractoryDelay
        { get => refractoryDelay; set { refractoryDelay = value; SetRefractoryDelay(refractoryDelay); } }

        public void Initialize(int count, int inRows, bool clipBoard = false)
        {
            rows = inRows;
            arraySize = count;
            ClearLabelCache();
            if (!MainWindow.useServers || clipBoard)
                base.Initialize(count);
            else
            {
                NeuronClient.InitServers(0, count);
            }
        }

        public NeuronArray()
        {
        }

        public new Neuron GetNeuron(int id, bool fromClipboard = false)
        {
            Neuron n = GetCompleteNeuron(id, fromClipboard);
            return n;
        }
        public Neuron GetNeuron(string label)
        {
            if (labelCache.ContainsValue(label))
            {
                int nID = labelCache.FirstOrDefault(x => x.Value == label).Key;
                return GetNeuron(nID);
            }
            return null;
        }


        public void GetCounts(out long synapseCount, out int useCount)
        {
            if (MainWindow.useServers)
            {
                useCount = NeuronClient.serverList.Sum(x => x.neuronsInUse);
                synapseCount = NeuronClient.serverList.Sum(x => x.totalSynapses);
            }
            else
            {
                synapseCount = GetTotalSynapses();
                useCount = GetTotalNeuronsInUse();
            }
        }

        public new void Fire()
        {
            if (MainWindow.useServers)
            {
                NeuronClient.Fire();
                lastFireCount = 0;
                for (int i = 0; i < NeuronClient.serverList.Count; i++)
                    lastFireCount += NeuronClient.serverList[i].firedCount;
                Generation = NeuronClient.serverList[0].generation;
            }
            else
            {
                base.Fire();
                Generation = GetGeneration();
                lastFireCount = GetFiredCount();
            }
            HandleProgrammedActions();
            FiringHistory.UpdateFiringHistory();
        }
        public void AddSynapse(int src, int dest, float weight, Synapse.modelType model, bool noBackPtr)
        {
            if (MainWindow.useServers && this == MainWindow.theNeuronArray)
                NeuronClient.AddSynapse(src, dest, weight, model, noBackPtr);
            else
                base.AddSynapse(src, dest, weight, (int)model, noBackPtr);
        }
        new public void DeleteSynapse(int src, int dest)
        {
            if (MainWindow.useServers && this == MainWindow.theNeuronArray)
                NeuronClient.DeleteSynapse(src, dest);
            else
                base.DeleteSynapse(src, dest);
        }

        //fires all the modules
        private void HandleProgrammedActions()
        {
            //lock (modules)
            {
                for (int i = 0; i < modules.Count; i++)
                {
                    ModuleView na = modules[i];
                    if (na.TheModule != null)
                    {
                        try
                        {
                            na.TheModule.Fire();
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Module " + na.Label + " threw unhandled exception with message:\n" + e.Message);
                        }
                    }
                }
            }
        }

        public ModuleView FindModuleByLabel(string label)
        {
            ModuleView moduleView = modules.Find(na => na.Label.Trim() == label);
            if (moduleView == null)
            {
                if (label.StartsWith("Module"))
                {
                    label = label.Replace("Module", "");
                    moduleView = modules.Find(na => na.Label.Trim() == label);
                }
            }
            return moduleView;
        }

        public void SetNeuron(int i, Neuron n)
        {
            SetCompleteNeuron(n);
        }


        public int GetNeuronIndex(int x, int y)
        {
            return x * rows + y;
        }

        public void GetNeuronLocation(int index, out int x, out int y)
        {
            x = index / rows;
            y = index % rows;
        }



        /// UNDO Handling from here on out
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
            public NeuronSelection selectionState;
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

        //these lists keep track of changed synapses and neurons for undo
        List<UndoPoint> undoList = new List<UndoPoint>(); //checkpoints for multi-undos 
        List<SynapseUndo> synapseUndoInfo = new List<SynapseUndo>();
        List<NeuronUndo> neuronUndoInfo = new List<NeuronUndo>();
        List<SelectUndo> selectionUndoInfo = new List<SelectUndo>();
        List<ModuleUndo> moduleUndoInfo = new List<ModuleUndo>();

        //what can be undone?
        //synapse change/add/delete
        //neuron change
        //multi synapse actions
        //multi neuron actions
        //cut => combo (what was in the selection before?)
        //paste => combo (what was in the target before?)
        //move => combo (what was in the target AND selection before?)


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
            s1.selectionState = new NeuronSelection();
            foreach (NeuronSelectionRectangle nsr in MainWindow.arrayView.theSelection.selectedRectangles)
            {
                NeuronSelectionRectangle nsr1 = new NeuronSelectionRectangle(nsr.FirstSelectedNeuron, nsr.Height, nsr.Width);
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
            SelectUndo s1 = new SelectUndo();
            foreach (NeuronSelectionRectangle nsr in selectionUndoInfo.Last().selectionState.selectedRectangles)
            {
                NeuronSelectionRectangle nsr1 = new NeuronSelectionRectangle(nsr.FirstSelectedNeuron, nsr.Height, nsr.Width);
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
