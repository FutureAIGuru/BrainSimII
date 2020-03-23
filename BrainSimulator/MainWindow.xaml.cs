//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Threading;
using System.Diagnostics;
using System.Threading;
using System.IO.MemoryMappedFiles;
using System.Drawing;
using BrainSimulator.Modules;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Globals
        public static NeuronArrayView arrayView = null;
        public static NeuronArray theNeuronArray = null;
        public static FiringHistoryWindow fwWindow = null;

        Thread engineThread = new Thread(new ThreadStart(EngineLoop)) { Name = "EngineThread" };
        private static int engineDelay = 500;//how long to wait after each cycle of the engine

        //timer to update the neuron values 
        private DispatcherTimer displayUpdateTimer = new DispatcherTimer();

        // if the control key is pressed...used for adding multiple selection areas
        public static bool ctrlPressed = false;
        public static bool shiftPressed = false;

        //the name of the currently-loaded network file
        public static string currentFileName = "";

        public static MainWindow thisWindow;
        Window splashScreen = new SplashScreeen();

        //for autorepeat on the zoom in-out buttons
        DispatcherTimer zoomInOutTimer;
        int zoomAomunt = 0;


        public MainWindow()
        {
            //this puts up a dialog on unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
                {
                    string message = eventArgs.ExceptionObject.ToString();
                    System.Windows.Forms.Clipboard.SetText(message);
                    int i = message.IndexOf("stack trace");
                    if (i > 0)
                        message = message.Substring(0, i + 16);
                    message += "\r\nPROGRAM WILL EXIT  (stack trace copied to clipboard)";
                    MessageBox.Show(message);
                    Application.Current.Shutdown(255);
                };
            InitializeComponent();
            displayUpdateTimer.Tick += DisplayUpdate_TimerTick;
            arrayView = theNeuronArrayView;
            Width = 1100;
            Height = 600;
            slider_ValueChanged(slider, null);

            thisWindow = this;

            splashScreen.Left = 300;
            splashScreen.Top = 300;
            splashScreen.Show();
            DispatcherTimer splashHide = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 3),
            };
            splashHide.Tick += SplashHide_Tick;
            splashHide.Start();

        }

        private void SplashHide_Tick(object sender, EventArgs e)
        {
            Application.Current.MainWindow = this;
            splashScreen.Close();
            ((DispatcherTimer)sender).Stop();
            if (theNeuronArray != null)
            {
                foreach (ModuleView na in theNeuronArray.Modules)
                {
                    if (na.TheModule != null)
                    {
                        na.TheModule.SetDlgOwner(this);
                    }
                }
            }
            OpenHistoryWindow();
        }

        public static void CloseHistoryWindow()
        {
            if (fwWindow != null)
                fwWindow.Close();
        }

        private void OpenHistoryWindow()
        {
            if (Application.Current.MainWindow != this) return;
            if (theNeuronArray != null)
            {
                bool history = false;
                foreach (Neuron n in theNeuronArray.neuronArray)
                {
                    if (n.KeepHistory) history = true;
                }
                if (history)
                    NeuronView.OpenHistoryWindow();
            }
        }


        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            Debug.WriteLine("Window_KeyUp");
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                ctrlPressed = false;
            }
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                shiftPressed = false;
                if (Mouse.LeftButton != MouseButtonState.Pressed)
                    theNeuronArrayView.theCanvas.Cursor = Cursors.Cross;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            Debug.WriteLine("Window_KeyDown");
            if (e.Key == Key.Delete)
            {
                if (theNeuronArrayView.theSelection.selectedRectangles.Count > 0)
                {
                    theNeuronArrayView.DeleteNeurons();
                    theNeuronArrayView.Update();
                }
                else
                {
                    if (theNeuronArray != null)
                    {
                        theNeuronArray.UndoSynapse();
                        theNeuronArrayView.Update();
                    }
                }
            }
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                ctrlPressed = true;
            }
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                shiftPressed = true;
                theNeuronArrayView.theCanvas.Cursor = Cursors.Hand;
            }
            if (e.Key == Key.Escape)
            {
                if (theNeuronArrayView.theSelection.selectedRectangles.Count > 0)
                {
                    theNeuronArrayView.ClearSelection();
                    Update();
                }
            }
            if (ctrlPressed && e.Key == Key.C)
            {
                theNeuronArrayView.CopyNeurons();
            }
            if (ctrlPressed && e.Key == Key.V)
            {
                theNeuronArrayView.PasteNeurons();
            }
            if (ctrlPressed && e.Key == Key.X)
            {
                theNeuronArrayView.CutNeurons();
            }
            if (ctrlPressed && e.Key == Key.Z)
            {
                if (theNeuronArray != null)
                {
                    theNeuronArray.UndoSynapse();
                    theNeuronArrayView.Update();
                }
            }
        }

        private void setTitleBar()
        {
            Title = "Brain Simulator II " + System.IO.Path.GetFileNameWithoutExtension(currentFileName);
        }

        private bool LoadFile(string fileName)
        {
            CloseAllModuleDialogs();
            CloseHistoryWindow();

            theNeuronArrayView.theSelection.selectedRectangles.Clear();
            CloseAllModuleDialogs();

            SuspendEngine();
            try
            {
                // Load the data from the XML to the Brainsim Engine.  
                FileStream file = File.Open(fileName, FileMode.Open);

                XmlSerializer reader = new XmlSerializer(typeof(NeuronArray), GetModuleTypes());
                theNeuronArray = (NeuronArray)reader.Deserialize(file);
                file.Close();

                for (int i = 0; i < theNeuronArray.arraySize; i++)
                    if (theNeuronArray.neuronArray[i] == null)
                        theNeuronArray.neuronArray[i] = new Neuron(i);

                //Update all the synapses to ensure that the synapse-from lists are correct
                foreach (Neuron n in theNeuronArray.neuronArray)
                {
                    foreach (Synapse s in n.Synapses)
                        n.AddSynapse(s.TargetNeuron, s.Weight, theNeuronArray, false);
                }

                theNeuronArray.CheckSynapseArray();
                theNeuronArrayView.Update();
                setTitleBar();
                Task.Delay(1000).ContinueWith(t => ShowDialogs());
                foreach (ModuleView na in theNeuronArray.modules)
                {
                    if (na.TheModule != null)
                        na.TheModule.SetUpAfterLoad();
                }
                if (theNeuronArray.displayParams != null)
                    theNeuronArrayView.Dp = theNeuronArray.displayParams;

                NeuronArrayView.SortAreas();
            }
            catch (Exception e1)
            {
                if (e1.InnerException != null)
                    MessageBox.Show("File Load failed because:\r\n " + e1.Message + "\r\nAnd:\r\n" + e1.InnerException.Message);
                else
                    MessageBox.Show("File Load failed because:\r\n " + e1.Message);
                return false;
            }
            Update();
            SetShowSynapsesCheckBox(theNeuronArray.ShowSynapses);
            OpenHistoryWindow();
            if (!theNeuronArray.hideNotes)
                MenuItemNotes_Click(null, null);
            ResumeEngine();
            return true;
        }

        //
        private void ShowDialogs()
        {
            SuspendEngine();
            foreach (ModuleView na in theNeuronArray.modules)
            {
                if (na.TheModule != null && na.TheModule.dlgIsOpen)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        na.TheModule.ShowDialog();
                    });
                }
            }
            ResumeEngine();
        }

        private void buttonLoad_Click(object sender, RoutedEventArgs e)
        {
            string fileName = (string)(sender as MenuItem).Header;
            if (fileName == "_Open")
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog
                {
                    Filter = "XML Network Files|*.xml",
                    Title = "Select a Brain Simulator File"
                };
                // Show the Dialog.  
                // If the user clicked OK in the dialog and  
                Nullable<bool> result = openFileDialog1.ShowDialog();
                if (result ?? false)
                {
                    currentFileName = openFileDialog1.FileName;
                    bool loadSuccessful = LoadFile(currentFileName);
                    if (!loadSuccessful)
                    {
                        currentFileName = "";
                    }
                    Properties.Settings.Default["CurrentFile"] = currentFileName;
                    Properties.Settings.Default.Save();
                }
            }
            else
            {
                currentFileName = Path.GetFullPath("./Networks/" + fileName + ".xml");
                bool loadSuccessful = LoadFile(currentFileName);
                if (!loadSuccessful)
                {
                    currentFileName = "";
                }
                Properties.Settings.Default["CurrentFile"] = currentFileName;
                Properties.Settings.Default.Save();
            }
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            if (currentFileName == "")
            {
                buttonSaveAs_Click(null, null);
            }
            else
            {
                SaveFile(currentFileName);
            }
        }

        //this is the set of moduletypes that the xml serializer will save
        private Type[] GetModuleTypes()
        {
            Type[] listOfBs = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                               from assemblyType in domainAssembly.GetTypes()
                               where assemblyType.IsSubclassOf(typeof(ModuleBase))
                               //                               where typeof(ModuleBase).IsAssignableFrom(assemblyType)
                               select assemblyType).ToArray();
            List<Type> list = new List<Type>();
            for (int i = 0; i < listOfBs.Length; i++)
                list.Add(listOfBs[i]);
            return list.ToArray();
        }

        private void SaveFile(string fileName)
        {
            SuspendEngine();
            string tempFile = System.IO.Path.GetTempFileName();
            FileStream file = File.Create(tempFile);

            foreach (ModuleView na in theNeuronArray.modules)
            {
                if (na.TheModule != null)
                    na.TheModule.SetUpBeforeSave();
            }

            //hide unused neurons to save on file size
            for (int i = 0; i < theNeuronArray.arraySize; i++)
                if (!theNeuronArray.neuronArray[i].InUse() && theNeuronArray.neuronArray[i].Model == Neuron.modelType.Std)
                    theNeuronArray.neuronArray[i] = null;
            //Save the data from the network (NeuronArray and modules) to the file
            try
            {
                theNeuronArray.displayParams = theNeuronArrayView.Dp;
                XmlSerializer writer = new XmlSerializer(typeof(NeuronArray), GetModuleTypes());
                writer.Serialize(file, theNeuronArray);
                file.Close();
                currentFileName = fileName;
                Properties.Settings.Default["CurrentFile"] = currentFileName;
                Properties.Settings.Default.Save();
                File.Copy(tempFile, currentFileName, true);
                File.Delete(tempFile);
            }
            catch (Exception e1)
            {
                MessageBox.Show("Save Failed because: " + e1.Message + "\r\n" + e1.InnerException.Message);
                if (File.Exists(tempFile))
                {
                    file.Close();
                    File.Delete(tempFile);
                }
            }

            //restore unused neurons 
            for (int i = 0; i < theNeuronArray.arraySize; i++)
                if (theNeuronArray.neuronArray[i] == null)
                    theNeuronArray.neuronArray[i] = new Neuron(i);

            ResumeEngine();
        }

        private void buttonSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Filter = "XML Network Files|*.xml",
                Title = "Select a Brain Simulator File"
            };

            // Show the Dialog.  
            // If the user clicked OK in the dialog and  
            Nullable<bool> result = saveFileDialog1.ShowDialog();
            if (result ?? false)// System.Windows.Forms.DialogResult.OK)
            {
                SaveFile(saveFileDialog1.FileName);
                setTitleBar();
            }
        }

        private void button_ClipboardSave_Click(object sender, RoutedEventArgs e)
        {
            if (theNeuronArrayView.myClipBoard == null) return;
            if (theNeuronArrayView.theSelection.GetSelectedNeuronCount() < 1) return;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Filter = "XML Network Files|*.xml",
                Title = "Select a Brain Simulator File"
            };

            // Show the Dialog.  
            // If the user clicked OK in the dialog and  
            Nullable<bool> result = saveFileDialog1.ShowDialog();
            if (result ?? false)// System.Windows.Forms.DialogResult.OK)
            {
                //Save the data from the NeuronArray to the file
                System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(typeof(NeuronArray));
                System.IO.FileStream file = System.IO.File.Create(saveFileDialog1.FileName);
                writer.Serialize(file, theNeuronArrayView.myClipBoard);
                file.Close();
            }
        }
        private void button_FileNew_Click(object sender, RoutedEventArgs e)
        {
            NewArrayDlg dlg = new NewArrayDlg();
            dlg.ShowDialog();
            if (dlg.returnValue)
            {
                SuspendEngine();
                theNeuronArrayView.Update();
                currentFileName = "";
                Properties.Settings.Default["CurrentFile"] = currentFileName;
                Properties.Settings.Default.Save();
                setTitleBar();
                MenuItemNotes_Click(null, null);
                ResumeEngine();
            }
        }
        private void button_Exit_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void button_PatternLoad_Click(object sender, RoutedEventArgs e)
        {
            if (theNeuronArrayView.targetNeuronIndex < 0) return;

            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "XML Network Files|*.xml",
                Title = "Select a Brain Simulator File"
            };

            // Show the Dialog.  
            // If the user clicked OK in the dialog and  
            Nullable<bool> result = openFileDialog1.ShowDialog();
            if (result ?? false)
            {
                // Load the data from the XML to the Brainsim NeuronArray.  
                FileStream file = File.Open(openFileDialog1.FileName, FileMode.Open);
                XmlSerializer reader = new XmlSerializer(typeof(NeuronArray));
                theNeuronArrayView.myClipBoard = (NeuronArray)reader.Deserialize(file);
                file.Close();
                //restore unused neurons to save on storage space
                for (int i = 0; i < theNeuronArrayView.myClipBoard.arraySize; i++)
                    if (theNeuronArrayView.myClipBoard == null)
                        theNeuronArrayView.myClipBoard.neuronArray[i] = new Neuron(i);
            }
            theNeuronArrayView.PasteNeurons(true);
            theNeuronArrayView.Update();
        }

        //this should be a module It's not currently used
        private void Button_PatternFileLoad_Click(object sender, RoutedEventArgs e)
        {
            if (theNeuronArrayView.targetNeuronIndex < 0) return;
            int firstNeuron = theNeuronArrayView.targetNeuronIndex;
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "BMP Files|*.bmp",
                Title = "Select a Brain Simulator pattern File"
            };

            // Show the Dialog.  
            // If the user clicked OK in the dialog and  
            Nullable<bool> result = openFileDialog1.ShowDialog();
            if (result ?? true)// System.Windows.Forms.DialogResult.OK)
            {
                Bitmap bitmap1 = new Bitmap(openFileDialog1.FileName);

                //for (int i = 0; i < bitmap1.Height; i++)
                //    for (int j = 0; j < bitmap1.Width; j++)
                //    {
                //        int i1 = i / 5;
                //        int j1 = j / 5;
                //        int neuronIndex = firstNeuron + i1 + j1 * theNeuronArrayView.dp.NeuronRows;
                //        if (neuronIndex >= theNeuronArray.arraySize) return;
                //        Neuron n = theNeuronArray.neuronArray[neuronIndex];
                //        System.Drawing.Color c = bitmap1.GetPixel(j, i);
                //        if (c.R != 255 || c.G != 255 || c.B != 255)
                //        {
                //            n.CurrentCharge = n.LastCharge = 1;
                //        }
                //        else
                //        {
                //            n.CurrentCharge = n.LastCharge = 0;
                //        }
                //    }
            }
        }

        //        static int oldEngineDelay = 0;
        public static void SuspendEngine()
        {
            if (engineDelay == 2000) return; //already suspended
            //suspend the engine...
            if (MainWindow.theNeuronArray != null)
                MainWindow.theNeuronArray.EngineSpeed = engineDelay;
            //            oldEngineDelay = engineDelay;
            engineDelay = 2000;
            while (!engineIsWaiting)
                Thread.Sleep(100);
        }
        public static void ResumeEngine()
        {
            //resume the engine
            //            engineDelay = oldEngineDelay;
            engineDelay = MainWindow.theNeuronArray.EngineSpeed;
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                MainWindow.thisWindow.SetSliderPosition(engineDelay);
            });
        }

        static bool engineIsWaiting = false;
        private static void EngineLoop()
        {
            while (true)
            {
                if (engineDelay > 1000)
                {
                    engineIsWaiting = true;
                    Thread.Sleep(100); //check the engineDelay every 100 ms.
                }
                else
                {
                    engineIsWaiting = false;
                    if (theNeuronArray != null)
                        theNeuronArray.Fire();
                    Thread.Sleep(Math.Abs(engineDelay));
                }
            }
        }

        private void SetSliderPosition(int interval)
        {
            if (interval == 0) slider.Value = 10;
            else if (interval <= 1) slider.Value = 9;
            else if (interval <= 2) slider.Value = 8;
            else if (interval <= 5) slider.Value = 7;
            else if (interval <= 10) slider.Value = 6;
            else if (interval <= 50) slider.Value = 5;
            else if (interval <= 100) slider.Value = 4;
            else if (interval <= 250) slider.Value = 3;
            else if (interval <= 500) slider.Value = 2;
            else if (interval <= 1000) slider.Value = 1;
            else slider.Value = 0;
        }

        //Set the engine speed
        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //            if (theNeuronArray == null) return;
            Slider s = sender as Slider;
            int value = (int)s.Value;
            int Interval = 0;
            if (value == 0) Interval = 1000;
            if (value == 1) Interval = 1000;
            if (value == 2) Interval = 500;
            if (value == 3) Interval = 250;
            if (value == 4) Interval = 100;
            if (value == 5) Interval = 50;
            if (value == 6) Interval = 10;
            if (value == 7) Interval = 5;
            if (value == 8) Interval = 2;
            if (value == 9) Interval = 1;
            if (value > 9)
                Interval = 0;
            engineDelay = Interval;
            if (!engineThread.IsAlive)
                engineThread.Start();
            displayUpdateTimer.Interval = TimeSpan.FromMilliseconds(100);
            displayUpdateTimer.Start();
        }

        bool disaplayUpdating = false;
        private void DisplayUpdate_TimerTick(object sender, EventArgs e)
        {
            if (disaplayUpdating) return;
            if (theNeuronArray == null) return;
            disaplayUpdating = true;
            if (engineDelay > 1000 || theNeuronArray == null)
            {
                label.Content = "Not Running   " + theNeuronArray.Generation;
            }
            else
            {
                theNeuronArrayView.UpdateNeuronColors();
                label.Content = "Running, Speed: " + slider.Value + "   " + theNeuronArray.Generation;
            }
            disaplayUpdating = false;
        }


        //Enable/disable menu item specified by "Entry"...pass in the Menu.Items as the root to search
        private void EnableMenuItem(ItemCollection mm, string Entry, bool enabled)
        {
            foreach (Object m1 in mm)
            {
                if (m1.GetType() == typeof(MenuItem))
                {
                    MenuItem m = (MenuItem)m1;
                    if ((string)m.Header == Entry)
                    {
                        m.IsEnabled = enabled;
                        return;
                    }
                    else
                        EnableMenuItem(m.Items, Entry, enabled);
                }
            }

            return;
        }

        //this enables and disables various menu entries based on circumstances
        private void MainMenu_MouseEnter(object sender, MouseEventArgs e)
        {
            if (theNeuronArray == null)
            {
                EnableMenuItem(MainMenu.Items, "_Save", false);
                EnableMenuItem(MainMenu.Items, "Save _As", false);
                EnableMenuItem(MainMenu.Items, "_Insert", false);
                EnableMenuItem(MainMenu.Items, "_Properties", false);
                EnableMenuItem(MainMenu.Items, "_Notes", false);
            }
            else
            {
                EnableMenuItem(MainMenu.Items, "_Save", true);
                EnableMenuItem(MainMenu.Items, "Save _As", true);
                EnableMenuItem(MainMenu.Items, "_Insert", true);
                EnableMenuItem(MainMenu.Items, "_Properties", true);
                EnableMenuItem(MainMenu.Items, "_Notes", true);
            }
            if (theNeuronArrayView.theSelection.selectedRectangles.Count == 0)
            {
                EnableMenuItem(MainMenu.Items, " Cut", false);
                EnableMenuItem(MainMenu.Items, " Copy", false);
                EnableMenuItem(MainMenu.Items, " Delete", false);
                EnableMenuItem(MainMenu.Items, " Move", false);
            }
            else
            {
                EnableMenuItem(MainMenu.Items, " Cut", true);
                EnableMenuItem(MainMenu.Items, " Copy", true);
                EnableMenuItem(MainMenu.Items, " Delete", true);
                EnableMenuItem(MainMenu.Items, " Move", true);
            }
            if (theNeuronArrayView.myClipBoard == null)
            {
                EnableMenuItem(MainMenu.Items, " Paste", false);
                EnableMenuItem(MainMenu.Items, " Save Selection to File", false);
            }
            else
            {
                EnableMenuItem(MainMenu.Items, " Paste", true);
                EnableMenuItem(MainMenu.Items, " Save Selection to File", true);
            }
        }

        static public void UpdateDisplayLabel(int zoomLevel, int firedCount)
        {
            thisWindow.labelDisplayStatus.Content = "Zoom Level: " + zoomLevel + ",  " + firedCount + " Neurons Fired";
        }


        public static void Update()
        {
            arrayView.Update();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (theNeuronArray != null)
            {
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
                    e.Cancel = true;
                }
                else
                {
                    SuspendEngine();
                    engineThread.Abort();

                    CloseAllModuleDialogs();
                }
            }
            else
            {
                engineThread.Abort();
            }
        }

        public static void CloseAllModuleDialogs()
        {
            if (theNeuronArray != null)
            {
                foreach (ModuleView na in theNeuronArray.Modules)
                {
                    if (na.TheModule != null)
                    {
                        na.TheModule.CloseDlg();
                    }
                }
            }
        }

        private void MenuItem_MoveNeurons(object sender, RoutedEventArgs e)
        {
            theNeuronArrayView.MoveNeurons();
        }
        private void MenuItem_CutNeurons(object sender, RoutedEventArgs e)
        {
            theNeuronArrayView.CutNeurons();
        }
        private void MenuItem_CopyNeurons(object sender, RoutedEventArgs e)
        {
            theNeuronArrayView.CopyNeurons();
        }
        private void MenuItem_PasteNeurons(object sender, RoutedEventArgs e)
        {
            theNeuronArrayView.PasteNeurons();
        }
        private void MenuItem_DeleteNeurons(object sender, RoutedEventArgs e)
        {
            theNeuronArrayView.DeleteNeurons();
        }
        private void Button_HelpAbout_Click(object sender, RoutedEventArgs e)
        {
            HelpAbout dlg = new HelpAbout();
            dlg.ShowDialog();
        }

        //this reloads the file which was being used on the previous run of the program
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                currentFileName = (string)Properties.Settings.Default["CurrentFile"];
                if (currentFileName != "")
                {
                    SuspendEngine();
                    bool loadSuccessful = LoadFile(currentFileName);
                    if (!loadSuccessful)
                        currentFileName = "";
                    setTitleBar();
                    ResumeEngine();
                }
                else
                {
                    bool showHelp = (bool)Properties.Settings.Default["ShowHelp"];
                    if (showHelp)
                        MenuItemHelp_Click(null, null);
                }
            }
            //various errors might have happened so we'll just ignore them all and start with a fresh file 
            catch (Exception e1)
            { }
        }

        private void ButtonInit_Click(object sender, RoutedEventArgs e)
        {
            if (theNeuronArray == null) return;
            SuspendEngine();
            foreach (ModuleView na in theNeuronArray.Modules)
            {
                if (na.TheModule != null)
                    na.TheModule.Init(true);
            }
            theNeuronArrayView.Update();
            ResumeEngine();
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (imagePause.Visibility == Visibility.Visible)
            {
                imagePause.Visibility = Visibility.Collapsed;
                imagePlay.Visibility = Visibility.Visible;
                SuspendEngine();
                theNeuronArrayView.UpdateNeuronColors();
            }
            else
            {
                imagePause.Visibility = Visibility.Visible;
                imagePlay.Visibility = Visibility.Collapsed;
                ResumeEngine();
            }
        }

        private void ButtonSingle_Click(object sender, RoutedEventArgs e)
        {
            if (theNeuronArray != null)
            {
                if (!engineIsWaiting)
                {
                    imagePause.Visibility = Visibility.Collapsed;
                    imagePlay.Visibility = Visibility.Visible;
                    SuspendEngine();
                    theNeuronArrayView.UpdateNeuronColors();
                }
                else
                {
                    theNeuronArray.Fire();
                    theNeuronArrayView.UpdateNeuronColors();
                }
            }
        }

        private void ButtonPan_Click(object sender, RoutedEventArgs e)
        {
            theNeuronArrayView.theCanvas.Cursor = Cursors.Hand;
        }

        private void ButtonZoomIn_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartZoom(3);
        }

        private void ButtonZoomOut_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartZoom(-3);
        }

        private void StartZoom(int amount)
        {
            zoomInOutTimer = new DispatcherTimer();
            zoomInOutTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);
            zoomInOutTimer.Tick += ZoomInOutTimer_Tick;
            CaptureMouse();
            zoomAomunt = amount;
            theNeuronArrayView.Zoom(zoomAomunt);
            zoomInOutTimer.Start();
        }

        private void ZoomInOutTimer_Tick(object sender, EventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                theNeuronArrayView.Zoom(zoomAomunt);
            }
            else
            {
                zoomInOutTimer.Stop();
                ReleaseMouseCapture();
            }
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            ////activate this windoe if it is not activated and not of the child dialogs are activated
            //if (theNeuronArray != null)
            //{
            //    foreach (ModuleView na in theNeuronArray.Modules)
            //    {
            //        if (na.TheModule != null)
            //        {
            //            na.TheModule.Activate();
            //        }
            //    }
            //}
            Activate();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            theNeuronArray.ShowSynapses = true;
            Update();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            theNeuronArray.ShowSynapses = false;
            Update();
        }

        private void SetShowSynapsesCheckBox(bool showSynapses)
        {
            checkBox.IsChecked = showSynapses;
        }

        private void MenuItemProperties_Click(object sender, RoutedEventArgs e)
        {
            PropertiesDlg p = new PropertiesDlg();
            try
            {
                p.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Properties could not be displayed");
            }
        }

        private void MenuItemNotes_Click(object sender, RoutedEventArgs e)
        {
            NotesDialog p = new NotesDialog();
            p.Topmost = true;
            try
            {
                p.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Notes could not be displayed");
            }
        }

        private void MenuItemHelp_Click(object sender, RoutedEventArgs e)
        {
            Help p = new Help();
            try
            {
                p.Owner = this;
                p.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Help could not be displayed");
            }
        }
    }
}
