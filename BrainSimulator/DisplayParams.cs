using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
namespace BrainSimulator
{
    public class DisplayParams
    {
        private int neuronDisplaySize = 45; //this is the zoom level
        private Point displayOffset = new Point(0, 0); //the pan position
        private int neuronRows = -1;     //this number lets us display a one-dimensional array as a 2D array

        public int NeuronDisplaySize
        {
            get
            {
                return neuronDisplaySize;
            }

            set
            {
                neuronDisplaySize = value;
                SynapseView.dp = this;
                NeuronView.dp = this;
            }
        }

        public Point DisplayOffset
        {
            get
            {
                return displayOffset;
            }

            set
            {
                displayOffset = value;
                SynapseView.dp = this;
                NeuronView.dp = this;
            }
        }

        public int NeuronRows
        {
            get
            {
                return neuronRows;
            }

            set
            {
                neuronRows = value;
                SynapseView.dp = this;
                NeuronView.dp = this;
            }
        }

        public Point pointFromNeuron(int index)
        {
            //return the upper left of the neuron on the canvas
            Point p = new Point(DisplayOffset.X, DisplayOffset.Y);
            p.Y += index % NeuronRows * NeuronDisplaySize;
            p.X += index / NeuronRows * NeuronDisplaySize;
            return p;
        }
        public int NeuronFromPoint(Point p)  //the point is the canvas position
        {
            p -= (Vector)DisplayOffset;
            int x = (int)p.X / NeuronDisplaySize;
            int y = (int)p.Y / NeuronDisplaySize;
            int index = x * NeuronRows + y;
            return index;
        }
    }
}
