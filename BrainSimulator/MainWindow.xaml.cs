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
using System.Windows.Threading;

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
        //for cut-copy-paste
        public static NeuronArray myClipBoard = null; //refactor back to private

        public static FiringHistoryWindow fwWindow = null;
        public static NotesDialog notesWindow = null;

        Thread engineThread;

        public static bool useServers = false;

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

        public static bool showSynapses = false;


        public MainWindow()
        {
            //this puts up a dialog on unhandled exceptions
#if !DEBUG
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
#endif
            engineThread = new Thread(new ThreadStart(EngineLoop)) { Name = "EngineThread" };

            InitializeComponent();

            engineThread.Priority = ThreadPriority.Lowest;

            displayUpdateTimer.Tick += DisplayUpdate_TimerTick;
            displayUpdateTimer.Interval = TimeSpan.FromMilliseconds(100);
            displayUpdateTimer.Start();

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
            if (Properties.Settings.Default.UpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
            }
        }
        private void SplashHide_Tick(object sender, EventArgs e)
        {
            Application.Current.MainWindow = this;
            splashScreen.Close();
            ((DispatcherTimer)sender).Stop();
            if (theNeuronArray != null)
            {
                lock (theNeuronArray.Modules)
                {
                    foreach (ModuleView na in theNeuronArray.Modules)
                    {
                        if (na.TheModule != null)
                        {
                            na.TheModule.SetDlgOwner(this);
                        }
                    }
                }
            }
            if (currentFileName == "")
            {
                bool showHelp = (bool)Properties.Settings.Default["ShowHelp"];
                if (showHelp)
                    MenuItemHelp_Click(null, null);
            }
            OpenHistoryWindow();
        }

        public static void CloseHistoryWindow()
        {
            if (fwWindow != null)
                fwWindow.Close();
            FiringHistory.history.Clear();
            FiringHistory.Clear();
        }

        private void OpenHistoryWindow()
        {
            return;
            //if (Application.Current.MainWindow != this) return;
            //if (theNeuronArray != null)
            //{
            //    bool history = false;
            //    foreach (Neuron n in theNeuronArray.Neurons())
            //    {
            //        if (n.KeepHistory)
            //            history = true;
            //    }
            //    if (history)
            //        NeuronView.OpenHistoryWindow();
            //}
        }


        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            //Debug.WriteLine("Window_KeyUp");
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
            //Debug.WriteLine("Window_KeyDown");
            if (e.Key == Key.Delete)
            {
                if (theNeuronArrayView.theSelection.selectedRectangles.Count > 0)
                {
                    theNeuronArrayView.DeleteSelection();
                    theNeuronArrayView.ClearSelection();
                    Update();
                }
                else
                {
                    if (theNeuronArray != null)
                    {
                        theNeuronArray.Undo();
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
            if (ctrlPressed && e.Key == Key.M)
            {
                theNeuronArrayView.MoveNeurons();
            }
            if (ctrlPressed && e.Key == Key.Z)
            {
                if (theNeuronArray != null)
                {
                    theNeuronArray.Undo();
                    theNeuronArrayView.Update();
                }
            }
        }

        private void setTitleBar()
        {
            Title = "Brain Simulator II " + System.IO.Path.GetFileNameWithoutExtension(currentFileName);
        }

        private async void LoadFile(string fileName)
        {
            CloseAllModuleDialogs();
            CloseHistoryWindow();
            CloseNotesWindow();
            theNeuronArrayView.theSelection.selectedRectangles.Clear();
            CloseAllModuleDialogs();
            SuspendEngine();

            await Task.Run(delegate { XmlFile.Load(ref theNeuronArray, fileName); });

            currentFileName = fileName;

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
            OpenHistoryWindow();
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

        private void SaveFile(string fileName)
        {
            SuspendEngine();
            foreach (ModuleView na in theNeuronArray.modules)
            {
                if (na.TheModule != null)
                    na.TheModule.SetUpBeforeSave();
            }

            theNeuronArray.displayParams = theNeuronArrayView.Dp;
            if (XmlFile.Save(theNeuronArray, fileName))
                currentFileName = fileName;

            ResumeEngine();
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
            if (!theNeuronArray.hideNotes && theNeuronArray.networkNotes != "")
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    MenuItemNotes_Click(null, null);
                });
            ResumeEngine();
        }
        private void MRUListItem_Click(object sender, RoutedEventArgs e)
        {
            if (PromptToSaveChanges())
            { }
            else
            {
                currentFileName = (string)(sender as MenuItem).ToolTip;
                LoadCurrentFile();
            }
        }
        private void LoadMRUMenu()
        {
            MRUListMenu.Items.Clear();
            StringCollection MRUList = (StringCollection)Properties.Settings.Default["MRUList"];
            if (MRUList == null)
                MRUList = new StringCollection();
            foreach (string fileItem in MRUList)
            {
                string shortName = Path.GetFileNameWithoutExtension(fileItem);
                MenuItem mi = new MenuItem() { Header = shortName };
                mi.Click += MRUListItem_Click;
                mi.ToolTip = fileItem;
                MRUListMenu.Items.Add(mi);
            }
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

        private void buttonLoad_Click(object sender, RoutedEventArgs e)
        {
            if (PromptToSaveChanges())
            { }
            else
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
                        LoadCurrentFile();
                    }
                }
                else
                {
                    //this is a file name from the File menu
                    currentFileName = Path.GetFullPath("./Networks/" + fileName + ".xml");
                    LoadCurrentFile();
                }
            }
        }

        private void LoadCurrentFile()
        {
            LoadFile(currentFileName);
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
                AddFileToMRUList(currentFileName);
                setTitleBar();
            }
        }

        private void button_ClipboardSave_Click(object sender, RoutedEventArgs e)
        {
            if (myClipBoard == null) return;
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
                SaveClipboardToFile(saveFileDialog1.FileName);
            }
        }
        private void button_FileNew_Click(object sender, RoutedEventArgs e)
        {
            if (PromptToSaveChanges())
            { } //cancel the operation
            else
            {
                SuspendEngine();
                NewArrayDlg dlg = new NewArrayDlg();
                dlg.ShowDialog();
                if (dlg.returnValue)
                {
                    Update();
                    currentFileName = "";
                    Properties.Settings.Default["CurrentFile"] = currentFileName;
                    Properties.Settings.Default.Save();
                    setTitleBar();
                    if (theNeuronArray.networkNotes != "")
                        MenuItemNotes_Click(null, null);
                }
            }
            ResumeEngine();
        }
        private void button_Exit_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void button_LoadClipboard_Click(object sender, RoutedEventArgs e)
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
                LoadClipBoardFromFile(openFileDialog1.FileName);
            }
            if (theNeuronArrayView.targetNeuronIndex < 0) return;

            theNeuronArrayView.PasteNeurons();
            theNeuronArrayView.Update();
        }


        public static void SuspendEngine()
        {
            if (engineIsPaused) return; //already suspended
                                             //suspend the engine...
            if (theNeuronArray != null)
                theNeuronArray.EngineSpeed = engineDelay;
            engineDelay = 2000;
            while (theNeuronArray != null && !engineIsPaused)
            {
                Thread.Sleep(100);
                System.Windows.Forms.Application.DoEvents();
            }
        }

        public static void ResumeEngine()
        {
            //resume the engine
            if (theNeuronArray != null && !theNeuronArray.EngineIsPaused)
            {
                engineDelay = MainWindow.theNeuronArray.EngineSpeed;
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    MainWindow.thisWindow.SetSliderPosition(engineDelay);
                });
            }
        }

        public void SetProgress(float value)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                if (value != -1)
                {
                    progressBar.Visibility = Visibility.Visible;
                    progressBar.Value = value;
                }
                else
                    progressBar.Visibility = Visibility.Hidden;
            });

        }
        static bool engineIsPaused = false;
        static long engineElapsed = 0;
        static long displayElapsed = 0;
        private void EngineLoop()
        {
            while (true)
            {
                if (theNeuronArray == null)
                {
                    Thread.Sleep(100);
                }
                else if (engineDelay > 1000)
                {
                    engineIsPaused = true;
                    if (updateDisplay)
                    {
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            labelEngineStatus.Content = "Not Running   Cycle: " + theNeuronArray.Generation.ToString("N0");
                        });
                        updateDisplay = false;
                        displayUpdateTimer.Start();
                    }
                    Thread.Sleep(100); //check the engineDelay every 100 ms.
                }
                else
                {
                    engineIsPaused = false;
                    if (theNeuronArray != null)
                    {
                        long start = Utils.GetPreciseTime();
                        theNeuronArray.Fire();
                        long end = Utils.GetPreciseTime();
                        engineElapsed = end - start;

                        if (updateDisplay)
                        {
                            Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                long dStart = Utils.GetPreciseTime();
                                theNeuronArrayView.UpdateNeuronColors();
                                long dEnd = Utils.GetPreciseTime();
                                displayElapsed = dEnd - dStart;
                            });
                            updateDisplay = false;
                            displayUpdateTimer.Start();
                        }
                    }
                    Thread.Sleep(Math.Abs(engineDelay));
                }
            }
        }

        //engine Refractory up/dn  buttons on the menu
        private void Button_RefractoryUpClick(object sender, RoutedEventArgs e)
        {
            theNeuronArray.RefractoryDelay++;
            Refractory.Text = theNeuronArray.RefractoryDelay.ToString();
        }

        private void Button_RefractoryDnClick(object sender, RoutedEventArgs e)
        {
            theNeuronArray.RefractoryDelay--;
            if (theNeuronArray.RefractoryDelay < 0) theNeuronArray.RefractoryDelay = 0;
            Refractory.Text = theNeuronArray.RefractoryDelay.ToString();
        }
        //engine speed up/dn  buttons on the menu
        private void Button_EngineSpeedUpClick(object sender, RoutedEventArgs e)
        {
            slider.Value += 1;
            slider_ValueChanged(slider, null);
        }

        private void Button_EngineSpeedDnClick(object sender, RoutedEventArgs e)
        {
            slider.Value -= 1;
            slider_ValueChanged(slider, null);
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
            EngineSpeed.Text = slider.Value.ToString();
        }


        //Set the engine speed
        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
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
            EngineSpeed.Text = slider.Value.ToString();
            displayUpdateTimer.Start();
        }

        static bool updateDisplay = false;
        private void DisplayUpdate_TimerTick(object sender, EventArgs e)
        {
            updateDisplay = true;

            //this hack is here so that the shift key can be trapped before the window is activated
            //which is important for debugging so the zoom/pan will work on the first try
            if ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) && !shiftPressed && mouseInWindow)
            {
                Debug.WriteLine("Left Shift Pressed in display timer");
                shiftPressed = true;
                theNeuronArrayView.theCanvas.Cursor = Cursors.Hand;
                Activate();
            }
            else if ((Keyboard.IsKeyUp(Key.LeftShift) && Keyboard.IsKeyUp(Key.RightShift)) && shiftPressed && mouseInWindow)
            {
                Debug.WriteLine("Left Shift released in display timer");
                shiftPressed = false;
                theNeuronArrayView.theCanvas.Cursor = Cursors.Cross;
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

        //this enables and disables various menu entries based on circumstances
        private void MainMenu_MouseEnter(object sender, MouseEventArgs e)
        {
            LoadMRUMenu();
            if (theNeuronArray != null) ThreadCount.Text = theNeuronArray.GetThreadCount().ToString();
            else ThreadCount.Text = "";
            if (theNeuronArray != null) Refractory.Text = theNeuronArray.GetRefractoryDelay().ToString();
            else Refractory.Text = "";
            if (!engineIsPaused)
            {
                EnableMenuItem(MainMenu.Items, "Run", false);
                EnableMenuItem(MainMenu.Items, "Pause", true);
            }
            else
            {
                EnableMenuItem(MainMenu.Items, "Run", true);
                EnableMenuItem(MainMenu.Items, "Pause", false);
            }
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
                EnableMenuItem(MainMenu.Items, " Clear Selection", false);
            }
            else
            {
                EnableMenuItem(MainMenu.Items, " Cut", true);
                EnableMenuItem(MainMenu.Items, " Copy", true);
                EnableMenuItem(MainMenu.Items, " Delete", true);
                if (arrayView.targetNeuronIndex < 0)    
                    EnableMenuItem(MainMenu.Items, " Move", false);
                else
                    EnableMenuItem(MainMenu.Items, " Move", true);
                EnableMenuItem(MainMenu.Items, " Clear Selection", true);
            }
            if (arrayView.targetNeuronIndex < 0 || myClipBoard  == null)
            {
                EnableMenuItem(MainMenu.Items, " Paste", false);
            }
            else
            {
                EnableMenuItem(MainMenu.Items, " Paste", true);
            }
            if (theNeuronArray.UndoPossible())
            {
                EnableMenuItem(MainMenu.Items, " Undo", true);
            }
            else
            {
                EnableMenuItem(MainMenu.Items, " Undo", false);

            }

            if (myClipBoard == null)
            {
                EnableMenuItem(MainMenu.Items, "Save Clipboard", false);
            }
            else
            {
                EnableMenuItem(MainMenu.Items, "Save Clipboard", true);
            }
        }

        static List<int> displayTimerMovingAverage;
        static List<int> engineTimerMovingAverage;
        static public void UpdateEngineLabel(int firedCount)
        {
            if (engineTimerMovingAverage == null)
            {
                engineTimerMovingAverage = new List<int>();
                for (int i = 0; i < 100; i++)
                {
                    engineTimerMovingAverage.Add(0);
                }
            }
            engineTimerMovingAverage.RemoveAt(0);
            engineTimerMovingAverage.Add((int)engineElapsed);
            thisWindow.labelEngineStatus.Content = "Running, Speed: " + thisWindow.slider.Value + "  Cycle: " + theNeuronArray.Generation.ToString("N0") +
            "  "+firedCount.ToString("N0") + " Neurons Fired  " + (engineTimerMovingAverage.Average() / 10000f).ToString("F2") + "ms";
        }
        static public void UpdateDisplayLabel(float zoomLevel)
        {
            if (displayTimerMovingAverage == null)
            {
                displayTimerMovingAverage = new List<int>();
                for (int i = 0; i < 10; i++)
                {
                    displayTimerMovingAverage.Add(0);
                }
            }
            displayTimerMovingAverage.RemoveAt(0);
            displayTimerMovingAverage.Add((int)displayElapsed);
            string formatString = "N0";
            if (zoomLevel < 10) formatString = "N1";
            if (zoomLevel < 1) formatString = "N2";
            if (zoomLevel < .1f) formatString = "N3";
            thisWindow.labelDisplayStatus.Content = "Zoom Level: " + zoomLevel.ToString(formatString) + ",  " + (displayTimerMovingAverage.Average() / 10000f).ToString("F2") + "ms";
        }


        public static void Update()
        {
            arrayView.Update();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (theNeuronArray != null)
            {
                if (PromptToSaveChanges())
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

        private bool PromptToSaveChanges()
        {
            if (theNeuronArray == null) return false;
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

        public static void CloseAllModuleDialogs()
        {
            if (theNeuronArray != null)
            {
                lock (theNeuronArray.Modules)
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
        }

        private void MenuItem_MoveNeurons(object sender, RoutedEventArgs e)
        {
            theNeuronArrayView.MoveNeurons();
        }
        private void MenuItem_Undo(object sender, RoutedEventArgs e)
        {
            theNeuronArray.Undo();
            theNeuronArrayView.Update();
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
            theNeuronArrayView.DeleteSelection();
            Update();
        }
        private void MenuItem_ClearSelection(object sender, RoutedEventArgs e)
        {
            theNeuronArrayView.ClearSelection();
            Update();
        }
        private void Button_HelpAbout_Click(object sender, RoutedEventArgs e)
        {
            HelpAbout dlg = new HelpAbout
            {
                Owner = this
            };
            dlg.Show();
        }

        //this reloads the file which was being used on the previous run of the program
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (Keyboard.IsKeyUp(Key.LeftShift))
            {
                try
                {
                    currentFileName = (string)Properties.Settings.Default["CurrentFile"];
                    if (currentFileName != "")
                    {
                        LoadFile(currentFileName);
                    }
                    else //force a new file creation on startup if no file name set
                    {
                        theNeuronArray = new NeuronArray();
                        arrayView.Dp.NeuronDisplaySize = 62;
                        arrayView.Dp.DisplayOffset = new Point(0, 0);
                        theNeuronArray.Initialize(450, 15);
                        setTitleBar();
                        Update();
                    }
                }
                //various errors might have happened so we'll just ignore them all and start with a fresh file 
                catch (Exception e1)
                {
                    e1.GetType();
                    MessageBox.Show("Error encountered in file load: " + e1.Message);
                }
            }

        }
        private void ButtonInit_Click(object sender, RoutedEventArgs e)
        {
            if (theNeuronArray == null) return;
            SuspendEngine();
            lock (theNeuronArray.Modules)
            {
                foreach (ModuleView na in theNeuronArray.Modules)
                {
                    if (na.TheModule != null)
                        na.TheModule.Init(true);
                }
            }
            //doing this messes up because LastFired is not reset
//            theNeuronArray.Generation = 0;
//            theNeuronArray.SetGeneration(0);
            theNeuronArrayView.Update();
            ResumeEngine();
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (!theNeuronArray.EngineIsPaused)
            {
                SetPlayPauseButtonImage(true);
                SuspendEngine();
                theNeuronArrayView.UpdateNeuronColors();
                theNeuronArray.EngineIsPaused = true;
            }
            else
            {
                SetPlayPauseButtonImage(false);
                theNeuronArray.EngineIsPaused = false;
                ResumeEngine();
            }
        }

        private void ButtonSingle_Click(object sender, RoutedEventArgs e)
        {
            if (theNeuronArray != null)
            {
                if (!theNeuronArray.EngineIsPaused)
                {
                    SetPlayPauseButtonImage(true);
                    theNeuronArray.EngineIsPaused = true;
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
        public void SetPlayPauseButtonImage(bool play)
        {
            if (play)
            {
                imagePause.Visibility = Visibility.Collapsed;
                imagePlay.Visibility = Visibility.Visible;
            }
            else
            {
                imagePause.Visibility = Visibility.Visible;
                imagePlay.Visibility = Visibility.Collapsed;
            }

        }

        private void ButtonPan_Click(object sender, RoutedEventArgs e)
        {
            theNeuronArrayView.theCanvas.Cursor = Cursors.Hand;
        }

        private void ButtonZoomIn_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartZoom(1);
        }

        private void ButtonZoomOut_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartZoom(-1);
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

        bool mouseInWindow = false;
        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("MainWindow MouseEnter");
            Keyboard.ClearFocus(); 
            Keyboard.Focus(this);
            this.Focus();
            mouseInWindow = true;
            //Activate();
        }
        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("MainWindow MouseLeave");
            mouseInWindow = false;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            showSynapses = true;
            if (theNeuronArray == null) return;
            theNeuronArray.ShowSynapses = true;
            Update();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            showSynapses = false;
            if (theNeuronArray == null) return;
            theNeuronArray.ShowSynapses = false;
            Update();
        }

        public void SetShowSynapsesCheckBox(bool showSynapses)
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

        public static void CloseNotesWindow()
        {
            if (notesWindow != null)
            {
                notesWindow.Close();
                notesWindow = null;
            }
        }
        private void MenuItemNotes_Click(object sender, RoutedEventArgs e)
        {
            if (notesWindow != null) notesWindow.Close();
            bool showTools = false;
            if (sender != null) showTools = true;
            notesWindow = new NotesDialog(showTools);
            try
            {
                notesWindow.Top = 200;
                notesWindow.Left = 500;
                notesWindow.Show();
            }
            catch
            {
                MessageBox.Show("Notes could not be displayed");
            }
        }

        private void MenuItemHelp_Click(object sender, RoutedEventArgs e)
        {
            Help p;
            p = new Help();
            try
            {
                p.Left = 200;
                p.Top = 200;
                p.Owner = this;
                p.Show();
            }
            catch
            {
                MessageBox.Show("Help could not be displayed");
            }
        }

        private void MenuItemOnlineHelp_Click(object sender, RoutedEventArgs e)
        {
            Help p;
            p = new Help("https://futureai.guru/BrainSimHelp/ui.html");
            try
            {
                p.Left = 200;
                p.Top = 200;
                p.Owner = this;
                p.Show();
            }
            catch
            {
                MessageBox.Show("Help could not be displayed");
            }
        }

        private void MenuItemOnlineBugs_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/FutureAIGuru/BrainSimII/issues");
        }

        private void MenuItemOnlineDiscussions_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://facebook.com/groups/BrainSim");
        }

        private void ThreadCount_TextChanged(object sender, TextChangedEventArgs e)
        {
           if (sender is TextBox tb)
            {
                if (int.TryParse(tb.Text, out int newThreadCount))
                {
                    if (newThreadCount > 0 && newThreadCount < 512)
                        theNeuronArray.SetThreadCount(newThreadCount);
                }
            }
        }
    }
}
