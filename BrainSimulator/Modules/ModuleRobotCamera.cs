//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Serialization;
using System.Diagnostics;

namespace BrainSimulator.Modules
{
    public class ModuleRobotCamera : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleRobotCamera()
        {
            minHeight = 2;
            maxHeight = 500;
            minWidth = 2;
            maxWidth = 500;
        }


        //needed to get the IP address of the ESP32 Camera device
        UdpClient serverClient = null; //listen only
        UdpClient clientServer; //send/broadcast only
        IPAddress broadCastAddress;
        int clientServerPort = 3333;
        int serverClientPort = 3333;
        IPAddress theIP = null;

        HttpClient theHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2), };
        bool httpClientBusy = false;

        [XmlIgnore]
        public BitmapImage theBitmap = null;

        public override void Fire()
        {
            if (theIP == null)
            {
                Broadcast("DevicePoll");
                theIP = new IPAddress(new byte[] { 10, 0, 0, 214 });
            }

            try
            {
                Init();  //be sure to leave this here

                GetCameraImage(); //only issues request if not currently busy


                //if you want the dlg to update, use the following code whenever any parameter changes
                UpdateDialog();

            }
            catch (Exception e)
            {
                MessageBox.Show("RobotCameraModule encountered an exception: " + e.Message);
            }
        }


        private void LoadImage(Bitmap bitmap1)
        {

            float vRatio = bitmap1.Height / (float)mv.Height;
            float hRatio = bitmap1.Width / (float)mv.Width;
            for (int i = 0; i < mv.Height; i++)
                for (int j = 0; j < mv.Width; j++)
                {
                    Neuron n = mv.GetNeuronAt(j, i);
                    int x = (int)(j * (bitmap1.Width - 1) / (float)(mv.Width - 1));
                    int y = (int)(i * (bitmap1.Height - 1) / (float)(mv.Height - 1));

                    System.Drawing.Color c = bitmap1.GetPixel(x, y);
                    int val = Utils.ColorToInt(c);
                    n.LastChargeInt = val;
                }
        }


        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
            foreach (Neuron n in mv.Neurons)
                n.Model = Neuron.modelType.Color;

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


        }

        async void GetCameraImage()
        {
            if (theIP == null)
                return;

            if (httpClientBusy)
                return;
            try
            {
                httpClientBusy = true;
                var response = await theHttpClient.GetAsync("http://" + theIP);
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var theStream = await response.Content.ReadAsByteArrayAsync();
                        using (var mem = new MemoryStream(theStream))
                        using (Bitmap bitmap1 = (Bitmap)System.Drawing.Image.FromStream(mem))
                        {
                            bitmap1.RotateFlip(RotateFlipType.Rotate270FlipNone);
                            LoadImage(bitmap1);

                            //for the dialog box display
                            mem.Position = 0;
                            theBitmap = new BitmapImage();
                            theBitmap.BeginInit();
                            theBitmap.Rotation = Rotation.Rotate270;
                            theBitmap.StreamSource = mem;
                            theBitmap.CacheOption = BitmapCacheOption.OnLoad;
                            theBitmap.EndInit();
                            theBitmap.Freeze();
                        }
                        debugMsgCount = 0;
                    }
                    catch
                    { }
                }
                else
                { }
            }
            catch (Exception e)
            {
                if (debugMsgCount++ < 5)
                    Debug.WriteLine("ModuleRobotCamera:GetCameraImage encountered exception: " + e.Message);
                mv.GetNeuronAt(0, 0).SetValueInt(0xff0000);
                theHttpClient.CancelPendingRequests();
            }
            httpClientBusy = false;
        }
        int debugMsgCount = 0;
        public void ReceiveFromServer()
        {
            while (true)
            {
                string incomingMessage = "";
                var from = new IPEndPoint(IPAddress.Any, serverClientPort);
                var recvBuffer = serverClient.Receive(ref from);
                incomingMessage += Encoding.UTF8.GetString(recvBuffer);
                if (incomingMessage == "Camera")
                {
                    theIP = from.Address;
                }
                Debug.WriteLine("Received from Device: " + from.Address + " " + incomingMessage);
            }
        }
        public void Broadcast(string message)
        {
            //Debug.WriteLine("Broadcast: " + message);
            byte[] datagram = Encoding.UTF8.GetBytes(message);
            IPEndPoint ipEnd = new IPEndPoint(broadCastAddress, clientServerPort);
            clientServer.SendAsync(datagram, datagram.Length, ipEnd);
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
            foreach (Neuron n in mv.Neurons)
                n.Model = Neuron.modelType.Color;
        }
    }
}
