using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public class NeuronArea
    {
        int firstNeuron, lastNeuron = 0;
        string label;
        string commandLine;
        int color;

        public NeuronArea(int firstNeuron1, int lastNeuron1, string theLabel, string theCommandLine,int theColor)
        {
            FirstNeuron = firstNeuron1;
            LastNeuron = lastNeuron1;
            Label = theLabel;
            CommandLine = theCommandLine;
            color = theColor;
        }
        public NeuronArea() { }
        public int FirstNeuron { get => firstNeuron; set => firstNeuron = value; }
        public int LastNeuron { get => lastNeuron; set => lastNeuron = value; }
        public string CommandLine { get => commandLine; set => commandLine = value; }
        public string Label { get => label; set => label = value; }
        public int Rows { get { return MainWindow.theNeuronArray.rows; } }

        public int Height { get { return 1 + lastNeuron % Rows - firstNeuron % Rows; } }
        public int Width { get { return 1 + lastNeuron / Rows - firstNeuron / Rows; } }
        public int NeuronCount { get { return Width * Height; } }
        public int Color { get => color; set => color = value; }

        //these two emulate a foreach which might be implemented some day
        int currentNeuronInArea = 0;
        public void BeginEnum()
        { currentNeuronInArea = 0; }
        public Neuron GetNextNeuron()
        {
            int neuronIndex = (currentNeuronInArea % Height) + (currentNeuronInArea / Height) * MainWindow.theNeuronArray.rows + firstNeuron;
            if (currentNeuronInArea >= Height * Width) return null;
            currentNeuronInArea++;
            return MainWindow.theNeuronArray.neuronArray[neuronIndex];
        }

        public Neuron GetFreeNeuron()
        {
            BeginEnum();
            for (Neuron n = GetNextNeuron(); n != null; n = GetNextNeuron())
                if (!n.InUse())
                    return n;
            return null;
        }
        //this gets the neuron with an index relative to the area itself
        public Neuron GetNeuronAt(int index)
        {
            int neuronIndex = (index % Height) + (index / Height) * Rows + firstNeuron;
            return MainWindow.theNeuronArray.neuronArray[neuronIndex];
        }
        public Neuron GetNeuronAt(int X, int Y)
        {
            int index = firstNeuron + Y + X * Rows;
            return MainWindow.theNeuronArray.neuronArray[index];
        }

        public void GetBounds(out int X1, out int Y1, out int X2, out int Y2)
        {
            NeuronSelectionRectangle nsr = new NeuronSelectionRectangle(Rows, firstNeuron, lastNeuron);
            nsr.GetSelectedArea(out X1, out Y1, out X2, out Y2);
        }
        public void GetAbsNeuronLocation(int index, out int X, out int Y)
        {
            X = index / Rows;
            Y = index % Rows;
        }

        public void GetNeuronLocation(Neuron n, out int X, out int Y)
        {
            GetAbsNeuronLocation(n.Id, out int X1, out int Y1);
            GetAbsNeuronLocation(FirstNeuron, out int X2, out int Y2);
            X = X1 - X2;
            Y = Y1 - Y2;
        }
        public void GetNeuronLocation(int nIndex, out int X, out int Y)
        {
            GetAbsNeuronLocation(nIndex, out int X1, out int Y1);
            GetAbsNeuronLocation(FirstNeuron, out int X2, out int Y2);
            X = X1 - X2;
            Y = Y1 - Y2;
        }

        public void ClearAllNeurons()
        {
            BeginEnum();
            for (Neuron n = GetNextNeuron(); n != null; n = GetNextNeuron())
                n.LastCharge = n.CurrentCharge = 0;
        }

        public void ClearNeuronArea()
        {
            BeginEnum();
            for (Neuron n = GetNextNeuron(); n != null; n = GetNextNeuron())
                n.DeleteAllSynapes();
        }
        public int NeuronsInUseInArea()
        {
            int count = 0;
            BeginEnum();
            for (Neuron n = GetNextNeuron(); n != null; n = GetNextNeuron())
                if (n.InUse())
                    count++;
            return count;
        }
        public int NeuronsFiredInArea()
        {
            int count = 0;
            BeginEnum();
            for (Neuron n = GetNextNeuron(); n != null; n = GetNextNeuron())
                if (n.LastCharge > .9)
                    count++;
            return count;
        }
        public void ClearNeuronChargeInArea()
        {
            BeginEnum();
            for (Neuron n = GetNextNeuron(); n != null; n = GetNextNeuron())
            {
                n.CurrentCharge = n.LastCharge = 0; n.Range = 0;
            }
        }
        public string GetParam(string key)
        {
            string[] parameters = CommandLine.Split(' ');
            for (int i = 0; i < parameters.Length - 1; i++)
            {
                string param = parameters[i];
                if (param == key)
                {
                    return parameters[i + 1];
                }
            }
            return "";
        }
    }
}
