using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator
{
    public  class NeuronSelection
    {

        //array of rectangular selection areas
        public NeuronSelectionRectangle[] selectedRectangle;

        //step counter 
        int position = -1;

        //the nueron ID of the current neurons
        public int selectedNeuronIndex;

        //list to avoid duplicates in enumeration
        List<Neuron> neuronsAlreadyVisited = new List<Neuron>();

        public NeuronSelection()
        {
            selectedRectangle = new NeuronSelectionRectangle[5];
            for (int i = 0; i < 5; i++)
                selectedRectangle[i] = null;

        }

        public void ClearSelection()
        {
            for (int i = 0; i < selectedRectangle.Length; i++)
                selectedRectangle[i] = null;
        }


        public void EnumSelectedNeurons()
        {
            position = -1;
            neuronsAlreadyVisited.Clear();
        }

        public Neuron GetSelectedNeuron()
        {
            position++;
            int currentStart = 0;
            Neuron n = null;
            for (int i = 0; i < selectedRectangle.Length; i++)
            {
                if (selectedRectangle[i] != null)
                {
                    while (position < currentStart + selectedRectangle[i].GetLength())
                    {
                        //the index into the current rectangle
                        int index = position - currentStart;
                        selectedRectangle[i].GetSelectedArea(out int X1, out int Y1, out int X2, out int Y2);
                        int height = Y2 - Y1;
                        selectedNeuronIndex = selectedRectangle[i].FirstSelectedNeuron + (index / height) * 
                            MainWindow.theNeuronArray.rows + index % height;
                        n = MainWindow.theNeuronArray.neuronArray[selectedNeuronIndex];
                        if (!neuronsAlreadyVisited.Contains(n))
                        {
                            neuronsAlreadyVisited.Add(n);
                            return n;
                        }
                        else
                            position++;

                    }
                }
                if (selectedRectangle[i] != null)
                    currentStart += selectedRectangle[i].GetLength();
            }
            return n;
        }

        //returns the number of times the neuron occurs in the selection area
        public int NeuronInSelection(int neuronIndex)
        {
            int count = 0;
            for (int i = 0; i < selectedRectangle.Length; i++)
                if (selectedRectangle[i] != null)
                    if (selectedRectangle[i].NeuronIsInSelection(neuronIndex))
                        count++;
            return count;
        }

        //counts the number of neurons in the selection (and deducts for overlapping selections
        public int GetSelectedNeuronCount()
        {
            int count = 0;
            EnumSelectedNeurons();
            for (Neuron n = GetSelectedNeuron(); n != null; n = GetSelectedNeuron())
            {
                count++;
            }
            return (int)count;
        }

        public void GetSelectedBoundingRectangle(out int X1o, out int Y1o, out int X2o, out int Y2o)
        {
            X1o = Y1o = int.MaxValue;
            X2o = Y2o = int.MinValue;
            for (int i = 0; i < selectedRectangle.Length; i++)
            {
                if (selectedRectangle[i] != null)
                {
                    selectedRectangle[i].GetSelectedArea(out int X1, out int Y1, out int X2, out int Y2);
                    if (X1 < X1o) X1o = X1;
                    if (Y1 < Y1o) Y1o = Y1;
                    if (X2 > X2o) X2o = X2;
                    if (Y2 > Y2o) Y2o = Y2;
                }
            }
        }

    }
}
