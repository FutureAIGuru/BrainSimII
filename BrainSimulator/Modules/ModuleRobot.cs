//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using System.Diagnostics;
using System.IO.Ports;

namespace BrainSimulator.Modules
{
    public class ModuleRobot : ModuleBase
    {
        public ModuleRobot()
        {
            minHeight = 2;
            maxHeight = 500;
            minWidth = 3;
            maxWidth = 3;
        }


        SerialPort sp = null;
        string serialPortName = "COM11";
        bool robotInitialized = false;

        DateTime lastCycleTime = DateTime.Now;

        public string configString =
@"
//Here's a sample configuration file
//This is what you'll get when you first instantiate this module

//Left Motor
Actuator Left x0 p2 t0 c1 T90 e1 
Sensor LPos x2 p0 m0 M10000 e1 t10 T200 
Sensor LRate x3 PLPos m1 M10000 e1 t10 T200

//Right Motor
Actuator . .
Actuator Right x0 p3 t0 c1 T90 e1 
Actuator . .
Sensor RPos x2 p1 m1 M10000 e1 t10 T200
Sensor RRate x3 PRPos m1 M10000 e1 t10 T200

//Eyes
Actuator EyeH x1 p6 t0 T90 e1 t100 
Actuator EyeV x1 p7 t0 T90 e1 m90 t100 
Sensor EyeH x1 PEyeH m1 e1 t100 T200
Sensor EyeV x1 PEyeV m1 e1 t100 T200

//Shoulder Lift
Actuator Lift x1 p9 t0 T160 e1 t3000
Sensor Rot x1 PLift m1 e1 t100 T200

//Elbow
Actuator Elbow x1 p10 t0 T135 e1 t3000
Sensor Rot x1 PElbow m1 e1 t100 T200

//Shoulder Rotation
Actuator Rot x1 p8 t0 T60 e1 t3000
Sensor Rot x1 PRot m1 e1 t100 T200

//Wrist
Actuator Wrist x1 p11 t0 T90 e1 t3000
Sensor Rot x1 PWrist m1 e1 t100 T200

//WRot
Actuator WRot x1 p12 t 0 T90 e1 t3000
Sensor Rot x1 PWRot m1 e1 t100 T200

//Grip
Actuator Grip x1 p13 t 0 T90 e1 t3000
Sensor Rot x1 PGrip  m1 e1 t100 T200

//Turning sensor (z-axis rotation)
Sensor Yaw x4 p5 t100 T200 e1 m1 

//Pitch sensor (x-axis rotation)
Sensor Pitch x4 p3 t100 T200 e1 m1 
";
        int configStringLinePointer = -1;
        string[] configStringLines = new string[] { };

        int sensorCount = 0;
        int actuatorCount = 0;

