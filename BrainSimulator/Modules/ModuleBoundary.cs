﻿//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleBoundary : ModuleBase
    {
        //any public variable you create here will automatically be stored with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;

        public override string ShortDescription { get => "Finds the boundaries in an imagefile module image."; }
        public override string LongDescription
        {
            get =>
                "TO DO: Long description of Module Boundary.";
        }

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            ModuleView naSource = theNeuronArray.FindModuleByLabel("ImageFile");
        }
        public override void Initialize()
        {
            ModuleView naSource = theNeuronArray.FindModuleByLabel("ImageFile");
            if (naSource == null)
            {
                MessageBox.Show("Boundary module requires ImageFile module for input.");
                return;
            }
            foreach (Neuron n in naSource.Neurons1)
            {
                n.Clear();
                n.Model = Neuron.modelType.Color;
            }
            foreach (Neuron n in na.Neurons1)
            {
                n.Clear();
                n.Model = Neuron.modelType.LIF;
                n.LeakRate = 1f;
            }

            theNeuronArray.GetNeuronLocation(na.FirstNeuron, out int col, out int row);

            na.Width = naSource.Width * 6;
            na.Height = naSource.Height;

            if (col + na.Width >= theNeuronArray.Cols ||
                row + na.Height >= theNeuronArray.rows)
            {
                MessageBox.Show(na.Label + " would exceed neuron array boundaries.");
                return;
            }

            //Up Down Left Right angles: UL DR UR DL
            for (int x = 0; x < naSource.Width; x++)
                for (int y = 0; y < naSource.Height; y++)
                {
                    Neuron n0 = naSource.GetNeuronAt(x, y);
                    Neuron n1 = naSource.GetNeuronAt(x + 1, y);
                    Neuron n2 = naSource.GetNeuronAt(x, y + 1);
                    Neuron n3 = naSource.GetNeuronAt(x + 1, y + 1);


                    //the letter indicates which side is white...WB = L BW = R
                    int x1 = x * 6;
                    Neuron nL = na.GetNeuronAt(x1, y);
                    Neuron nR = na.GetNeuronAt(x1 + 1, y);
                    Neuron nU = na.GetNeuronAt(x1 + 2, y);
                    Neuron nD = na.GetNeuronAt(x1 + 3, y);
                    Neuron nXU = na.GetNeuronAt(x1 + 4, y); //angle up
                    Neuron nXD = na.GetNeuronAt(x1 + 5, y); //angle down
                    /*
                     * L:   10
                     *      10
                     * R:   01
                     *      01
                     * U:   11
                     *      00
                     * D:   00
                     *      11
                     * XU:  01  //only one of the two 0's is needed
                     *      10
                     * XD:  01
                     *      10
                     * 
                     * */

                    nL.Label = "*|";
                    nR.Label = "|*";
                    nU.Label = @"/*\";
                    nD.Label = @"\*/";
                    nXU.Label = @"/";
                    nXD.Label = @"\";

                    AddSynapse(n0, nXU, -.5f);
                    AddSynapse(n1, nXU, .75f);
                    AddSynapse(n2, nXU, .75f);
                    AddSynapse(n3, nXU, -.5f);

                    AddSynapse(n0, nXD, .75f);
                    AddSynapse(n1, nXD, -.5f);
                    AddSynapse(n2, nXD, -.5f);
                    AddSynapse(n3, nXD, .75f);


                    AddSynapse(n0, nL, .5f);
                    AddSynapse(n1, nL, -1f);
                    AddSynapse(n2, nL, .5f);
                    AddSynapse(n3, nL, -1f);

                    AddSynapse(n0, nR, -1f);
                    AddSynapse(n1, nR, .5f);
                    AddSynapse(n2, nR, -1f);
                    AddSynapse(n3, nR, .5f);

                    AddSynapse(n0, nU, .5f);
                    AddSynapse(n1, nU, .5f);
                    AddSynapse(n2, nU, -1f);
                    AddSynapse(n3, nU, -1f);

                    AddSynapse(n0, nD, -1f);
                    AddSynapse(n1, nD, -1f);
                    AddSynapse(n2, nD, .5f);
                    AddSynapse(n3, nD, .5f);


                }
        }

        void AddSynapse(Neuron source, Neuron dest, float weight)
        {
            if (source == null || dest == null) return;
            source.AddSynapse(dest.id, weight);
        }

    }
}

