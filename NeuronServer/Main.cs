
using NeuronEngine.CLI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;

namespace NeuronServer
{
    //Multiple ports are used so a server can reside on same machine as client
    //PORTS: server->server 49001  every server listens and sends
    //       client->server 49002  client broadcasts and sends, server listens
    //       server->client 49003  server sends, client listens
    class Program
    {

        //for timing info
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        public static extern void GetSystemTimePreciseAsFileTime(out long filetime);

        static List<long> elapsedFiring;
        static List<long> elapsedTransfer;
        static List<long> neuronsFired;
        static List<long> boundarySynapses;

        static int firstNeuron = -1;
        static int lastNeuron = -1;

        
        const int maxDatagramSize = 1000;

        static IPAddress ipAddressClient = null;
        static IPAddress ipAddressThisMachine = null;

        static UdpClient serverServer; //send and receive
        static UdpClient serverClient; //send only
        static UdpClient clientServer; //receive only

        const int serverServerPort = 49001;
        const int clientServerPort = 49002;
        const int serverClientPort = 49003;

        static public NeuronArrayBase theNeuronArray = null;
        static void Main(string[] args)
        {

            serverServer = new UdpClient(serverServerPort);
            serverServer.EnableBroadcast = true;

            serverClient = new UdpClient();
            clientServer = new UdpClient(clientServerPort);
            clientServer.Client.ReceiveBufferSize = 100000000;

            Console.SetCursorPosition(0, 0);
            Console.WriteLine("Neuron Server Started");


            //set up lists for moving averages
            elapsedFiring = new List<long>();
            for (int i = 0; i < 100; i++)
            {
                elapsedFiring.Add(0);
            }
            elapsedTransfer = new List<long>();
            for (int i = 0; i < 100; i++)
            {
                elapsedTransfer.Add(0);
            }
            boundarySynapses = new List<long>();
            for (int i = 0; i < 100; i++)
            {
                boundarySynapses.Add(0);
            }
            neuronsFired = new List<long>();
            for (int i = 0; i < 100; i++)
            {
                neuronsFired.Add(0);
            }


            Task.Run(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
                ReceiveFromOtherServer();
            });
            Task.Run(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
                ReceiveFromClient();
            });

