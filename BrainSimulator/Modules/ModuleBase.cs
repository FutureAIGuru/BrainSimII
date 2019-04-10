using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    abstract public class ModuleBase
    {
        protected NeuronArray theNeuronArray { get => MainWindow.theNeuronArray; }
        protected NeuronArea na = null;
        protected NeuronArea naIn = null;
        protected NeuronArea naOut = null;
        private bool initialized = false;
        public ModuleBase() { }

        abstract public void Fire();

        virtual protected void Initialize()
        { }

        protected void Init()
        {
            if (initialized) return;
            initialized = true;
            if (na != null) return;

            //figure out which area is this one
            foreach (NeuronArea na1 in theNeuronArray.areas)
            {
                if (na1.TheModule == this)
                {
                    na = na1;
                    break;
                }
            }
            string input = na.GetParam("-i");
            naIn = theNeuronArray.FindAreaByLabel(input);
            string output = na.GetParam("-o");
            naOut = theNeuronArray.FindAreaByLabel(output);
            Initialize();
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
    }
}
