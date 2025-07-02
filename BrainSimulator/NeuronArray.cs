//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public int refractoryDelay = 0;
        public bool animateSynapses = false;

        public int lastFireCount = 0;
        internal List<ModuleView> modules = new List<ModuleView>();
        public DisplayParams displayParams;

        //these have nothing to do with the NeuronArray but are here so it will be saved and restored with the network
        private bool showSynapses = false;
        public bool ShowSynapses
        {
            get => showSynapses;
            set => showSynapses = value;
        }
        public int Cols { get => arraySize / rows; }
        private bool loadComplete = false;
        [XmlIgnore]
        public bool LoadComplete { get => loadComplete; set => loadComplete = value; }

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

        public Neuron GetNeuron(int id, bool fromClipboard = false)
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
            else
            {
                string searchKey = label + Neuron.toolTipSeparator;
                int nID = labelCache.FirstOrDefault(x => x.Value.StartsWith(searchKey)).Key;
                if (nID != 0)
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
            int badModule = -1;
            string message = "";
            lock (modules)
            {
                List<int> firstNeurons = new List<int>();
                for (int i = 0; i < modules.Count; i++)
                    firstNeurons.Add(modules[i].FirstNeuron);
                firstNeurons.Sort();

                for (int i = 0; i < modules.Count; i++)
                {
                    ModuleView na = modules.Find(x => x.FirstNeuron == firstNeurons[i]);
                    if (na.TheModule != null)
                    {
                        //if not in debug mode, trap exceptions and offer to delete module
#if !DEBUG
                        try
                        {
                            if (na.TheModule.isEnabled)
                                na.TheModule.Fire();
                        }
                        catch (Exception e)
                        {
                            // Get stack trace for the exception with source file information
                            var st = new StackTrace(e, true);
                            // Get the top stack frame
                            var frame = st.GetFrame(0);
                            // Get the line number from the stack frame
                            var line = frame.GetFileLineNumber();

                            message = "Module " + na.Label + " threw an unhandled exception with the message:\n" + e.Message;
                            message += "\nat line " + line;
                            message += "\n\n Would you like to remove it from this network?";
                            badModule = i;
                        }
#else
                        if (na.TheModule.isEnabled)
                            na.TheModule.Fire();
#endif
                    }
                }
            }
            if (message != "")
            {
                MessageBoxResult mbr = MessageBox.Show(message, "Remove Module?", MessageBoxButton.YesNo);
                if (mbr == MessageBoxResult.Yes)
                {
                    ModuleView.DeleteModule(badModule);
                    MainWindow.Update();
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

        public Neuron GetNeuron(int x, int y)
        {
            return GetNeuron(GetNeuronIndex(x, y));
        }

        public void GetNeuronLocation(int index, out int x, out int y)
        {
            x = index / rows;
            y = index % rows;
        }

    }
}
