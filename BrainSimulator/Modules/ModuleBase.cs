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
using System.Threading;


namespace BrainSimulator
{
    abstract public class ModuleBase
    {
        protected NeuronArray theNeuronArray { get => MainWindow.theNeuronArray; }
        protected Module na = null;
        protected Module naIn = null;
        protected Module naOut = null;
        public bool initialized = false;

        protected ModuleBaseDlg dlg = null;
        public Point dlgPos;
        public Point dlgSize;
        public bool dlgIsOpen = false;

        public void CloseDlg()
        {
            if (dlg != null)
                dlg.Close();
        }
        public ModuleBase() { }
                
        abstract public void Fire();

        virtual public void Initialize()
        { }

        public void Init(bool forceInit = false)
        {
            if (na  == null)
            {
                //figure out which area is this one
                foreach (Module na1 in theNeuronArray.modules)
                {
                    if (na1.TheModule == this)
                    {
                        na = na1;
                        break;
                    }
                }
            }
            
            if (initialized && !forceInit) return;
            initialized = true;

            Initialize();

            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });

            if (dlg == null && dlgIsOpen)
            {
                ShowDialog();
                dlgIsOpen = true;
            }
        }

        public virtual void ShowDialog()
        {
            ApartmentState aps = Thread.CurrentThread.GetApartmentState();
            if (aps != ApartmentState.STA) return;
            Type t = this.GetType();
            Type t1 = Type.GetType(t.ToString()+"Dlg");
            if (t1 == null) return;
            if (dlg != null) dlg.Close();
            dlg = (ModuleBaseDlg)Activator.CreateInstance(t1);
            if (dlg == null) return;
            dlg.Parent1 = (ModuleBase)this;
            dlg.Closed += Dlg_Closed;
            dlg.LocationChanged += Dlg_LocationChanged;
            dlg.SizeChanged += Dlg_SizeChanged;
            if (dlgPos != new Point(0,0))
            {
                dlg.Top = dlgPos.Y;
                dlg.Left = dlgPos.X;
            }
            else
            {
                dlg.Top = 250;
                dlg.Left = 250;
            }
            if (dlgSize != new Point (0,0))
            {
                dlg.Width = dlgSize.X;
                dlg.Height = dlgSize.Y;
            }
            else
            {
                dlg.Width = 250;
                dlg.Height = 200;
            }
            //this hack is here because a file might load and create dialogs prior to the mainwindow opening
            Window mainWindow = Application.Current.MainWindow;
            if (mainWindow.GetType() == typeof(MainWindow))
                dlg.Owner = Application.Current.MainWindow;

            dlg.Show();
            dlgIsOpen = true;
        }
        //this hack is here because a file might load and create dialogs prior to the mainwindow opening
        public void SetDlgOwner(Window MainWindow)
        {
            if (dlg != null) 
                dlg.Owner =MainWindow;
        }

        private void Dlg_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            dlgSize = new Point()
            { Y = dlg.Height, X = dlg.Width};
        }

        private void Dlg_LocationChanged(object sender, EventArgs e)
        {
            dlgPos = new Point()
            { Y = dlg.Top, X = dlg.Left };
        }

        private void Dlg_Closed(object sender, EventArgs e)
        {
            dlg = null;
            dlgIsOpen = false;
        }

        public virtual void SetUpAfterLoad()
        { }
        public virtual void SetUpBeforeSave()
        { }

        protected ModuleBase FindModuleByType(Type t)
        {
            foreach (Module na1 in theNeuronArray.modules)
            {
                if (na1.TheModule != null && na1.TheModule.GetType() == t)
                {
                    return na1.TheModule;
                }
            }
            return null;
        }
        protected ModuleBase FindModuleByName(string name)
        {
            foreach (Module na1 in theNeuronArray.modules)
            {
                if (na1.Label == name)
                {
                    return na1.TheModule;
                }
            }
            return null;
        }
    }
}
