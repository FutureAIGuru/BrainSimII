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
using System.Threading;

namespace BrainSimulator.Modules
{
    public class ModuleRobotOLD : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleRobotOLD()
        {
            minHeight = 2;
            maxHeight = 500;
            minWidth = 2;
            maxWidth = 500;
        }

        SerialPort sp = null;
        string serialPortName = "COM11";
        int[] sensorValues = new int[100];

        public override void Fire()
        {
            Init();  //be sure to leave this here

            HandleDataFromRobot();

            //motor control (platform) neurons
            if (Fired("Stop"))
            {
                MoveRobot(10, 0);
                MoveRobot(11, 0);
            }
            if (Fired("Fwd"))
            {
                MoveRobot(10, 5); //left
                MoveRobot(11, 5); //right
            }
            if (Fired("Back"))
            {
                MoveRobot(10, -25);
                MoveRobot(11, -25);
            }
            if (Fired("Right"))
            {
                MoveRobot(10, 40);
                MoveRobot(11, 15);
            }
            if (Fired("Left"))
            {
                MoveRobot(10, 15);
                MoveRobot(11, 40);
            }

            //Servo control neurons
            int incr = 5;
            for (int i = 0; i < 8; i++)
            {
                incr = 0;
                if (na.GetNeuronAt(2, i).Fired())
                    incr = 5;
                if (na.GetNeuronAt(3, i).Fired())
                    incr = -5;
                if (incr != 0)
                {
                    int currentPosition = sensorValues[i];
                    int newValue = currentPosition + incr;
                    int intValue = newValue;
                    MoveRobot(i, intValue, 250);
                }
            }


            //is a programming step firing?
            bool foundMacros = false;
            foreach (Neuron n in na.Neurons1)
            {
                if (foundMacros && n.Fired())
                    nextInstruction = GetActionNumber(n.Label);
                if (n.Label == "Init")
                {
                    foundMacros = true;
                    if (n.Fired())
                        InitializeBehaviors();
                }
            }


            ProcessActions();
            // UpdateDialog();
        }

        bool Fired(string neuronLabel)
        {
            return GetNeuron(neuronLabel)?.Fired()==true ? true : false;
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


        /*Home Position
         *    Actuator   Position
         *      10          90          motor stopped
         *      11          90
         *      7           90          gripper open
         *      6           90          wrist rotation centered
         *      5           180         wrist up
         *      4           130         elbow up a bit
         *      3           170         shoulder over base
         *      2           135         shoulder 45-degree right
         *      1           90          eye position centered
         *      0           90          eye position min
        */

        int GetActionNumber(string name)
        {
            int retVal = -1;
            retVal = actions.FindIndex(x => x.name == name);
            return retVal;
        }

        class Action
        {
            public string name = "";
            public List<int> actuators = new List<int>();
            public List<int> positions = new List<int>();
            public int timing = 0; //in ms
            public DateTime startTime;
            public bool relativeMotion = false;
        }

        List<Action> actions = new List<Action>();

        //List<int> nextInstruction = new List<int>();
        int nextInstruction = -1;
        Stack<int> instructionStack = new Stack<int>();

        void ProcessActions()
        {
            //  for (int i = 0; i < nextInstruction.Count; i++)
            {
                if (nextInstruction != -1)
                {
                    Action currentAction = actions[nextInstruction];
                    if (DateTime.Now >= currentAction.startTime)
                    {
                        if (currentAction.name.IndexOf("Call") == 0 || currentAction.name.IndexOf("Goto") == 0)
                        {
                            if (currentAction.name.IndexOf("Call") == 0) 
                                instructionStack.Push(nextInstruction + 1);
                            string name = currentAction.name.Substring(5);
                            //                            Debug.WriteLine("Calling " + name);
                            nextInstruction = GetActionNumber(name);
                            if (nextInstruction == -1)
                                return;
                        }
                        else if (currentAction.name == "Done")
                        {
                            if (instructionStack.Count == 0)
                            {
                                //Debug.WriteLine("Done");
                                nextInstruction = -1;
                            }
                            else
                            {
                                nextInstruction = instructionStack.Pop();
//                                Debug.WriteLine("Return" + nextInstruction);
                            }
                        }
                        else
                        //take the current action
                        {
                            for (int i = 0; i < currentAction.actuators.Count; i++)
                            {
                                if (!currentAction.relativeMotion)
                                    MoveRobot(currentAction.actuators[i], currentAction.positions[i], currentAction.timing);
                                else
                                    MoveRobot(currentAction.actuators[i], sensorValues[currentAction.actuators[i]] + currentAction.positions[i], currentAction.timing);
                            }
                            //set up the next action
                            nextInstruction++;
                        }
                        if (nextInstruction != -1)
                        {
                            Action nextAction = actions[nextInstruction];
                            nextAction.startTime = DateTime.Now + TimeSpan.FromMilliseconds(currentAction.timing);
                        }
                    }
                }
            }
        }


        string inputBuffer = "";
        void HandleDataFromRobot()
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
                    if (senseNumber < sensorValues.Length)
                        sensorValues[senseNumber] = senseValue;
                }

