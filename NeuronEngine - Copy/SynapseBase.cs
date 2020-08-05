using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace BrainSimulator
{
    public class SynapseBase
    {
        public float weight;
        public bool IsHebbian = false;
        [XmlIgnore]
        public NeuronBase N; //this is used by the engine
        [XmlIgnore]
        public int IWeight; //this is used by the engine
        public int targetNeuron;
        public int TargetNeuron;
    }
}
