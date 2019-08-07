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
        protected NeuronArea na = null;
        protected NeuronArea naIn = null;
        protected NeuronArea naOut = null;
        private bool initialized = false;

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
            if (initialized && !forceInit) return;
            initialized = true;
            if (na  == null)
            {
                //figure out which area is this one
                foreach (NeuronArea na1 in theNeuronArray.areas)
                {
                    if (na1.TheModule == this)
                    {
                        na = na1;
                        break;
                    }
                }
            }
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
            dlg = (ModuleBaseDlg)Activator.CreateInstance(t1);
            if (dlg == null) return;
            dlg.Parent1 = (ModuleBase)this;
            dlg.Closed += Dlg_Closed;
            dlg.LocationChanged += Dlg_LocationChanged;
            dlg.SizeChanged += Dlg_SizeChanged;
            if (dlgPos != null)
            {
                dlg.Top = dlgPos.Y;
                dlg.Left = dlgPos.X;
            }
            if (dlgSize != null)
            {
                dlg.Width = dlgSize.X;
                dlg.Height = dlgSize.Y;
            }
            dlg.Show();
            dlgIsOpen = true;
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



        protected ModuleBase FindModuleByType(Type t)
        {
            foreach (NeuronArea na1 in theNeuronArray.areas)
            {
                if (na1.TheModule.GetType() == t)
                {
                    return na1.TheModule;
                }
            }
            return null;
        }
        protected ModuleBase FindModuleByName(string name)
        {
            foreach (NeuronArea na1 in theNeuronArray.areas)
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
