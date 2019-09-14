using Microsoft.Win32;
//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

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
using System.Windows.Shapes;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Threading;
using System.Diagnostics;
using System.Threading;
using System.IO.MemoryMappedFiles;
using System.Drawing;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        static NeuronArrayView arrayView = null;
        public static NeuronArray theNeuronArray = null;

        Thread engineThread = new Thread(new ThreadStart(EngineLoop));
        private static int engineDelay = 500;//how long to wait after each cycle of the engine

        //timer to update the neuron values 
        private DispatcherTimer displayUpdateTimer = new DispatcherTimer();

        // if the control key is pressed...used for adding multiple selection areas
        public static bool crtlPressed = false;

        //the name of the currently-loaded network file
        public static string currentFileName = "";

        public static MainWindow thisWindow;
        Window splashScreen = new SplashScreeen();
        public MainWindow()
        {
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
            splashScreen.Close();
            ((DispatcherTimer)sender).Stop();
            if (theNeuronArray != null) foreach (NeuronArea na in theNeuronArray.Areas)
                {
                    if (na.TheModule != null)
                    {
                        na.TheModule.SetDlgOwner(this);
                    }
                }
            Application.Current.MainWindow = this;
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            Debug.WriteLine("Window_KeyUp");
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                crtlPressed = false;
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
                    theNeuronArray.UndoSynapse();
                    theNeuronArrayView.Update();
                }
            }
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                crtlPressed = true;
            }
            if (e.Key == Key.Escape)
            {
                if (theNeuronArrayView.theSelection.selectedRectangles.Count > 0)
                {
                    theNeuronArrayView.ClearSelection();
                }
            }
            if (crtlPressed && e.Key == Key.C)
            {
                theNeuronArrayView.CopyNeurons();
            }
            if (crtlPressed && e.Key == Key.V)
            {
                theNeuronArrayView.PasteNeurons();
            }
            if (crtlPressed && e.Key == Key.X)
            {
                theNeuronArrayView.CutNeurons();
            }
        }


        private void setTitleBar()
        {
            this.Title = "Brain Simulator II " + System.IO.Path.GetFileNameWithoutExtension(currentFileName);
        }

        private void LoadFile(string fileName)
        {

            theNeuronArrayView.theSelection.selectedRectangles.Clear();

            Thread.Sleep(1);

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
                foreach (Synapse s in n.Synapses)
                    n.AddSynapse(s.TargetNeuron, s.Weight, theNeuronArray, false);
            theNeuronArray.CheckSynapseArray();
            theNeuronArrayView.Update();
            setTitleBar();
            Task.Delay(1000).ContinueWith(t => ShowDialogs());
            foreach(NeuronArea na in theNeuronArray.areas)
            {
                if (na.TheModule != null)
                    na.TheModule.SetUpAfterLoad();
            }
        }

        private void ShowDialogs()
        {
            SuspendEngine();
            foreach (NeuronArea na in theNeuronArray.areas)
            {
                if (na.TheModule != null)
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
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "XML Network Files|*.xml",
                Title = "Select a Brain Simulator File"
            };

            // Show the Dialog.  
            // If the user clicked OK in the dialog and  
            Nullable<bool> result = openFileDialog1.ShowDialog();
            if (result ?? false)// System.Windows.Forms.DialogResult.OK)
            {
                currentFileName = openFileDialog1.FileName;
                Properties.Settings.Default["CurrentFile"] = currentFileName;
                Properties.Settings.Default.Save();
                LoadFile(currentFileName);
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

            foreach (NeuronArea na in theNeuronArray.areas)
            {
                if (na.TheModule != null)
                    na.TheModule.SetUpBeforeSave();
            }

            //hide unused neurons to save on file size
            for (int i = 0; i < theNeuronArray.arraySize; i++)
                if (!theNeuronArray.neuronArray[i].InUse() && theNeuronArray.neuronArray[i].Model == Neuron.modelType.Std)
                    theNeuronArray.neuronArray[i] = null;
            //Save the data from the Brainsim Engine to the file
            try
            {
                XmlSerializer writer = new XmlSerializer(typeof(NeuronArray), GetModuleTypes());
                //FileStream file = File.Create(tempFile);
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
                //Save the data from the Brainsim Engine to the file
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
                theNeuronArrayView.Update();
                currentFileName = "";
                Properties.Settings.Default["CurrentFile"] = currentFileName;
                Properties.Settings.Default.Save();
                setTitleBar();
            }
        }
        private void button_Exit_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
        }
        private void button_Undo_Click(object sender, RoutedEventArgs e)
        {

        }
        private void button_NameSelection_Click(object sender, RoutedEventArgs e)
        {
            if (theNeuronArrayView.theSelection.GetSelectedNeuronCount() > 0)
            {

            }
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
            if (result ?? false)// System.Windows.Forms.DialogResult.OK)
            {
                // Load the data from the XML to the Brainsim Engine.  
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

        static int oldEngineDelay = 0;
        public static
            void SuspendEngine()
        {
            if (engineDelay == 2000) return;
            //suspend the engine...
            oldEngineDelay = engineDelay;
            engineDelay = 2000;
            while (!engineIsWaiting)
                Thread.Sleep(100);
        }
        public static void ResumeEngine()
        {
            //resume the engine
            engineDelay = oldEngineDelay;
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

        private void MainMenu_MouseEnter(object sender, MouseEventArgs e)
        {
            if (theNeuronArrayView.theSelection.selectedRectangles.Count > 0)
            {
                EnableMenuItem(MainMenu.Items, " Cut", false);
            }
            else
            {
                EnableMenuItem(MainMenu.Items, " Cut", true);
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

        static public RealitySimulator realSim = null;
        static public CameraHandler theCameraWindow = null;
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
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
                engineThread.Abort();


                if (realSim != null)
                    realSim.Close();
                if (theCameraWindow != null)
                    theCameraWindow.Close();
                if (theNeuronArray != null)
                {
                    foreach (NeuronArea na in theNeuronArray.Areas)
                    {
                        if (na.TheModule != null)
                        {
                            na.TheModule.CloseDlg();
                        }
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


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                currentFileName = (string)Properties.Settings.Default["CurrentFile"];
                if (currentFileName != "")
                {
                    LoadFile(currentFileName);
                    setTitleBar();
                }
            }
            catch (Exception e1)
            { }
        }



        private void ButtonInit_Click(object sender, RoutedEventArgs e)
        {
            SuspendEngine();
            foreach (NeuronArea na in theNeuronArray.Areas)
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

        private void ButtonDisplay_Click(object sender, RoutedEventArgs e)
        {
            theNeuronArrayView.Origin();
            double height = theNeuronArrayView.ActualHeight;
            theNeuronArrayView.Dp.NeuronDisplaySize = (int)height / theNeuronArrayView.Dp.NeuronRows;
            if (theNeuronArrayView.Dp.NeuronDisplaySize < 1)
                theNeuronArrayView.Dp.NeuronDisplaySize = 1;
            Update();
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
    }
}
