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
                CreateEmptyNetwork();
                Properties.Settings.Default["CurrentFile"] = currentFileName;
                Properties.Settings.Default.Save();
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
                {
                    try
                    {
                        na.TheModule.SetUpAfterLoad();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("SetupAfterLoad failed on module " + na.Label + ".   Message: " + e.Message);
                    }
                }
            }
            return true;
        }

        private bool SaveFile(string fileName)
        {
            SuspendEngine();
            foreach (ModuleView na in theNeuronArray.modules)
            {
                if (na.TheModule != null)
                {
                    try
                    {
                        na.TheModule.SetUpBeforeSave();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("SetupBeforeSave failed on module " + na.Label + ".   Message: " + e.Message);
                    }
                }
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
            if (IsArrayEmpty()) return false;
            if (!XmlFile.CanWriteTo(currentFileName, out string message)) return false;
            MainWindow.theNeuronArray.GetCounts(out long synapseCount, out int neuronInUseCount);
            if (neuronInUseCount == 0) return false;
            SuspendEngine();

            bool retVal = false;
            MessageBoxResult mbResult = System.Windows.MessageBox.Show(this, "Do you want to save changes?", "Save", MessageBoxButton.YesNoCancel,
            MessageBoxImage.Asterisk, MessageBoxResult.No);
            if (mbResult == MessageBoxResult.Yes)
            {
                if (currentFileName != "")
                    SaveFile(currentFileName);
                else
                {
                    if (!SaveAs())
                        retVal = true;
                }
            }
            if (mbResult == MessageBoxResult.Cancel)
            {
                retVal = true;
            }
            ResumeEngine();
            return retVal;
        }
        private bool SaveAs()
        {
            string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            defaultPath += "\\BrainSim";
            try
            {
                if (Directory.Exists(defaultPath)) defaultPath = "";
                else Directory.CreateDirectory(defaultPath);
            }
            catch
            {
                //maybe myDocuments is readonly of offline? let the user do whatever they want
                defaultPath = "";
            }
            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Filter = "XML Network Files|*.xml",
                Title = "Select a Brain Simulator File",
                InitialDirectory = defaultPath
            };

            // Show the Dialog.  
            // If the user clicked OK in the dialog and  
            Nullable<bool> result = saveFileDialog1.ShowDialog();
            if (result ?? false)// System.Windows.Forms.DialogResult.OK)
            {
                if (SaveFile(saveFileDialog1.FileName))
                {
                    AddFileToMRUList(currentFileName);
                    setTitleBar();
                    return true;
                }
            }
            return false;
        }

    }
}
