//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleRobot : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleRobot()
        {
            minHeight = 8;
            maxHeight = 12;
            minWidth = 15;
            maxWidth = 20;
        }

        //fill this method in with code which will execute
        //once for each cycle of the engine
        string[] labels = new string[] { "rotate", "shoulder", "elbow", "wrist", "grip", "Rwrist" };
        int[] home = new int[] { 5, 7, 8, 4, 4, 5 };
        int[] curPos = new int[] { 500, 500, 500, 500, 500, 500 };
        int[,] savedPos = new int[11, 6];

        public override void Fire()
        {
            Init();
            if (!na.GetNeuronAt(0, 0).InUse())
            {
                for (int j = 0; j < labels.Length; j++)
                    na.GetNeuronAt(0, j).Label = labels[j];
                na.GetNeuronAt(0, 6).Label = "Home";
                Home();
            }
            if (na.GetNeuronAt(0, 6).LastCharge > .9)
                Home();

            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 11; j++)
                {

                    Neuron n = na.GetNeuronAt(j, i);
                    if (n != null && n.LastCharge > .9)
                    {
                        MoveRobot(i, j * 200 + 500);
                        break;
                    }
                }
            }
            for (int j = 0; j < 11; j++)
            {
                Neuron n = na.GetNeuronAt(j, 8);
                if (n != null && n.LastCharge > .9)
                {
                    MoveRobot(8, j * 200 + 500);
                    break;
                }
            }
            for (int j = 0; j < 11; j++)
            {
                Neuron n = na.GetNeuronAt(j, 9);
                if (n != null && n.LastCharge > .9)
                {
                    MoveRobot(9, j * 200 + 500);
                    break;
                }
            }


            for (int i = 0; i < 6; i++)
            {
                int nudge = 0;
                if (na.GetNeuronAt(11, i).LastCharge > .9) nudge = 20;
                if (na.GetNeuronAt(12, i).LastCharge > .9) nudge = -20;
                if (nudge != 0)
                    MoveRobot(i, curPos[i] + nudge);
            }
            for (int i = 1; i < 11; i++)
            {
                if (na.GetNeuronAt(i, 6).LastCharge > .9)
                {
                    if (savedPos[i, 0] == 0)
                    {
                        for (int j = 0; j < curPos.Length; j++)
                            savedPos[i, j] = curPos[j];
                    }
                    else
                    {
                        for (int j = 0; j < curPos.Length; j++)
                        {
                            if (j == 4) continue;  //don't change the gripper state
                            MoveRobot(j, savedPos[i, j]);
                        }
                    }
                }
            }
        }
        void Home()
        {
            for (int i = 0; i < home.Length; i++)
            { MoveRobot(i, home[i] * 200 + 500); }
        }
        //position is pulse-width ranging 500-2500
        void MoveRobot(int port, int position)
        {
            //curPos[port] = position;
            SerialPort sp = new SerialPort("COM5");

            string commandString = "#" + port + " P" + position + "T2000\r\n";// S100\r\n";
            try
            {
                sp.Open();
                sp.Write(commandString);
                sp.Close();
            }
            catch (Exception e) 
            {
                string x = e.Message; 
            }
        }



        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (na == null) return; //this is called the first time before the module actually exists
        }
    }
}
