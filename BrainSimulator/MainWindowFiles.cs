//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private async void LoadFile(string fileName)
        {
            CloseAllModuleDialogs();
            CloseHistoryWindow();
            CloseNotesWindow();
            theNeuronArrayView.theSelection.selectedRectangles.Clear();
            CloseAllModuleDialogs();
            SuspendEngine();

            bool success = false;
            await Task.Run(delegate { success = XmlFile.Load(ref theNeuronArray, fileName); });
            if (!success)
            {
                Update();
                return;
            }
            currentFileName = fileName;

            if (XmlFile.CanWriteTo(currentFileName))
                SaveButton.IsEnabled = true;
            else
                SaveButton.IsEnabled = false;

            setTitleBar();
            await Task.Delay(1000).ContinueWith(t => ShowDialogs());
            foreach (ModuleView na in theNeuronArray.modules)
            {
                if (na.TheModule != null)
                    na.TheModule.SetUpAfterLoad();
            }
            if (theNeuronArray.displayParams != null)
                theNeuronArrayView.Dp = theNeuronArray.displayParams;
            NeuronArrayView.SortAreas();

            AddFileToMRUList(currentFileName);
            Properties.Settings.Default["CurrentFile"] = currentFileName;
            Properties.Settings.Default.Save();

            Update();
            SetShowSynapsesCheckBox(theNeuronArray.ShowSynapses);
            SetPlayPauseButtonImage(theNeuronArray.EngineIsPaused);
            ResumeEngine();
        }

        private bool LoadClipBoardFromFile(string fileName)
        {

            XmlFile.Load(ref myClipBoard, fileName);

            foreach (ModuleView na in myClipBoard.modules)
            {
                if (na.TheModule != null)
                    na.TheModule.SetUpAfterLoad();
            }
            return true;
        }

        private bool SaveFile(string fileName)
        {
            SuspendEngine();
            foreach (ModuleView na in theNeuronArray.modules)
            {
                if (na.TheModule != null)
                    na.TheModule.SetUpBeforeSave();
            }

            theNeuronArray.displayParams = theNeuronArrayView.Dp;
            if (XmlFile.Save(theNeuronArray, fileName))
            {
                currentFileName = fileName;
                SetCurrentFileNameToProperties();
                ResumeEngine();
                return true;
            }
            else
            {
                ResumeEngine();
                return false;
            }
        }
        private void SaveClipboardToFile(string fileName)
        {
            foreach (ModuleView na in myClipBoard.modules)
            {
                if (na.TheModule != null)
                    na.TheModule.SetUpBeforeSave();
            }

            if (XmlFile.Save(myClipBoard, fileName))
                currentFileName = fileName;
        }

        private void AddFileToMRUList(string filePath)
        {
            StringCollection MRUList = (StringCollection)Properties.Settings.Default["MRUList"];
            if (MRUList == null)
                MRUList = new StringCollection();
            MRUList.Remove(filePath); //remove it if it's already there
            MRUList.Insert(0, filePath); //add it to the top of the list
            Properties.Settings.Default["MRUList"] = MRUList;
            Properties.Settings.Default.Save();
        }

        private void LoadCurrentFile()
        {
            LoadFile(currentFileName);
        }


        private static void SetCurrentFileNameToProperties()
        {
            Properties.Settings.Default["CurrentFile"] = currentFileName;
            Properties.Settings.Default.Save();
        }

        private bool PromptToSaveChanges()
        {
            if (theNeuronArray == null) return false;
            if (!XmlFile.CanWriteTo(currentFileName, out string message)) return false;
            MainWindow.theNeuronArray.GetCounts(out long synapseCount, out int neuronInUseCount);
            if (neuronInUseCount == 0) return false;

            bool retVal = false;
            MessageBoxResult mbResult = System.Windows.MessageBox.Show(this, "Do you want to save changes?", "Save", MessageBoxButton.YesNoCancel,
            MessageBoxImage.Asterisk, MessageBoxResult.No);
            if (mbResult == MessageBoxResult.Yes)
            {
                if (currentFileName != "")
                    SaveFile(currentFileName);
                else
                    buttonSaveAs_Click(null, null);
            }
            if (mbResult == MessageBoxResult.Cancel)
            {
                retVal = true;
            }
            return retVal;
        }

    }
}
