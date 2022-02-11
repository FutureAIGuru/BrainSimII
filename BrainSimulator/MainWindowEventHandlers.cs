//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private void DisplayUpdate_TimerTick(object sender, EventArgs e)
        {
            updateDisplay = true;

            //Debug.WriteLine("Display Update " + DateTime.Now);

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
            }
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
            SetKBStatus();
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
                //TODO here is where we'd add deleting the last-clicked module (issue #160) should we choose to implement it
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
                NeuronArrayView.StopInsertingModule();
            }
            if (e.Key == Key.F1)
            {
                MenuItemHelp_Click(null, null);
            }
            if (ctrlPressed && e.Key == Key.O)
            {
                buttonLoad_Click(null, null);
            }
            if (ctrlPressed && e.Key == Key.N)
            {
                button_FileNew_Click(null, null);
            }
            if (ctrlPressed && e.Key == Key.S)
            {
                buttonSave_Click(null, null);
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
            if (ctrlPressed && e.Key == Key.A)
            {
                MenuItem_SelectAll(null, null);
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
            SetKBStatus();
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
            if (SaveAs())
                SaveButton.IsEnabled = true;
        }
        private void buttonReloadNetwork_click(object sender, RoutedEventArgs e)
        {

            if (PromptToSaveChanges())
                return;
            else
            {
                if (currentFileName != "")
                {
                    LoadCurrentFile();
                }
            }
        }
        private void NeuronMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                string id = mi.Header.ToString();
                id = id.Substring(id.LastIndexOf('(') + 1);
                id = id.Substring(0, id.Length - 1);
                if (int.TryParse(id, out int nID))
                {
                    theNeuronArrayView.targetNeuronIndex = nID;
                    theNeuronArrayView.PanToNeuron(nID);
                }
            }
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

        private void buttonLoad_Click(object sender, RoutedEventArgs e)
        {
            if (PromptToSaveChanges())
                return;
            string fileName = "_Open";
            if (sender is MenuItem mainMenu)
                fileName = (string)mainMenu.Header;

            if (fileName == "_Open")
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog
                {
                    Filter = "XML Network Files|*.xml",
                    Title = "Select a Brain Simulator File",
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
                //TODO: the following line unconditionally clobbers the current network
                //so the cancel button in the dialog won't work properly
                //CreateEmptyNetwork(); // to avoid keeping too many bytes occupied...

                //Set buttons
                ReloadNetwork.IsEnabled = false;
                Reload_network.IsEnabled = false;
                // and make sure we have maximum memory free...
                GC.Collect();
                GC.WaitForPendingFinalizers();
                UpdateFreeMem();
                NewArrayDlg dlg = new NewArrayDlg();
                dlg.ShowDialog();
                if (dlg.returnValue)
                {
                    arrayView.Dp.NeuronDisplaySize = 62;
                    ButtonZoomToOrigin_Click(null, null);
                    currentFileName = "";
                    SetCurrentFileNameToProperties();
                    SetTitleBar();
                    if (theNeuronArray.networkNotes != "")
                        MenuItemNotes_Click(null, null);
                }
                Update();
                ResumeEngine();
            }
        }
        private void button_Exit_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            this.Close();
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
            int interval = 0;
            if (value == 0) interval = 1000;
            if (value == 1) interval = 1000;
            if (value == 2) interval = 500;
            if (value == 3) interval = 250;
            if (value == 4) interval = 100;
            if (value == 5) interval = 50;
            if (value == 6) interval = 10;
            if (value == 7) interval = 5;
            if (value == 8) interval = 2;
            if (value == 9) interval = 1;
            if (value > 9)
                interval = 0;
            engineDelay = interval;
            if (theNeuronArray != null)
                theNeuronArray.EngineSpeed = interval;
            if (!engineThread.IsAlive)
                engineThread.Start();
            EngineSpeed.Text = slider.Value.ToString();
            displayUpdateTimer.Start();
            if (engineSpeedStack.Count > 0)
            {//if there is a stack entry, the engine is paused...put the new value on the stack
                engineSpeedStack.Pop();
                engineSpeedStack.Push(engineDelay);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //close any help window which was open.
            Process[] theProcesses1 = Process.GetProcesses();
            for (int i = 1; i < theProcesses1.Length; i++)
            {
                try
                {
                    if (theProcesses1[i].MainWindowTitle != "")
                    {
                        if (theProcesses1[i].MainWindowTitle.Contains("GettingStarted"))
                        {
                            theProcesses1[i].CloseMainWindow(); ;
                        }
                    }
                }
                catch (Exception e1)
                {
                    MessageBox.Show("Window Clopsing Failed, Message: " + e1.Message);
                }
            }


            if (theNeuronArray != null)
            {
                if (PromptToSaveChanges())
                {
                    e.Cancel = true;
                }
                else
                {
                    SuspendEngine();

                    engineIsCancelled = true;
                    CloseAllModuleDialogs();
                }
            }
            else
            {
                engineIsCancelled = true;
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
        //or creates a new one
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            progressDialog = new ProgressDialog();
            progressDialog.Visibility = Visibility.Collapsed;
            progressDialog.Top = 100;
            progressDialog.Left = 100;
            progressDialog.Owner = this;
            progressDialog.Show();
            progressDialog.Hide();
            progressDialog.SetProgress(100, "");

            //if the left shift key is pressed, don't load the file
            if (Keyboard.IsKeyUp(Key.LeftShift))
            {
                try
                {
                    string fileName = ""; //if the load is successful, currentfile will be set by the load process
                    if (App.StartupString != "")
                        fileName = App.StartupString;
                    if (fileName == "")
                        fileName = (string)Properties.Settings.Default["CurrentFile"];
                    if (fileName != "")
                    {
                        LoadFile(fileName);
                        NeuronView.OpenHistoryWindow();
                    }
                    else //force a new file creation on startup if no file name set
                    {
                        CreateEmptyNetwork();
                        SetPlayPauseButtonImage(false);
                    }
                }
                //various errors might have happened so we'll just ignore them all and start with a fresh file 
                catch (Exception e1)
                {
                    e1.GetType();
                    MessageBox.Show("Error encountered in file load: " + e1.Message);
                    CreateEmptyNetwork();
                }
            }
            LoadModuleTypeMenu();
        }
        private void ButtonInit_Click(object sender, RoutedEventArgs e)
        {
            if (IsArrayEmpty()) return;
            SuspendEngine();
            lock (theNeuronArray.Modules)
            {
                foreach (ModuleView na in theNeuronArray.Modules)
                {
                    if (na.TheModule != null)
                        na.TheModule.Initialize();
                }
            }
            //TODO: doing this messes up because LastFired is not reset
            //            theNeuronArray.Generation = 0;
            //            theNeuronArray.SetGeneration(0);
            theNeuronArrayView.Update();
            ResumeEngine();
        }

        //this is two buttons on one event handler for historical reasons
        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (theNeuronArray == null)
                return;

            if (sender is Button playOrPause)
                if (playOrPause.Name == "buttonPause")
                {
                    SetPlayPauseButtonImage(true);
                    SuspendEngine();
                    theNeuronArrayView.UpdateNeuronColors();
                    engineIsPaused = true;
                    theNeuronArray.EngineIsPaused = true;
                }
                else
                {
                    SetPlayPauseButtonImage(false);
                    theNeuronArray.EngineIsPaused = false;
                    engineIsPaused = false;
                    ResumeEngine();
                }
        }
        public void SetPlayPauseButtonImage(bool paused)
        {
            if (paused)
            {
                buttonPlay.IsEnabled = true;
                buttonPause.IsEnabled = false;
            }
            else
            {
                buttonPlay.IsEnabled = false;
                buttonPause.IsEnabled = true;
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
                    SetPlayPauseButtonImage(true);
                    theNeuronArray.Fire();
                    theNeuronArrayView.UpdateNeuronColors();
                }
            }
        }
        private void ButtonPan_Click(object sender, RoutedEventArgs e)
        {
            theNeuronArrayView.theCanvas.Cursor = Cursors.Hand;
        }

        private void ButtonZoomIn_Click(object sender, RoutedEventArgs e)
        {
            theNeuronArrayView.Zoom(1);
        }

        private void ButtonZoomOut_Click(object sender, RoutedEventArgs e)
        {
            theNeuronArrayView.Zoom(-1);
        }

        private void ButtonZoomToOrigin_Click(object sender, RoutedEventArgs e)
        {
            //both menu items come here as the button is a toggler
            if (sender is MenuItem mi)
            {
                if (mi.Header.ToString() == "Show all")
                    theNeuronArrayView.Dp.NeuronDisplaySize = 62;
                else
                    theNeuronArrayView.Dp.NeuronDisplaySize = 63;
            }

            theNeuronArrayView.Dp.DisplayOffset = new Point(0, 0);
            if (theNeuronArrayView.Dp.NeuronDisplaySize != 62)
                theNeuronArrayView.Dp.NeuronDisplaySize = 62;
            else
            {
                double size = Math.Min(theNeuronArrayView.ActualHeight() / (double)(theNeuronArray.rows), theNeuronArrayView.ActualWidth() / (double)(theNeuronArray.Cols));
                theNeuronArrayView.Dp.NeuronDisplaySize = (float)size;
            }
            Update();
        }

        //This is here so we can capture the shift key to do a pan whenever the mouse in in the window
        bool mouseInWindow = false;
        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            //Debug.WriteLine("MainWindow MouseEnter");
            Keyboard.ClearFocus();
            Keyboard.Focus(this);
            this.Focus();
            mouseInWindow = true;
        }
        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            //Debug.WriteLine("MainWindow MouseLeave");
            mouseInWindow = false;
        }

        private void Menu_ShowSynapses(object sender, RoutedEventArgs e)
        {
            if (IsArrayEmpty()) return;
            if (sender is MenuItem mi)
            {
                //single menu item comes here so must be toggled
                theNeuronArray.ShowSynapses = !theNeuronArray.ShowSynapses;
                SetShowSynapsesCheckBox(theNeuronArray.ShowSynapses);
            }
            Update();
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (IsArrayEmpty()) return;
            theNeuronArray.ShowSynapses = true;
            Update();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (IsArrayEmpty()) return;
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

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);
        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void MenuItemHelp_Click(object sender, RoutedEventArgs e)
        {
            Uri theUri = new Uri("https://futureAI.guru/help/getting started.htm");

            //first check to see if help is already open
            Process[] theProcesses1 = Process.GetProcesses();
            Process latestProcess = null;
            for (int i = 1; i < theProcesses1.Length; i++)
            {
                try
                {
                    if (theProcesses1[i].MainWindowTitle != "")
                    {
                        if (theProcesses1[i].MainWindowTitle.Contains("GettingStarted"))
                            latestProcess = theProcesses1[i];
                    }
                }
                catch (Exception e1)
                {
                    MessageBox.Show("Opening Help Item Failed, Message: " + e1.Message);
                }
            }


            if (latestProcess == null)
            {
                OpenApp("https://futureai.guru/BrainSimHelp/gettingstarted.html");

                //gotta wait for the page to render before it shows in the processes list
                DateTime starttTime = DateTime.Now;

                while (latestProcess == null && DateTime.Now < starttTime + new TimeSpan(0, 0, 3))
                {
                    theProcesses1 = Process.GetProcesses();
                    for (int i = 1; i < theProcesses1.Length; i++)
                    {
                        try
                        {
                            if (theProcesses1[i].MainWindowTitle != "")
                            {
                                if (theProcesses1[i].MainWindowTitle.Contains("GettingStarted"))
                                    latestProcess = theProcesses1[i];
                            }
                        }
                        catch (Exception e1)
                        {
                            MessageBox.Show("Opening Help Item Failed, Message: " + e1.Message);
                        }
                    }
                }
            }

            try
            {
                if (latestProcess != null)
                {
                    IntPtr id = latestProcess.MainWindowHandle;

                    Rect theRect = new Rect();
                    GetWindowRect(id, ref theRect);

                    bool retVal = MoveWindow(id, 300, 100, 1200, 700, true);
                    this.Activate();
                    SetForegroundWindow(id);
                }
            }
            catch (Exception e1)
            {
                MessageBox.Show("Opening Help Failed, Message: " + e1.Message);
            }
        }

        //This opens an app depending on the assignments of the file extensions in Windows
        public static void OpenApp(string fileName)
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = fileName;
                p.StartInfo.UseShellExecute = true;
                p.Start();
            }
            catch (Exception e)
            {
                MainWindow.thisWindow.SetStatus(0, "OpenApp: "+e.Message, 1);
            }
        }

        private void MenuItemOnlineHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenApp("https://futureai.guru/BrainSimHelp/ui.html");
        }

        private void MenuItemOnlineBugs_Click(object sender, RoutedEventArgs e)
        {
            OpenApp("https://github.com/FutureAIGuru/BrainSimII/issues");
        }

        private void MenuItemRegister_Click(object sender, RoutedEventArgs e)
        {
            OpenApp("https://futureai.guru/BrainSimRegister.aspx");
        }

        private void MenuItemOnlineDiscussions_Click(object sender, RoutedEventArgs e)
        {
            OpenApp("https://facebook.com/groups/BrainSim");
        }

        private void MenuItemYouTube_Click(object sender, RoutedEventArgs e)
        {
            OpenApp("https://www.youtube.com/c/futureai");
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

        private void MenuItem_SelectAll(object sender, RoutedEventArgs e)
        {
            arrayView.ClearSelection();
            SelectionRectangle rr = new SelectionRectangle(0, theNeuronArray.Cols, theNeuronArray.rows);
            arrayView.theSelection.selectedRectangles.Add(rr);
            Update();
        }

        private void cbShowHelpAtStartup_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default["ShowHelp"] = cbShowHelpAtStartup.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void InsertModule_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
                NeuronArrayView.StartInsertingModule(mi.Header.ToString());
        }
        private void ModuleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb)
            {
                if (cb.SelectedItem != null)
                {
                    string moduleName = ((Label)cb.SelectedItem).Content.ToString();
                    cb.SelectedIndex = -1;
                    NeuronArrayView.StartInsertingModule(moduleName);
                }
            }
        }
        private void MenuCheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            CheckForVersionUpdate(true);
        }
        private void MenuItemModuleInfo_Click(object sender, RoutedEventArgs e)
        {
            ModuleDescriptionDlg md = new ModuleDescriptionDlg("");
            md.ShowDialog();
        }

    }
}
