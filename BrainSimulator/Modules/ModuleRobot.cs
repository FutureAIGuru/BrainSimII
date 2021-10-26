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
using System.Windows.Threading;
using System.Xml.Serialization;
using System.Diagnostics;

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
            minHeight = 2;
            maxHeight = 500;
            minWidth = 2;
            maxWidth = 500;
        }

        SerialPort sp = null;
        string serialPortName = "COM7";
        float[] sensorValues = new float[100];

        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if (!sp.IsOpen)
            //{
            //    Initialize();
            //    if (!sp.IsOpen)
            //        return;
            //}

            HandleDataFromRobot();

            TestServo();
            if (near(sensorValues[7], .5f))
                MoveRobot(7, 160);
            if (near(sensorValues[7], .88f))
                MoveRobot(7, 90);

            if (near(sensorValues[6], .5f))
                MoveRobot(6, 160);
            if (near(sensorValues[6], .88f))
                MoveRobot(6, 90);

            if (near(sensorValues[5], .5f))
                MoveRobot(5, 160);
            if (near(sensorValues[5], .88f))
                MoveRobot(5, 90);

            if (near(sensorValues[4], .83f))
                MoveRobot(4, 130);
            if (near(sensorValues[4], .72f))
                MoveRobot(4, 150);

            if (near(sensorValues[0], 0, .07f) && near(sensorValues[1], .5f, .02f))
            {
                MoveRobot(0, 180);
                MoveRobot(1, 180);
            }
            else if (near(sensorValues[0], 1, .01f) && near(sensorValues[1], 1, .02f))
            {
                MoveRobot(1, 90);
            }
            else if (near(sensorValues[0], 1, .01f) && near(sensorValues[1], .5f, .02f))
            {
                MoveRobot(0, 0);
            }

            na.GetNeuronAt(0, 0).SetValue(sensorValues[0]);
            na.GetNeuronAt(0, 1).SetValue(sensorValues[1]);

            // UpdateDialog();
        }

        bool near(float f1, float f2, float tolerance = 0.05f)
        {
            return Math.Abs(f2 - f1) < tolerance;
        }

        //actuator assignments
        //Platform: 85 = off <:forward >:reverse
        //p2=right p3=left
        //Servos:
        //p6 eye L/R (pan) 0:L 180:R
        //p7 eye U/D (tilt) min 90:slightly down  180:straight up;
        //p8 base-rotation (shoulder)
        //p9 Shoulder servo  Max 175: back
        //p10 elbow 0:straighten MAX 145 bend
        //p11 wrist bend 0:close in   180 max back
        //p12 write rotation 10 180
        //p13 gripper close 50:full upen  180: closed  (TEMP p5)


        string inputBuffer = "";
        void HandleDataFromRobot()
        {
            if (!sp.IsOpen) return;
            inputBuffer += sp.ReadExisting();
            int lineBreak = inputBuffer.IndexOf('\n');
            while (lineBreak != -1)
            {
                //strip the first command off the head of the input buffer
                string inputLine = inputBuffer.Substring(0, lineBreak - 1).Trim();
                Debug.WriteLine("RECD:" + inputLine); //put everything you receive in the debug display but only process sensor input
                inputBuffer = inputBuffer.Substring(lineBreak + 1);
                lineBreak = inputBuffer.IndexOf('\n');

                //process the command
                if (inputLine != "" && inputLine[0] == 'S')
                {
                    string[] parameters = inputLine.Split(':');
                    int.TryParse(parameters[0].Substring(1), out int senseNumber);
                    int.TryParse(parameters[1], out int senseValue);
                    float newVal = senseValue / 180.0f; //for servos with range 0-180
                    if (senseNumber < sensorValues.Length)
                        sensorValues[senseNumber] = newVal;
                }

                if (inputLine.Contains("Initialization Complete"))
                {
                    //init sensors
                    //pin numbers for servo sensors refer to servo numbers
                    SendDataToRobot("S0 x1 p0 m1 e1 t100 T200 \n");
                    SendDataToRobot("S1 x1 p1 m1 e1 t100 T200 \n");
                    //SendDataToRobot("S2 x1 p2 m1 e1 t100 T200 \n");
                    //SendDataToRobot("S3 x1 p3 m1 e1 t100 T200 \n");
                    SendDataToRobot("S4 x1 p4 m1 e1 t100 T500 \n");
                    SendDataToRobot("S5 x1 p5 m1 e1 t100 T500 \n");
                    SendDataToRobot("S6 x1 p6 m1 e1 t100 T500 \n");
                    SendDataToRobot("S7 x1 p7 m1 e1 t100 T200 \n");

                    //configure actuators
                    //pin numbers refer to actual pins
                    SendDataToRobot("A0 p6 x1 t0 T90 e1 t4000 \n");
                    SendDataToRobot("A1 p7 x1 t0 T90 e1 m90 t4000 \n");
                    //SendDataToRobot("A2 p8 x1 e1 t4000\n");
                    //SendDataToRobot("A3 p9 x1 e1 t4000\n");
                    SendDataToRobot("A4 p10 x1 t0 T130 e1 t5000\n");
                    SendDataToRobot("A5 p11 x1 t0 T90 e1 t4000 \n");
                    SendDataToRobot("A6 p12 x1 t0 T90 e1 t4000 \n");
                    SendDataToRobot("A7 p5 x1 t0 T90 e1 t4000 \n");

                    //set initial robot position
                    MoveRobot(0, 180);
                    MoveRobot(1, 180);

                    //MoveRobot(2, 80);   //shoulder rot
                    // MoveRobot(3, 160);  //shoulder ang
                    // MoveRobot(4, 90);   //elbow
                    //MoveRobot(5, 90);   //wrist
                    //MoveRobot(6, 90);   //wrist rot
                    //MoveRobot(7, 160);   //gripper


                }
            }
        }

        private void TestServo()
        {

        }

        private void SendDataToRobot(string data)
        {
            try
            {
                sp.Write(data);
                Debug.Write("SEND: " + data);
            }
            catch (Exception e)
            {
                Debug.WriteLine("SEN FAILED: " + e.Message + " ATTEMPTING TO SEND " + data);
            }
        }


        DispatcherTimer dt = null;

        public override void Initialize()
        {
            Init();
            if (na == null) return;

            string[] portNames = SerialPort.GetPortNames();

            if (sp == null || !sp.IsOpen)
            {
                try
                {
                    if (sp == null)
                        sp = new SerialPort(serialPortName);
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
            sp.Handshake = Handshake.None;

            //reset the arduino processor
            sp.DtrEnable = true;
            sp.DtrEnable = false;

            ////set up timeout
            //if (dt == null)
            //{
            //    dt = new DispatcherTimer();
            //    dt.Tick += sp_Timeout;
            //}
            //dt.Interval = new TimeSpan(0, 0, 5);
        }

        private void sp_Timeout(object sender, EventArgs e)
        {
            dt.Stop();
            Debug.WriteLine("Port timed out");
            //if (sp.IsOpen) sp.Close();
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


        void MoveRobot(int servo, int position)
        {
            //dt.Stop();
            //dt.Start();
            if (!sp.IsOpen) sp.Open();

            string commandString = "A" + servo + " T" + position + " \n"; //" s6  100 \n";
            try
            {
                SendDataToRobot(commandString);
                sensorValues[servo] = -1; //set the sensor value to an invalid value 
            }
            catch (Exception e)
            {
                string x = e.Message;
            }
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
