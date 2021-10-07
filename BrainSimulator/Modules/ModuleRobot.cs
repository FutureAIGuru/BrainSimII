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
using System.Diagnostics;
using System.Windows.Threading;

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
            minHeight = 5;
            maxHeight = 12;
            minWidth = 5;
            maxWidth = 20;
        }

        //fill this method in with code which will execute
        //once for each cycle of the engine
        string[] labels = new string[] { "rotate", "shoulder", "elbow", "wrist", "grip", "Rwrist" };
        int[] home = new int[] { 5, 7, 8, 4, 4, 5 };
        int[] curPos = new int[] { 500, 500, 500, 500, 500, 500 };
        int[,] savedPos = new int[11, 6];


        string comPort = "COM11";
        DispatcherTimer dt = null;
        SerialPort sp = null;
        float[] sensorValues = new float[100];
        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            if (!sp.IsOpen)
            {
                Initialize();
                if (!sp.IsOpen)
                    return;
            }


            if (sensorValues[6] == 0 && near(sensorValues[7], .5f))
            {
                MoveRobot(6, 180);
                MoveRobot(7, 180);
            }
            else if (sensorValues[7] == 1 && sensorValues[7] == 1)
            {
                MoveRobot(7, 90);
            }
            else if (sensorValues[6] == 1 && near(sensorValues[7],.5f))
            {
                MoveRobot(6, 0);
            }
            na.GetNeuronAt(0, 0).SetValue(sensorValues[6]);
            na.GetNeuronAt(0, 1).SetValue(sensorValues[7]);

            //if you want the dlg to update, use the following code whenever any parameter changes
            // UpdateDialog();
        }

        bool near (float f1, float f2, float tolerance = 0.05f)
        {
            return Math.Abs(f2 - f1) < tolerance;

        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            Init();
            if (na == null) return;

            string[] portNames = SerialPort.GetPortNames();

            if (sp == null || !sp.IsOpen)
            {
                try
                {
                    sp = new SerialPort(comPort);
                    if (!sp.IsOpen)
                        sp.Open();
                }
                catch (Exception e)
                {
                    MessageBox.Show("ModuleRobot port open failed because: " + e.Message);
                    return;
                }
            }
            sp.BaudRate = 115200;
            sp.DataReceived += Sp_DataReceived;

            //set up timeout
            if (dt == null)
            {
                dt = new DispatcherTimer();
                dt.Tick += sp_Timeout;
            }
            dt.Interval = new TimeSpan(0, 0, 10);

            //init test sensore
            SendCommand("s6 100 s7 100 \n");
            MoveRobot(6, 180);
            MoveRobot(7, 180);
        }

        void SendCommand(string theCommand)
        {
            dt.Stop();
            dt.Start();

            try
            {
                if (sp != null && !sp.IsOpen) sp.Open();
                sp.Write(theCommand);
                Debug.WriteLine("ModuleRobot sent: " + theCommand.Trim());
            }
            catch (Exception e)
            {
                string s = e.Message;
                Debug.WriteLine("ModuleRobot:SendCommand failed because:" + s);
            }
        }
        private void sp_Timeout(object sender, EventArgs e)
        {
            dt.Stop();
            Debug.WriteLine("Port timed out");
            if (sp.IsOpen) sp.Close();
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
            Initialize();
        }

        //data incoming from the robot...  handle sense values
        private void Sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            dt.Stop();
            dt.Start();  //reset the watchdog timer
            if (!sp.IsOpen) sp.Open();
            string indata = sp.ReadExisting();
            string[] commands = indata.Split(new char[] { '\n', }, StringSplitOptions.RemoveEmptyEntries); //multiple commands in input string
            foreach (string s in commands)
            {
                Debug.WriteLine(s.Trim());
                // sX:Y where X is the sensor number and Y is the value
                if (s.Contains(":") && s[0] == 's')
                {
                    string s1 = s.Trim();
                    string[] parameters = s1.Split(':');
                    int.TryParse(parameters[0].Substring(1), out int senseNumber);
                    int.TryParse(parameters[1], out int senseValue);
                    float newVal = senseValue / 180.0f; //for servos with range 0-180
                    if (senseNumber < sensorValues.Length)
                        sensorValues[senseNumber] = newVal;
                }
            }
        }

        void MoveRobot(int servo, int position)
        {
            string commandString = "t 2000 m" + servo + " " + position + " \n";
            SendCommand(commandString);
            sensorValues[servo] = -1;
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
