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

    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 1)
                startupString = e.Args[0];
        }
        public static string startupString = "";
    }
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

        public ProgressDialog progressDialog;

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


            //this is here because the file can be loaded before the mainwindow displays so
            //module dialogs may open before their owner so this happens a few seconds later
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
        }

        public static void CloseHistoryWindow()
        {
            if (fwWindow != null)
                fwWindow.Close();
            FiringHistory.history.Clear();
            FiringHistory.Clear();
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
        private void LoadFindMenus()
        {
            if (IsArrayEmpty()) return;
            NeuronMenu.Items.Clear();

            List<string> neuronLabelList = theNeuronArray.GetValuesFromLabelCache();
            List<int> neuronIdList = theNeuronArray.GetKeysFromLabelCache();
            for (int i = 0; i < neuronLabelList.Count && i < 100; i++)
            {
                string shortLabel = neuronLabelList[i];
                if (shortLabel.Length > 20) shortLabel = shortLabel.Substring(0, 20);
                shortLabel += " (" + neuronIdList[i] + ")";
                neuronLabelList[i] = shortLabel;
            }
            neuronLabelList.Sort();
            if (neuronLabelList.Count > 100)
                neuronLabelList.RemoveRange(100, neuronLabelList.Count - 100);
            NeuronMenu.IsEnabled = (neuronLabelList.Count == 0) ? false : true;
            foreach (string s in neuronLabelList)
            {
                MenuItem mi = new MenuItem { Header = s };
                mi.Click += NeuronMenu_Click;
                NeuronMenu.Items.Add(mi);
            }


            ModuleMenu.Items.Clear();
            List<string> moduleLabelList = new List<string>();
            for (int i = 0; i < theNeuronArray.Modules.Count; i++)
            {
                ModuleView theModule = theNeuronArray.Modules[i];
                string shortLabel = theModule.Label;
                if (shortLabel.Length > 10) shortLabel = shortLabel.Substring(0, 10);
                shortLabel += " (" + theModule.FirstNeuron + ")";
                moduleLabelList.Add(shortLabel);
            }
            moduleLabelList.Sort();
            ModuleMenu.IsEnabled = (moduleLabelList.Count == 0) ? false : true;
            foreach (string s in moduleLabelList)
            {
                MenuItem mi = new MenuItem { Header = s };
                mi.Click += NeuronMenu_Click;
                ModuleMenu.Items.Add(mi);
            }
        }

        private void SetKBStatus()
        {
            string kbString = "";
            if (ctrlPressed) kbString += "CTRL ";
            if (shiftPressed) kbString += "SHFT ";
            KBStatus.Text = kbString;
        }

        private void setTitleBar()
        {
            Title = "Brain Simulator II " + System.IO.Path.GetFileNameWithoutExtension(currentFileName);
        }

        /// <summary>
        /// Set the a field in the status bar at the bottom of the main window
        /// </summary>
        /// <param name="field">0-4 to set select which field to update</param> 
        /// <param name="message"></param>
        /// <param name="severity">0-2 = green,yellow,red</param> 
        public void SetStatus(int field, string message, int severity = 0)
        {
            TextBlock tb = null;
            switch (field)
            {
                case 0: tb = statusField0; break;
                case 1: tb = statusField1; break;
                case 2: tb = statusField2; break;
                case 3: tb = statusField3; break;
            }
            SolidColorBrush theColor = null;
            switch (severity)
            {
                case 0: theColor = new SolidColorBrush(Colors.LightGreen); break;
                case 1: theColor = new SolidColorBrush(Colors.Yellow); break;
                case 2: theColor = new SolidColorBrush(Colors.Pink); break;
            }
            tb.Background = theColor;
            tb.Text = message;
        }
        /// <summary>
        /// Really cool progress bar which can be shown at any time
        /// </summary>
        /// <param name="value">0-100 the completion percent 0 initializes, 100 closes</param> 
        /// <param name="label"></param>
        /// <returns>true if the cancel button was pressed</returns>  
        /// 
        float prevValue = 0;
        public bool SetProgress(float value, string label)
        {
            if (value != 0 && value < 100 && Math.Abs(prevValue - value ) < 0.1)
            {
                return false;
            }
            bool retVal = false;
            prevValue = value;
            if (Application.Current.Dispatcher.CheckAccess())
            {
                retVal = progressDialog.SetProgress(value, label);
                AllowUIToUpdate();
            }
            else
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    retVal = progressDialog.SetProgress(value, label);
                });
            }
            return retVal;
        }

        //this little helper lets the progress bar update in the middle of a long operation in the UI thread
        void AllowUIToUpdate()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(delegate (object parameter)
            {
                frame.Continue = false;
                return null;
            }), null);

            Dispatcher.PushFrame(frame);
            //EDIT:
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                                          new Action(delegate { }));
        }
        public static bool Busy()
        {
            if (thisWindow == null) return true;
            if (thisWindow.progressDialog == null) return true;
            if (thisWindow.progressDialog.Visibility == Visibility.Visible) return true;
            return false;
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
            LoadFindMenus();
            if (theNeuronArray != null && !useServers) ThreadCount.Text = theNeuronArray.GetThreadCount().ToString();
            else ThreadCount.Text = "";
            if (theNeuronArray != null) Refractory.Text = theNeuronArray.GetRefractoryDelay().ToString();
            else Refractory.Text = "";

            if (currentFileName != "" &&
                XmlFile.CanWriteTo(currentFileName, out string message)
                && theNeuronArray != null)
            {
                EnableMenuItem(MainMenu.Items, "_Save", true);
                SaveButton.IsEnabled = true;
            }
            else
            {
                EnableMenuItem(MainMenu.Items, "_Save", false);
                SaveButton.IsEnabled = false;
            }
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
            if (useServers)
            {
                var tb0 = Utils.FindByName(MainMenu, "ThreadCount");
                if (tb0.Parent is UIElement ui)
                    ui.Visibility = Visibility.Collapsed;
                tb0 = Utils.FindByName(MainMenu, "Refractory");
                if (tb0.Parent is UIElement ui1)
                    ui1.Visibility = Visibility.Collapsed;
            }
            else
            {
                var tb0 = Utils.FindByName(MainMenu, "ThreadCount");
                if (tb0.Parent is UIElement ui)
                    ui.Visibility = Visibility.Visible;
                tb0 = Utils.FindByName(MainMenu, "Refractory");
                if (tb0.Parent is UIElement ui1)
                    ui1.Visibility = Visibility.Visible;
            }
            if (IsArrayEmpty())
            {
                EnableMenuItem(MainMenu.Items, "_Save", false);
                EnableMenuItem(MainMenu.Items, "Save _As", false);
                EnableMenuItem(MainMenu.Items, "_Insert", false);
                EnableMenuItem(MainMenu.Items, "_Properties", false);
                EnableMenuItem(MainMenu.Items, "_Notes", false);
            }
            else
            {
                EnableMenuItem(MainMenu.Items, "Save _As", true);
                EnableMenuItem(MainMenu.Items, "_Insert", true);
                EnableMenuItem(MainMenu.Items, "_Properties", true);
                EnableMenuItem(MainMenu.Items, "_Notes", true);
                MenuItem mi = (MenuItem)Utils.FindByName(MainMenu, "ShowSynapses");
                if (mi != null)
                    mi.IsChecked = theNeuronArray.ShowSynapses;
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
            if (arrayView.targetNeuronIndex < 0 || myClipBoard == null)
            {
                EnableMenuItem(MainMenu.Items, " Paste", false);
            }
            else
            {
                EnableMenuItem(MainMenu.Items, " Paste", true);
            }
            if (theNeuronArray != null && theNeuronArray.UndoPossible())
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
            string displayStatus= "Zoom Level: " + zoomLevel.ToString(formatString) + ",  " + (displayTimerMovingAverage.Average() / 10000f).ToString("F2") + "ms";
            thisWindow.SetStatus(2, displayStatus, 0);
        }

        public static void Update()
        {
            arrayView.Update();
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

        public static bool IsArrayEmpty()
        {
            if (MainWindow.theNeuronArray == null) return true;
            if (MainWindow.theNeuronArray.arraySize == 0) return true;
            if (MainWindow.theNeuronArray.rows == 0) return true;
            if (MainWindow.theNeuronArray.Cols == 0) return true;
            return false;
        }
        
        public void CreateEmptyNetwork()
        {
            theNeuronArray = new NeuronArray();
            arrayView.Dp.NeuronDisplaySize = 62;
            arrayView.Dp.DisplayOffset = new Point(0, 0);
            theNeuronArray.Initialize(450, 15);
            Update();
        }
    }
}