            Console.ReadKey();
        }

        static long pingCount = 0;
        public class ServerInfo { public IPAddress ip; public int firstNeuron; public int lastNeuron; };
        static List<ServerInfo> serverList;

        static void ProcessIncomingMessages(string message)
        {
            string[] commands = message.Trim().Split(' ');
            switch (commands[0])
            {
                case "Ping":
                    pingCount++;
                    SendToClient("PingBack " + pingCount);
                    break;

                case "Exit":
                    Environment.Exit(0);
                    break;

                case "GetServerInfo":
                    message = "ServerInfo " + ipAddressThisMachine.ToString() + " " + Environment.MachineName + " " + firstNeuron + " " + lastNeuron;
                    if (theNeuronArray != null)
                        message += " " + theNeuronArray.GetTotalNeuronsInUse() + " " + theNeuronArray.GetTotalSynapses();
                    Console.SetCursorPosition(0, 1);
                    Console.WriteLine(message + " PacketCount: " + pingCount);
                    pingCount = 0;
                    SendToClient(message);
                    break;

                case "InitServers":
                    serverList = new List<ServerInfo>();
                    int.TryParse(commands[1], out int synapsesPerNeuron);
                    int.TryParse(commands[2], out int arraySize);
                    for (int i = 3; i < commands.Length; i += 3)
                    {
                        ServerInfo si = new ServerInfo();
                        si.ip = IPAddress.Parse(commands[i]);
                        int.TryParse(commands[i + 1], out si.firstNeuron);
                        int.TryParse(commands[i + 2], out si.lastNeuron);
                        serverList.Add(si);
                        if (si.ip.Equals(ipAddressThisMachine))
                        {
                            firstNeuron = si.firstNeuron;
                            lastNeuron = si.lastNeuron;
                            theNeuronArray = new NeuronArrayBase();
                            theNeuronArray.Initialize(lastNeuron - firstNeuron);
                            if (synapsesPerNeuron != 0)
                            {
                                // Parallel.For(0, lastNeuron - firstNeuron, j => CreateRandomSynapses(j,synapsesPerNeuron));
                                for (int j = 0; j < lastNeuron - firstNeuron; j++) CreateRandomSynapses(j, synapsesPerNeuron, arraySize);
                            }
                            SendToClient("Done " + Environment.MachineName + " " + theNeuronArray.GetGeneration() + " " + theNeuronArray.GetFiredCount());
                            Console.SetCursorPosition(0, 2);

                            Console.WriteLine("Server initialized: " + firstNeuron + "-" + lastNeuron + " with " + synapsesPerNeuron + " synapses/neuron");
                        }
                    }
                    break;

                case "Fire":
                    Task.Run(() =>
                    {
                        GetSystemTimePreciseAsFileTime(out long start);
                        theNeuronArray.Fire();
                        GetSystemTimePreciseAsFileTime(out long end);
                        elapsedFiring.RemoveAt(0);
                        elapsedFiring.Add(end - start);
                        neuronsFired.RemoveAt(0);
                        neuronsFired.Add(theNeuronArray.GetFiredCount());
                        SendToClient("Done " + Environment.MachineName + " " + theNeuronArray.GetGeneration() + " " + theNeuronArray.GetFiredCount());
                    });
                    break;

                case "Transfer":
                    Task.Run(() =>
                    {
                        GetSystemTimePreciseAsFileTime(out long start);
                        byte[] xx = theNeuronArray.GetRemoteFiringSynapses();
                        List<Synapse> synapses = ConvertToSynapseList(xx);
                        List<Synapse>[] synapsesForServer = new List<Synapse>[serverList.Count];
                        for (int j = 0; j < synapsesForServer.Length; j++) synapsesForServer[j] = new List<Synapse>();
                        for (int i = 0; i < synapses.Count; i++)
                        {
                            int j;
                            for (j = 0; j < serverList.Count; j++)
                            {
                                if (-synapses[i].targetNeuron >= serverList[j].firstNeuron && -synapses[i].targetNeuron < serverList[j].lastNeuron)
                                    break;
                            }
                            synapses[i].targetNeuron = -synapses[i].targetNeuron;
                            synapsesForServer[j].Add(synapses[i]);
                        }
                        for (int j = 0; j < synapsesForServer.Length; j++)
                        {
                            int synapsesPerPacket = maxDatagramSize / 9;
                            int numPackets = synapsesForServer[j].Count / synapsesPerPacket;
                            int remainder = synapsesForServer[j].Count % synapsesPerPacket;
                            int k;
                            for (k = 0; k < numPackets; k++)
                            {
                                byte[] buffer = ConvertSynapseListToByteArray(synapsesForServer[j].GetRange(k * synapsesPerPacket, synapsesPerPacket));
                                SendToOtherServer2(serverList[j].ip, buffer);
                            }
                            if (remainder != 0)
                            {
                                byte[] buffer = ConvertSynapseListToByteArray(synapsesForServer[j].GetRange(k * synapsesPerPacket, remainder));
                                SendToOtherServer2(serverList[j].ip, buffer);
                            }
                        }
                        SendToClient("Done " + Environment.MachineName + " " + theNeuronArray.GetGeneration() + " " + theNeuronArray.GetFiredCount());
                        GetSystemTimePreciseAsFileTime(out long end);
                        elapsedTransfer.RemoveAt(0);
                        elapsedTransfer.Add(end - start);
                        boundarySynapses.RemoveAt(0);
                        boundarySynapses.Add(synapses.Count);
                        Console.SetCursorPosition(0, 4);
                        Console.Write("Gen: " + theNeuronArray.GetGeneration() + " Neurons fired: " + neuronsFired.Average().ToString("f0") + " Boundary Synapses: " + boundarySynapses.Average().ToString("f0")+ " Firing: " + (elapsedFiring.Average() / 10000f).ToString("f2") + "ms Transfer: " + (elapsedTransfer.Average() / 10000f).ToString("f2") + "ms                                                                 ");
                    });
                    break;

                case "GetNeuron":
                    int.TryParse(commands[1], out int neuronID);
                    if (neuronID >= firstNeuron && neuronID < lastNeuron)
                    { //TODO threshold and model
                        int localID = neuronID - firstNeuron;
                        string retVal = "Neuron ";
                        retVal += neuronID + " ";
                        retVal += theNeuronArray.GetNeuronModel(neuronID) + " ";
                        retVal += theNeuronArray.GetNeuronLastCharge(neuronID) + " ";
                        retVal += theNeuronArray.GetNeuronLeakRate(neuronID) + " ";
                        retVal += theNeuronArray.GetNeuronAxonDelay(neuronID) + " ";
                        retVal += theNeuronArray.GetNeuronInUse(localID);
                        SendToClient(retVal);
                    }
                    break;

                case "GetNeurons":
                    int.TryParse(commands[1], out neuronID);
                    int.TryParse(commands[2], out int count);
                    if (neuronID >= firstNeuron && neuronID + count <= lastNeuron)
                    { //TODO threshold and model
                        string retVal = "Neurons " + count + " ";
                        for (int i = 0; i < count; i++)
                        {
                            if (retVal.Length > maxDatagramSize)
                            {
                                retVal += "...";
                                SendToClient(retVal);
                                retVal = " ...";
                            }
                            retVal += neuronID + " ";
                            int localID = neuronID - firstNeuron;
                            retVal += theNeuronArray.GetNeuronModel(localID) + " ";
                            retVal += theNeuronArray.GetNeuronLastCharge(localID) + " ";
                            retVal += theNeuronArray.GetNeuronLeakRate(localID) + " ";
                            retVal += theNeuronArray.GetNeuronAxonDelay(localID) + " ";
                            retVal += theNeuronArray.GetNeuronInUse(localID) + " ";
                            neuronID++;
                        }
                        SendToClient(retVal);
                    }
                    break;

                case "SetNeuron":
                    int.TryParse(commands[1], out neuronID);
                    if (neuronID >= firstNeuron && neuronID < lastNeuron)
                    {
                        int localID = neuronID - firstNeuron;
                        int.TryParse(commands[2], out int neuronModel);
                        float.TryParse(commands[3], out float currentCharge);
                        float.TryParse(commands[4], out float lastCharge);
                        float.TryParse(commands[5], out float leakRate);
                        int.TryParse(commands[6], out int axonDelay);
                        theNeuronArray.SetNeuronModel(localID, neuronModel);
                        theNeuronArray.SetNeuronCurrentCharge(localID, currentCharge);
                        theNeuronArray.SetNeuronLastCharge(localID, lastCharge);
                        theNeuronArray.SetNeuronLeakRate(localID, leakRate);
                        theNeuronArray.SetNeuronAxonDelay(localID, axonDelay);
                    }
                    break;

                case "AddSynapse":
                    int.TryParse(commands[1], out int src);
                    int.TryParse(commands[2], out int dest);
                    float.TryParse(commands[3], out float weight);
                    int.TryParse(commands[4], out int model);
                    AddSynapse(src, dest, weight, model);
                    break;

                case "DeleteSynapse":
                    int.TryParse(commands[1], out src);
                    int.TryParse(commands[2], out dest);
                    //set src and dest neuron #'s, zero-based for server
                    if (src < firstNeuron || src >= lastNeuron)
                        src = -src;
                    else
                        src = src - firstNeuron;
                    if (dest < firstNeuron || dest >= lastNeuron)
                        dest = -dest;
                    else
                        dest = dest - firstNeuron;
                    if (src >= 0)
                    {
                        theNeuronArray.DeleteSynapse(src, dest);
                    }
                    if (dest >= 0)
                    {
                        theNeuronArray.DeleteSynapseFrom(src, dest);
                    }
                    break;

                case "GetSynapses":
                    int.TryParse(commands[1], out neuronID);
                    if (neuronID >= firstNeuron && neuronID < lastNeuron)
                    {
                        int localID = neuronID - firstNeuron;
                        List<Synapse> synapses = ConvertToSynapseList(theNeuronArray.GetSynapses(localID));
                        string retVal = "Synapses " + neuronID + " ";
                        foreach (Synapse s in synapses)
                        {
                            if (s.targetNeuron < 0) s.targetNeuron = -s.targetNeuron;
                            else s.targetNeuron = s.targetNeuron + firstNeuron;
                            retVal += s.targetNeuron + " " + s.weight + " " + s.model + " ";
                        }
                        SendToClient(retVal);
                    }
                    break;

                case "GetSynapsesFrom":
                    int.TryParse(commands[1], out neuronID);
                    if (neuronID >= firstNeuron && neuronID < lastNeuron)
                    {
                        int localID = neuronID - firstNeuron;
                        List<Synapse> synapses = ConvertToSynapseList(theNeuronArray.GetSynapsesFrom(localID));
                        string retVal = "SynapsesFrom " + neuronID + " ";
                        foreach (Synapse s in synapses)
                        {
                            if (s.targetNeuron < 0) s.targetNeuron = -s.targetNeuron;
                            else s.targetNeuron = s.targetNeuron + firstNeuron;
                            retVal += s.targetNeuron + " " + s.weight + " " + s.model + " ";
                        }
                        SendToClient(retVal);
                    }
                    break;
            }
        }

        private static void AddSynapse(int src, int dest, float weight, int model)
        {
            //set src and dest neuron #'s, zero-based for server
            if (src < firstNeuron || src >= lastNeuron)
                src = -src;
            else
                src = src - firstNeuron;
            if (dest < firstNeuron || dest >= lastNeuron)
                dest = -dest;
            else
                dest = dest - firstNeuron;
            if (src >= 0)
            {
                theNeuronArray.AddSynapse(src, dest, weight, model, true);
            }
            if (dest >= 0)
            {
                theNeuronArray.AddSynapseFrom(src, dest, weight, model);
            }
        }

        public static void ReceiveFromOtherServer()
        {
            var from = new IPEndPoint(IPAddress.Any, serverServerPort);
            while (true)
            {
                var recvBuffer = serverServer.Receive(ref from);
                List<Synapse> incomingSynapses = ConvertByteArrayToSynapseList(recvBuffer);
                for (int i = 0; i < incomingSynapses.Count; i++)
                {
                    theNeuronArray.AddToNeuronCurrentCharge(incomingSynapses[i].targetNeuron - firstNeuron, incomingSynapses[i].weight);
                }
            }
        }
        public static void ReceiveFromClient()
        {
            while (true)
            {
                var from = new IPEndPoint(IPAddress.Any, clientServerPort);
                var recvBuffer = clientServer.Receive(ref from);

                if (ipAddressThisMachine == null)
                {
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    for (int i = 0; i < host.AddressList.Length; i++)
                    {
                        if (host.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                        {
                            ipAddressClient = from.Address;
                            byte[] ip1 = from.Address.GetAddressBytes(); ;
                            byte[] ip2 = host.AddressList[i].GetAddressBytes();
                            if (ip1[0] == ip2[0] && ip1[1] == ip2[1] && ip1[2] == ip2[2])
                                ipAddressThisMachine = host.AddressList[i];
                        }
                    }
                    Console.SetCursorPosition(0, 3);
                    Console.WriteLine("Server IP Address: " + ipAddressThisMachine + " Name: " + Environment.MachineName + " Client IP Address: " + ipAddressClient);
                }
                string incomingMessage = Encoding.UTF8.GetString(recvBuffer);
                //Console.WriteLine("Received from client: " + incomingMessage);
                ProcessIncomingMessages(incomingMessage);
            }
        }
        public static void SendToOtherServer2(IPAddress ip, byte[] datagram)
        {
            //experiments in data compression
            //System.IO.MemoryStream stream = new System.IO.MemoryStream(datagram);
            //using (var outStream = new System.IO.MemoryStream())
            //{
            //    using (var compressionStream =
            //        new System.IO.Compression.GZipStream(outStream, System.IO.Compression.CompressionMode.Compress))
            //    {
            //        stream.CopyTo(compressionStream);
            //    }
            //    byte[] compressed = outStream.ToArray();
            //}
            //using (var outStream = new System.IO.MemoryStream())
            //{
            //    using (var compressionStream =
            //        new System.IO.Compression.DeflateStream(outStream, System.IO.Compression.CompressionLevel.Optimal))
            //    {
            //        stream.Position = 0;
            //        stream.CopyTo(compressionStream);
            //    }
            //    byte[] compressed = outStream.ToArray();
            //}

            //Console.WriteLine("Send to servers: " + message.Length + " bytes");
            IPEndPoint ipEnd = new IPEndPoint(ip, serverServerPort);
            serverServer.SendAsync(datagram, datagram.Length, ipEnd);
        }
        public static void SendToClient(string message)
        {
            //Console.WriteLine("Send to client: " + message);
            byte[] datagram = Encoding.UTF8.GetBytes(message);
            IPEndPoint ipEnd = new IPEndPoint(ipAddressClient, serverClientPort);
            serverClient.SendAsync(datagram, datagram.Length, ipEnd);
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Synapse
        {
            public int targetNeuron;
            public float weight;
            public int model;
        }

        static byte[] ConvertSynapseListToByteArray(List<Synapse> synapses)
        {
            var stream = new System.IO.MemoryStream();
            var writer = new System.IO.BinaryWriter(stream);
            for (int i = 0; i < synapses.Count; i++)
            {
                writer.Write(synapses[i].targetNeuron);
                writer.Write(synapses[i].weight);
                writer.Write(synapses[i].model);
            }
            return stream.ToArray();
        }
        static List<Synapse> ConvertByteArrayToSynapseList(byte[] bytes)
        {
            List<Synapse> retVal = new List<Synapse>();
            var reader = new System.IO.BinaryReader(new System.IO.MemoryStream(bytes));
            for (int i = 0; i < bytes.Length / 9; i++)
            {
                Synapse s = new Synapse();
                s.targetNeuron = reader.ReadInt32();
                s.weight = reader.ReadSingle();
                s.model = reader.ReadInt32();
                retVal.Add(s);
            }
            return retVal;
        }

        static List<Synapse> ConvertToSynapseList(byte[] input)
        {
            List<Synapse> retVal = new List<Synapse>();
            if (input.Length == 0) return retVal;
            Synapse s;
            int sizeOfSynapse = Marshal.SizeOf(typeof(Synapse));
            int numberOfSynapses = input.Length / sizeOfSynapse;
            byte[] oneSynapse = new byte[sizeOfSynapse];
            for (int i = 0; i < numberOfSynapses; i++)
            {
                int offset = i * sizeOfSynapse;
                for (int k = 0; k < sizeOfSynapse; k++)
                    oneSynapse[k] = input[k + offset];
                GCHandle handle = GCHandle.Alloc(oneSynapse, GCHandleType.Pinned);
                s = (Synapse)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Synapse));
                retVal.Add(s);
                handle.Free();
            }
            return retVal;
        }

        static Random rand;
        static private void CreateRandomSynapses(int i, int synapsesPerNeuron, int arraySize)
        {
            int rows = 1000;
            if (rand == null) rand = new Random();
            for (int j = 0; j < synapsesPerNeuron; j++)
            {
                //int targetNeuron = i + rand.Next() % (2 * synapsesPerNeuron) - synapsesPerNeuron;
                int rowOffset = rand.Next() % 100 - 50;
                int colOffset = rand.Next() % 100 - 50;
                int targetNeuron = i + firstNeuron + (colOffset * rows) + rowOffset;

                while (targetNeuron < 0) targetNeuron += arraySize;
                while (targetNeuron >= arraySize) targetNeuron -= arraySize;

                float weight = (rand.Next(521) / 1000f) - .2605f;
                AddSynapse(i + firstNeuron, targetNeuron, weight, 0);
            }

            //if (rand == null) rand = new Random();
            //for (int j = 0; j < synapsesPerNeuron; j++)
            //{
            //    int rowOffset = rand.Next() % 10 - 5;
            //    int colOffset = rand.Next() % 10 - 5;
            //    int targetNeuron = i+firstNeuron + (colOffset * rows) + rowOffset;

            //    while (targetNeuron < 0) targetNeuron += arraySize;
            //    while (targetNeuron >= arraySize) targetNeuron -= arraySize;
            //    float weight = (rand.Next(1000) / 750f) - .5f;
            //    AddSynapse(i+firstNeuron, targetNeuron, weight, false);
            //}
        }

    }
}
