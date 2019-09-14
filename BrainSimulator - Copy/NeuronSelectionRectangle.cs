using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Controls;

namespace BrainSimulator
{
    public class NeuronSelectionRectangle
    {
        int firstSelectedNeuron, lastSelectedNeuron;
  
        private int rows;

        public int LastSelectedNeuron { get => lastSelectedNeuron; set => lastSelectedNeuron = value; }
        public int FirstSelectedNeuron { get => firstSelectedNeuron; set => firstSelectedNeuron = value; }

        public NeuronSelectionRectangle(int iRows, int iFirstSelectedNeuron, int iLastSelectedNeuron)
        {
            rows = iRows;
            FirstSelectedNeuron = iFirstSelectedNeuron;
            lastSelectedNeuron = iLastSelectedNeuron;
        }

        public void GetSelectedArea(out int X1, out int Y1, out int X2, out int Y2)
        {
            Y1 = FirstSelectedNeuron % rows;
            X1 = FirstSelectedNeuron / rows;
            Y2 = lastSelectedNeuron % rows+1;
            X2 = lastSelectedNeuron / rows+1;
        }

        public bool NeuronIsInSelection(int neuronIndex)
        {
            GetSelectedArea(out int X1, out int Y1, out int X2, out int Y2);
            int selX = neuronIndex / rows;
            int selY = neuronIndex % rows;
            if (selX >= X1 && selX < X2 && selY >= Y1 && selY < Y2)
                return true;
            return false;
        }

        public Rectangle GetRectangle(DisplayParams dp)
        {
            Rectangle r = new Rectangle();
            Point p1 = dp.pointFromNeuron(FirstSelectedNeuron);
            Point p2 = dp.pointFromNeuron(lastSelectedNeuron);
            p2.X += dp.NeuronDisplaySize;
            p2.Y += dp.NeuronDisplaySize;
            r.Width = p2.X - p1.X;
            r.Height = p2.Y - p1.Y;
            Canvas.SetTop(r, p1.Y);
            Canvas.SetLeft(r, p1.X);
            return r;
        }

        public int GetLength()
        {
            GetSelectedArea(out int X1, out int Y1, out int X2, out int Y2);
            return (X2 - X1) * (Y2 - Y1);
        }
    }
}
