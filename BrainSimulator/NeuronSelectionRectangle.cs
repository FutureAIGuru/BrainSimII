//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

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
        int firstSelectedNeuron;
  
        int width;
        int height;
        public int Rows { get { return MainWindow.theNeuronArray.rows; } }


        public int FirstSelectedNeuron { get => firstSelectedNeuron; set => firstSelectedNeuron = value; }
        public int LastSelectedNeuron { get { return FirstSelectedNeuron + (Height - 1) + Rows * (Width - 1); } }
        public int Width { get => width; set => width = value; }
        public int Height { get => height; set => height = value; }


        public NeuronSelectionRectangle(int iFirstSelectedNeuron, int width,int height)
        {
            FirstSelectedNeuron = iFirstSelectedNeuron;
            Height = height;
            Width = width;
        }

        public IEnumerable<int> NeuronInRectangle()
        {
            int count = width * height;
            for (int i = 0; i < count; i++)
            {
                int row = i % height;
                int col = i / height;
                int target = firstSelectedNeuron + row + MainWindow.theNeuronArray.rows * col;
                yield return target;
            }
        }



        //in neuron numbers
        public void GetSelectedArea(out int X1, out int Y1, out int X2, out int Y2)
        {
            Y1 = FirstSelectedNeuron % Rows;
            X1 = FirstSelectedNeuron / Rows;
            Y2 = Y1 + Height - 1;
            X2 = X1 + Width - 1;
        }

        //in pixels
        public Rectangle GetRectangle(DisplayParams dp)
        {
            Rectangle r = new Rectangle();
            Point p1 = dp.pointFromNeuron(FirstSelectedNeuron);
            Point p2 = dp.pointFromNeuron(LastSelectedNeuron);
            p2.X += dp.NeuronDisplaySize;
            p2.Y += dp.NeuronDisplaySize;
            r.Width = p2.X - p1.X;
            r.Height = p2.Y - p1.Y;
            Canvas.SetTop(r, p1.Y);
            Canvas.SetLeft(r, p1.X);
            return r;
        }

        public bool NeuronIsInSelection(int neuronIndex)
        {
            GetSelectedArea(out int X1, out int Y1, out int X2, out int Y2);
            int selX = neuronIndex / Rows;
            int selY = neuronIndex % Rows;
            if (selX >= X1 && selX <= X2 && selY >= Y1 && selY <= Y2)
                return true;
            return false;
        }

        public int GetLength()
        {
            GetSelectedArea(out int X1, out int Y1, out int X2, out int Y2);
            return (X2 - X1+1) * (Y2 - Y1+1);
        }
    }
}