        public override void Fire()
        {
            Init();  //be sure to leave this here

            HandleMessagesFromRobot();

            if (!robotInitialized) return;

            //currently outputting config string
            if (configStringLinePointer >= 0)
            {
                if (configStringLinePointer < configStringLines.Length)
                {
                    string currentLine = configStringLines[configStringLinePointer++];
                    string[] theParams = currentLine.Split(new char[] { ' ' });
                    if (theParams.Length > 2 && theParams[0].IndexOf("//") != 0)//ignore comment and blank lines
                    {
                        Neuron n = null;
                        Neuron nPrev = null;
                        float value = 0;
                        switch (theParams[0])
                        {
                            case "Sensor":
                                while (sensorCount >= na.Height - 1)
                                    na.Height++;
                                n = na.GetNeuronAt(2, 1 + sensorCount);
                                nPrev = na.GetNeuronAt(3, 1 + sensorCount);

                                if (n != null)
                                {
                                    n.Label = theParams[1];
                                    n.Model = Neuron.modelType.FloatValue;
                                    n.SetValue(value);
                                }

                                theParams[1] = "S" + sensorCount;
                                if (theParams[3][0] == 'P')
                                {
                                    //allow for reference by name (back ref only)
                                    string theName = theParams[3].Substring(1);
                                    Neuron n1 = na.GetNeuronAt(theName);
                                    if (n1 != null)
                                    {
                                        na.GetNeuronLocation(n1, out int x, out int y);
                                        theParams[3] = "p" + (y - 1);
                                    }
                                    else
                                    {
                                        Debug.WriteLine("Parsing initialization string error, referenced Label: " + theName);
                                        return;

                                    }
                                }
                                sensorCount++;
                                break;
                            case "Actuator":
                                n = na.GetNeuronAt(0, 1 + actuatorCount);
                                nPrev = na.GetNeuronAt(1, 1 + actuatorCount);
                                if (theParams[1] != ".")
                                    value = 0.5f;
                                while (actuatorCount >= na.Height - 1)
                                    na.Height++;
                                if (n != null)
                                {
                                    n.Label = theParams[1];
                                    n.Model = Neuron.modelType.FloatValue;
                                    n.SetValue(value);
                                }
                                if (nPrev != null)
                                {
                                    nPrev.Model = Neuron.modelType.FloatValue;
                                    nPrev.SetValue(0);
                                }


                                theParams[1] = "A" + actuatorCount;
                                for (int i = 0; i < theParams.Length; i++)
                                {
                                    if (theParams[i] != "" && theParams[i][0] == 'T')
                                    {
                                        if (int.TryParse(theParams[i].Substring(1), out int val))
                                        {
                                            value = (float)val / 180f;
                                        }
                                    }
                                }
                                actuatorCount++;
                                break;
                        }
                        currentLine = String.Join(" ", theParams.Skip(1)).Trim();
                        SendStringToRobot(currentLine);
                    }
                }
                else
                    configStringLinePointer = -1;
                MainWindow.Update();
                return;
            }

            //limit all motor neuron values
            for (int j = 0; j < na.Height; j++)
            {
                Neuron n = na.GetNeuronAt(0, j);
                if (n != null)
                {
                    if (n.LastCharge < 0) n.SetValue(0);
                    if (n.LastCharge > 1) n.SetValue(1);
                }
            }


            //send action to robot
            Neuron nTiming = na.GetNeuronAt(0, 0);
            for (int i = 0; i < na.Width - 1; i += 2)
            {
                for (int j = 1; j < na.Height; j++)
                {
                    Neuron nCurrent = na.GetNeuronAt(i, j);
                    Neuron nPrevious = na.GetNeuronAt(i + 1, j);
                    if (nCurrent.LastCharge != nPrevious.LastCharge)
                    {
                        if (i == 0)
                        {
                            SendActuatorValueToRobot(j - 1, nCurrent.LastCharge, nTiming.LastCharge);
                            na.GetNeuronAt("Busy").SetValue(na.GetNeuronAt("Timing").LastCharge);
                        }
                        nPrevious.SetValue(nCurrent.LastCharge);
                    }
                    if (nCurrent.LeakRate != nPrevious.LeakRate)
                    {
                        if (i == 0)
                            SendActuatorEnabledToRobot(j - 1, nCurrent.LeakRate >= 0);
                        nPrevious.LeakRate = nCurrent.LeakRate;
                        MainWindow.Update();
                    }
                }
            }

            TimeSpan elapsed = DateTime.Now - lastCycleTime;
            Neuron nBusy = na.GetNeuronAt("Busy");
            float newValue = nBusy.LastCharge;
            newValue -= (float)elapsed.TotalMilliseconds / 10000f;
            if (newValue < 0) newValue = 0;
            nBusy.SetValue(newValue);

            lastCycleTime = DateTime.Now;
        }

        string inputBuffer = "";
        void HandleMessagesFromRobot()
        {
            if (sp == null || !sp.IsOpen) return;
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
                    Neuron n = na.GetNeuronAt(2, senseNumber + 1);
                    if (n != null)
                        n.SetValue(senseValue / 180f);
                }

                if (inputLine.Contains("Initialization Complete"))
                {
                    robotInitialized = true;
                }
            }
        }

        void SendActuatorValueToRobot(int actuator, float value, float timing)
        {
            if (!sp.IsOpen) sp.Open();
            int intTiming = (int)(timing * 10000);
            int intValue = (int)(value * 180);
            string commandString = "A" + actuator + " t" + intTiming + " T" + intValue + " \n";
            SendStringToRobot(commandString);
        }
        void SendActuatorEnabledToRobot(int actuator, bool enabled)
        {
            string commandString = "A" + actuator + " e" + (enabled ? "1" : "0") + " \n";
            SendStringToRobot(commandString);
        }

        void SendStringToRobot(string str)
        {
            try
            {
                str = str.Trim() + " \n";
                sp.Write(str);
                Debug.Write("SEND: " + str);
            }
            catch (Exception e)
            {
                Debug.WriteLine("SEND FAILED: " + e.Message + " ATTEMPTING TO SEND " + str);
            }
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            foreach (Neuron n in na.Neurons1)
            {
                n.Label = "";
            }
            if (na.GetNeuronAt(0, 0) != null)
                na.GetNeuronAt(0, 0).Label = "Timing";
            if (na.GetNeuronAt(1, 0) != null)
                na.GetNeuronAt(1, 0).Label = "Busy";
            if (na.GetNeuronAt(2, 0) != null)
                na.GetNeuronAt(2, 0).Label = "Sensor";

            sensorCount = 0;
            actuatorCount = 0;

            if (sp?.IsOpen != true)
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
            robotInitialized = false;

            configStringLines = configString.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            configStringLinePointer = 0;
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
            Init();
            Initialize();
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
