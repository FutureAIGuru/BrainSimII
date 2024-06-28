//
// Copyright (c) Charles Simon. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using System.Diagnostics;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Net.NetworkInformation;

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

        // needed to get the IP address of the ESP8266 device
        UdpClient serverClient = null; //listen only
        UdpClient clientServer; //send/broadcast only
        IPAddress broadCastAddress;
        int clientServerPort = 4444;
        int serverClientPort = 4444;
        int tcpClientPort   = 44444;
        IPAddress theIP = null;
        TcpClient theTcpClient = new TcpClient { };
        NetworkStream theTcpStream = null;
        static bool robotOnWiFi = false;
        SerialPort sp = null;
        string serialPortName = RetrieveSerialPort();
        bool robotInitialized = false;

        DateTime lastCycleTime = DateTime.Now;

        public string configString = RetrieveConfigString();

        int configStringLinePointer = -1;
        string[] configStringLines = new string[] { };

        int sensorCount = 0;
        int actuatorCount = 0;

        public static string RetrieveWifiRobotIdentifier()
        {
            string result = Environment.GetEnvironmentVariable("BS2_ROBOT_IDENTIFIER");
            if (result == null)
            {
                // robot NOT on WiFi but Serial if environment variable not set...
                robotOnWiFi = false;
                return "";
            }
            robotOnWiFi = true;
            return result;
        }

        public static string RetrieveSerialPort()
        {
            string result = Environment.GetEnvironmentVariable("BS2_ROBOT_COMPORT");
            if (result == null)
            {
                // Default is as before...
                result = "COM11";
            }
            return result;
        }

        public static string RetrieveConfigString()
        {
            string configFilename = Environment.GetEnvironmentVariable("BS2_ROBOT_CONFIG");
            string configString = "";
            if (configFilename == null)
            {
                configFilename = "DEFAULT";
                // Default is as before...
                configString = @"
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
            }
            else
            {
                Console.WriteLine("Reading robot config" + configFilename);
                configString = File.ReadAllText(configFilename);
            }
            return configString;
        }

        public override void Fire()
        {
            if (theIP == null)
            {
                Broadcast("RobotPoll");
                theIP = new IPAddress(new byte[] { 127, 0, 0, 1 });
            }

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
                                while (sensorCount >= mv.Height - 1)
                                    mv.Height++;
                                n = mv.GetNeuronAt(2, 1 + sensorCount);
                                nPrev = mv.GetNeuronAt(3, 1 + sensorCount);

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
                                    Neuron n1 = mv.GetNeuronAt(theName);
                                    if (n1 != null)
                                    {
                                        mv.GetNeuronLocation(n1, out int x, out int y);
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
                                n = mv.GetNeuronAt(0, 1 + actuatorCount);
                                nPrev = mv.GetNeuronAt(1, 1 + actuatorCount);
                                if (theParams[1] != ".")
                                    value = 0.5f;
                                while (actuatorCount >= mv.Height - 1)
                                    mv.Height++;
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
            for (int j = 0; j < mv.Height; j++)
            {
                Neuron n = mv.GetNeuronAt(0, j);
                if (n != null)
                {
                    if (n.LastCharge < 0) n.SetValue(0);
                    if (n.LastCharge > 1) n.SetValue(1);
                }
            }


            //send action to robot
            Neuron nTiming = mv.GetNeuronAt(0, 0);
            for (int i = 0; i < mv.Width - 1; i += 2)
            {
                for (int j = 1; j < mv.Height; j++)
                {
                    Neuron nCurrent = mv.GetNeuronAt(i, j);
                    Neuron nPrevious = mv.GetNeuronAt(i + 1, j);
                    if (nCurrent.LastCharge != nPrevious.LastCharge)
                    {
                        if (i == 0)
                        {
                            SendActuatorValueToRobot(j - 1, nCurrent.LastCharge, nTiming.LastCharge);
                            mv.GetNeuronAt("Busy").SetValue(mv.GetNeuronAt("Timing").LastCharge);
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
            Neuron nBusy = mv.GetNeuronAt("Busy");
            float newValue = nBusy.LastCharge;
            newValue -= (float)elapsed.TotalMilliseconds / 10000f;
            if (newValue < 0) newValue = 0;
            nBusy.SetValue(newValue);

            lastCycleTime = DateTime.Now;
        }

        public void ReceiveFromServer()
        {
            while (true)
            {
                string incomingMessage = "";
                var from = new IPEndPoint(IPAddress.Any, serverClientPort);
                var recvBuffer = serverClient.Receive(ref from);
                incomingMessage += Encoding.UTF8.GetString(recvBuffer);
                if (incomingMessage == RetrieveWifiRobotIdentifier())
                {
                    theIP = from.Address;
                    theTcpClient.Connect(theIP, tcpClientPort);
                    theTcpStream = theTcpClient.GetStream();
                }
                Debug.WriteLine("Received from Device: " + from.Address + " " + incomingMessage);
            }
        }

        public void Broadcast(string message)
        {
            Debug.WriteLine("Broadcast: " + message);
            byte[] datagram = Encoding.UTF8.GetBytes(message);
            IPEndPoint ipEnd = new IPEndPoint(broadCastAddress, clientServerPort);
            clientServer.SendAsync(datagram, datagram.Length, ipEnd);
        }

        string inputBuffer = "";
        void HandleMessagesFromRobot()
        {
            if (robotOnWiFi)
            {
                if (theTcpStream == null) return;
                while (theTcpStream.DataAvailable)
                {
                    inputBuffer += theTcpStream.ReadByte();
                }
            }
            else
            {
                inputBuffer += sp.ReadExisting();
            }
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
                    Neuron n = mv.GetNeuronAt(2, senseNumber + 1);
                    if (n != null)
                        n.SetValue(senseValue / 180f);
                }

                if (inputLine.Contains("Heartbeat Active"))
                {
                    robotInitialized = true;
                }
            }
        }

        void SendActuatorValueToRobot(int actuator, float value, float timing)
        {
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
                if (robotOnWiFi)
                {
                    for (int i = 0; i < str.Length; i++)
                    {
                        theTcpStream.WriteByte((byte)(str[i]));
                    }
                }
                else
                {
                    sp.Write(str);
                }
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
            RetrieveWifiRobotIdentifier(); // make sure we know if the Robot works over WiFi or Serial

            //This gets the wifi IP address
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            byte[] ips = ip.Address.GetAddressBytes();
                            broadCastAddress = IPAddress.Parse(ips[0] + "." + ips[1] + "." + ips[2] + ".255");
                        }
                    }
                }
            }

            serverClient = new UdpClient(serverClientPort);
            serverClient.Client.ReceiveBufferSize = 10000000;

            clientServer = new UdpClient();
            clientServer.EnableBroadcast = true;

            Task.Run(() =>
            {
                ReceiveFromServer();
            });
            
            foreach (Neuron n in mv.Neurons)
            {
                n.Label = "";
            }
            if (mv.GetNeuronAt(0, 0) != null)
                mv.GetNeuronAt(0, 0).Label = "Timing";
            if (mv.GetNeuronAt(1, 0) != null)
                mv.GetNeuronAt(1, 0).Label = "Busy";
            if (mv.GetNeuronAt(2, 0) != null)
                mv.GetNeuronAt(2, 0).Label = "Sensor";

            sensorCount = 0;
            actuatorCount = 0;

            if (!robotOnWiFi && sp?.IsOpen != true)
            {
                try
                {
                    if (sp == null)
                        sp = new SerialPort(serialPortName);
                    if (!sp.IsOpen)
                        sp.Open();
                    sp.BaudRate = 115200;
                    sp.Handshake = Handshake.None;

                    //reset the arduino processor
                    sp.DtrEnable = true;
                    sp.DtrEnable = false;
                }
                catch (Exception e)
                {
                    MessageBox.Show(MainWindow.thisWindow,"ModuleRobot port open failed because: " + e.Message);
                    return;
                }
            }

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
            if (mv == null) return; //this is called the first time before the module actually exists
        }
    }
}