                if (inputLine.Contains("Initialization Complete"))
                {
                    InitSensors();
                    Thread.Sleep(1000);
                    InitServos();
                    Thread.Sleep(1000);
                    InitMotors();
                }
            }
        }

        private void InitMotors()
        {
            //Left
            SendDataToRobot("S10 x2 p0 m1 M10000 e1 t10 T200 \n"); //increasing rev
            SendDataToRobot("S12 x3 p10 m1 M10000 e1 t10 T20000 \n");
            SendDataToRobot("A10 x0 p2 t0 c1 T0 e1 \n");
            //Right
            SendDataToRobot("S11 x2 p1 m1 M10000 e1 t10 T200 \n"); //increasing fwd
            SendDataToRobot("S13 x3 p11 m1 M10000 e1 t10 T20000 \n");
            SendDataToRobot("A11 x0 p3 t0 c1 T0 e1 \n");
        }


        private void InitServos()
        {
            //configure actuators
            //pin numbers refer to actual pins
            SendDataToRobot("A0 x1 p6 t0 T90 e1 t1000 \n");
            SendDataToRobot("A1 x1 p7 t0 T90 e1 m90 t1000 \n");
            Thread.Sleep(1000);

            SendDataToRobot("A7 x1 p13 t0 T90 e1 t1000 \n");
            SendDataToRobot("A6 x1 p12 t0 T90 e1 t1000 \n");
            SendDataToRobot("A5 x1 p11 t0 T90 e1 t3000 \n");
            Thread.Sleep(1000);
            SendDataToRobot("A4 x1 p10 t0 T135 e1 t3000\n");
            Thread.Sleep(1000);
            SendDataToRobot("A3 x1 p9 t0 T160 e1 t3000\n");
            Thread.Sleep(1000);
            SendDataToRobot("A2 x1 p8 t0 T60 e1 t3000\n");
        }

        private void InitSensors()
        {
            //init sensors
            //pin numbers for servo sensors refer to servo numbers
            SendDataToRobot("S0 x1 p0 m1 e1 t100 T200 \n");
            SendDataToRobot("S1 x1 p1 m1 e1 t100 T200 \n");
            SendDataToRobot("S2 x1 p2 m1 e1 t100 T200 \n");
            SendDataToRobot("S3 x1 p3 m1 e1 t100 T200 \n");
            SendDataToRobot("S4 x1 p4 m1 e1 t100 T500 \n");
            SendDataToRobot("S5 x1 p5 m1 e1 t100 T500 \n");
            SendDataToRobot("S6 x1 p6 m1 e1 t100 T500 \n");
            SendDataToRobot("S7 x1 p7 m1 e1 t100 T200 \n");
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
                Debug.WriteLine("SEND FAILED: " + e.Message + " ATTEMPTING TO SEND " + data);
            }
        }


        public override void Initialize()
        {
            Init();
            if (na == null) return;

            //this might be useful someday If more than one, put up a selection dropdown
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


            InitializeBehaviors();
        }

        void InitializeBehaviors()
        {
            actions = new List<Action>(){
            new Action{
                name = "Home",
            },
            new Action{
                name = "Call Reach",
            },
            new Action{
                relativeMotion = false,
                actuators = new List<int> { 0,1,6},
                positions = new List<int> { 90,90,90 },
                timing = 1000,
            },
            new Action{
                relativeMotion = false,
                actuators = new List<int> { 5,4,3,2,0 },
                positions = new List<int> { 160,155,160,60,100 },
                timing = 3000,
            },
            new Action{
                name = "Done",
            },
            new Action{
                name = "Grip",
                relativeMotion = false,
                actuators = new List<int> { 7 },
                positions = new List<int> { 160 },
                timing = 1000,
            },
            new Action{
                name = "Done",
            },
            new Action{
                name = "Release",
                relativeMotion = false,
                actuators = new List<int> { 7 },
                positions = new List<int> { 45 },
                timing = 1000,
            },
            new Action{
                name = "Done",
            },
            new Action{
                name = "Reach",
                relativeMotion = false,
                actuators = new List<int> { 5,4,3 },
                positions = new List<int> { 40,60,50 },
                timing = 2000,
            },
            new Action{
                name = "Done",
            },
            new Action{
                name = "Lift",
                actuators = new List<int> { 5 },
                positions = new List<int> { 47 },
                timing = 100,
            },
            new Action{
                actuators = new List<int> { 5,4,3 },
                positions = new List<int> { 46,68,42 },
                timing = 100,
            },
            new Action{
                actuators = new List<int> { 5,4,3 },
                positions = new List<int> { 37,65,49 },
                timing = 2000,
            },
            new Action{
                actuators = new List<int> {6 },
                positions = new List<int> {90},
                timing = 100,
            },
            new Action{
                name = "Done",
            },
            new Action{
                name = "Down",
                actuators = new List<int> {6 },
                positions = new List<int> {180 },
                timing = 100,
            },
            new Action{
                actuators = new List<int> { 5,4,3 },
                positions = new List<int> { 45,70,40 },
                timing = 1000,
            },
            new Action{
                name = "Done",
            },
            new Action{
                name = "Grasp",
                timing = 0,
            },
            new Action{
                name = "Call Grip",
                timing = 0,
            },
            new Action{
                name = "Call Lift",
                timing = 0,
            },
            new Action{
                name = "Done",
            },

            new Action{
                name = "PutDown",
            },
            new Action{
                name = "Call Down",
                timing = 0,
            },
            new Action{
                name = "Call Release",
                timing = 0,
            },
            new Action{
                name = "Call Lift",
                timing = 0,
            },
            new Action{
                name = "Done",
            },

            new Action{
                name = "MoveThing",
            },
            new Action{
                name = "Call Grasp",
                timing = 0,
            },
            new Action{
                relativeMotion = true,
                actuators = new List<int> { 2 },
                positions = new List<int> { -65 },
                timing = 1000,
            },
            new Action{
                name = "Call PutDown",
                timing = 0,
            },
            new Action{
                relativeMotion = true,
                actuators = new List<int> { 2},
                positions = new List<int> { 50},
                timing = 1000,
            },
            new Action{
                timing=2000,
            },
            new Action{
                name = "Call Down",
            },
            new Action{
                name = "Done",
            },
            new Action{
                name = "P1",
            },
            new Action{
                actuators = new List<int> { 2 },
                positions = new List<int> { 170 },
                timing = 1000,
            },
            new Action{
                actuators = new List<int> { 5,4,3 },
                positions = new List<int> { 37,65,49 },
                timing = 2000,
            },
            new Action{
                timing = 3000,
            },
            new Action{
                name = "Call Down",
            },
            new Action{
                name = "Done",
            },
            new Action{
                name = "P2",
            },
            new Action{
                name = "Call Lift",
            },
            new Action{
                name = "Call Reach",
            },
            new Action{
                actuators = new List<int> { 2},
                positions = new List<int> { 110 },
                timing = 1000,
            },
            new Action{
                name = "Call Down",
            },
            new Action{
                name = "Done",
            },

            new Action{
                name = "PNext",
            },
            new Action{
                name = "Call Lift",
            },
            new Action{
                relativeMotion=true,
                actuators = new List<int> { 2 },
                positions = new List<int> { -10},
                timing = 1000,
            },
            new Action{
                name = "Call Down",
            },
            new Action{
                name = "Done",
            },

            new Action{
                name = "Eye",
                timing=1000,
            },
            new Action{
                actuators = new List<int> { 0},
                positions = new List<int> { 45},
                timing = 2000,
            },
            new Action{
                actuators = new List<int> { 0,1 },
                positions = new List<int> { 90, 140},
                timing = 2000,
            },
            new Action{
                actuators = new List<int> { 0,1},
                positions = new List<int> {135,120},
                timing = 2000,
            },
            new Action{
                name = "Goto Eye",
            },

            new Action{
                name = "KTurn",
                timing=5000,
            },
            new Action{ //F
                relativeMotion = false,
                actuators = new List<int> { 10,11},
                positions = new List<int> { 90,90 },
                timing = 2000,
            },
            new Action{ //R
                relativeMotion = false,
                actuators = new List<int> { 10,11},
                positions = new List<int> { 120,60 },
                timing = 4300,
            },
            new Action{ //F
                relativeMotion = false,
                actuators = new List<int> { 10,11},
                positions = new List<int> { 90,90 },
                timing = 400,
            },
            new Action{ //stop
                relativeMotion = false,
                actuators = new List<int> { 10,11},
                positions = new List<int> { 0,0 },
            },
            new Action{
                timing=1000,
            },
            new Action{ //-R
                relativeMotion = false,
                actuators = new List<int> { 10,11},
                positions = new List<int> { -60,-120 },
                timing = 4100,
            },
            new Action{ //stop
                relativeMotion = false,
                actuators = new List<int> { 10,11},
                positions = new List<int> { 0,0 },
            },
            new Action{
                timing=1000,
            },
            new Action{
                relativeMotion = false,
                actuators = new List<int> { 10,11},
                positions = new List<int> { 120,120 },
                timing = 4500,
            },
            new Action{
                relativeMotion = false,
                actuators = new List<int> { 10,11},
                positions = new List<int> { 0,0 },
            },
            new Action{
                name = "Done",
            },

        };

            int actionPtr = 0;
            bool foundStartingPoint = false;
            foreach (Neuron n in na.Neurons1)
            {
                if (foundStartingPoint)
                {
                    if (actionPtr < actions.Count)
                    {
                        n.inUse = true;
                        n.Label = actions[actionPtr++].name;
                        while (actionPtr < actions.Count && (actions[actionPtr].name == "" || actions[actionPtr].name == "Done" || actions[actionPtr].name.IndexOf("Call") == 0))
                            actionPtr++;
                    }
                    else
                    {
                        n.inUse = false;
                        n.Label = "";
                    }
                }
                if (n.Label == "Init")
                {
                    foundStartingPoint = true;
                }
            }

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


        void MoveRobot(int servo, int position, int timing = 2000)
        {
            if (!sp.IsOpen) sp.Open();

            string commandString = "A" + servo + " t" + timing + " T" + position + " \n"; //" s6  100 \n";
            try
            {
                SendDataToRobot(commandString);
            }
            catch (Exception e)
            {
                string x = e.Message;
            }
        }

        public override void SizeChanged()
        {
            if (na == null) return; //this is called the first time before the module actually exists
        }
    }
}
