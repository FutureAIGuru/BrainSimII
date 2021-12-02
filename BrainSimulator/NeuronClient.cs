using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BrainSimulator
{

    class NeuronClient
    {

        static UdpClient serverClient = null; //listen only
        static UdpClient clientServer; //send/broadcast only
        static IPAddress broadCastAddress; 

        const int clientServerPort = 49002;
        const int serverClientPort = 49003;
        public static void Init()
        {
            if (serverClient != null) return; //already initialized

            //what is my ipaddress
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    byte[] ips = ip.GetAddressBytes();
                    broadCastAddress = IPAddress.Parse(ips[0] +"."+ ips[1] + "."+ips[2]+".255");
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

        public class Server
        {
            public string name;
            public IPAddress ipAddress;
            public int firstNeuron;
            public int lastNeuron;
            public bool busy = false;
            public long generation;
            public int firedCount;
            public long totalSynapses;
            public int neuronsInUse;
        }
        public static List<Server> serverList;
        public static void GetServerList()
        {
            serverList = new List<Server>();
            Broadcast("GetServerInfo");
        }

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void GetSystemTimePreciseAsFileTime(out long filetime);
        static long returnTime;
        public static long pingCount = 0;
        public static long Ping(IPAddress targetIp,string payload)
        {
            //run the test
            returnTime = 0;
            GetSystemTimePreciseAsFileTime(out long startTime);
            SendToServer(targetIp,"Ping " + payload);
            while (returnTime == 0) Thread.Sleep(1);
            long elapsed = returnTime - startTime;
            return elapsed;
        }

        public static string CreatePayload(int payloadSize)
        {
            //create the payload
            string payload1 = "0123456789";
            string payload = "";
            for (int i = 0; i < (payloadSize + 1) / 10; i++) //fill the dummy, then truncate to desired length
                payload += payload1;
            payload = payload.Substring(0, payloadSize);
            return payload;
        }

        public static void InitServers(int synapsesPerNeuron,int arraySize)
        {
            string message = "InitServers "+synapsesPerNeuron + " " + arraySize + " ";
            for (int i = 0; i < serverList.Count; i++)
            {
                message += serverList[i].ipAddress + " " + serverList[i].firstNeuron + " " + serverList[i].lastNeuron + " ";
            }
            Broadcast(message);
            WaitForDoneOnAllServers();
        }
        public static void MarkServersBusy()
        {
            for (int i = 0; i < serverList.Count; i++)
                serverList[i].busy = true;
        }
        public static void Fire()
        {
            MarkServersBusy();
            Broadcast("Fire");
            WaitForDoneOnAllServers();
            MarkServersBusy();
            Broadcast("Transfer");
            WaitForDoneOnAllServers();
        }
        static Neuron tempNeuron = null;
        public static Neuron GetNeuron(int id)
        {
            tempNeuron = null;
            Broadcast("GetNeuron " + id);
            while (tempNeuron == null) 
                Thread.Sleep(1);
            return tempNeuron;

        }
        static List<Neuron> tempNeurons = null;
        public static List<Neuron> GetNeurons(int id, int count)
        {
            tempNeurons = new List<Neuron>();
            int start = id;
            int remaining = count;

            int i;
            for (i = 0; i < serverList.Count; i++)
                if (start >= serverList[i].firstNeuron && start < serverList[i].lastNeuron) break;
            int accumNeurons = 0;
            while (i < serverList.Count && start + count > serverList[i].lastNeuron) //handle split across multiple servers
            {
                int firstCount = serverList[i].lastNeuron - start;
                Broadcast("GetNeurons " + start + " " + firstCount);
                remaining -= firstCount;
                accumNeurons += firstCount;
                start = serverList[i].lastNeuron;
                while (tempNeurons.Count < accumNeurons) Thread.Sleep(1);
                i++;
            }

            Broadcast("GetNeurons " + start + " " + remaining);
            while (tempNeurons.Count < count) 
                Thread.Sleep(1);
            tempNeurons.Sort((t1, t2) => t1.id.CompareTo(t2.id)); //the neurons may be returned in different order
            return tempNeurons;
        }
        public static void SetNeuron(Neuron n)
        {
            string command = "SetNeuron ";
            command += n.id + " ";
            command += (int)n.model + " ";
            command += n.currentCharge + " ";
            command += n.lastCharge + " ";
            command += n.leakRate + " ";
            command += n.axonDelay + " ";
            Broadcast(command);
        }

        static private int GetServerIndex(int neuronID)
        {
            for (int i = 0; i < serverList.Count; i++)
                if (neuronID >= serverList[i].firstNeuron && neuronID < serverList[i].lastNeuron) return i;
            return -1;
        }
        public static void AddSynapse(int src, int dest, float weight, Synapse.modelType model, bool noBackPtr)
        {
            string command = "AddSynapse ";
            command += src + " ";
            command += dest + " ";
            command += weight + " ";
            command += (int)model + " ";
            int srcServer = GetServerIndex(src);
            SendToServer(serverList[srcServer].ipAddress, command);
            int destServer = GetServerIndex(dest);
            if (srcServer != destServer)
                SendToServer(serverList[destServer].ipAddress, command);
        }
        public static void DeleteSynapse(int src, int dest)
        {
            string command = "DeleteSynapse ";
            command += src + " ";
            command += dest + " ";
            Broadcast(command);
        }

        static List<Synapse> tempSynapses = null;
        public static List<Synapse> GetSynapses(int id)
        {
            tempSynapses = null;
            string command = "GetSynapses " + id;
            Broadcast(command);
            while (tempSynapses == null) Thread.Sleep(1);
            return tempSynapses;
        }
        public static List<Synapse> GetSynapsesFrom(int id)
        {
            tempSynapses = null;
            string command = "GetSynapsesFrom " + id;
            Broadcast(command);
            while (tempSynapses == null) Thread.Sleep(1);
            return tempSynapses;
        }

        public static void WaitForDoneOnAllServers()
        {
            while (serverList.FindIndex(x => x.busy == true) != -1) Thread.Sleep(1);
        }

        static void ProcessIncomingMessages(string message)
        {
            string[] commands = message.Trim().Split(' ');
            string command = commands[0];
            switch (command)
            {
                case "PingBack":
                    GetSystemTimePreciseAsFileTime(out returnTime);
                    pingCount++;
                    break;

                case "ServerInfo":
                    int index = serverList.FindIndex(x => x.name == commands[2]);
                    if (index == -1)
                    {
                        Server s = new Server();
                        IPAddress.TryParse(commands[1], out s.ipAddress);
                        s.name = commands[2];
                        int.TryParse(commands[3], out s.firstNeuron);
                        int.TryParse(commands[4], out s.lastNeuron);
                        if (commands.Length > 5) int.TryParse(commands[5], out s.neuronsInUse);
                        if (commands.Length > 6) long.TryParse(commands[6], out s.totalSynapses);
                        serverList.Add(s);
                        index = serverList.Count - 1;
                    }
                    break;

                case "Done":
                    index = serverList.FindIndex(x => x.name == commands[1]);
                    if (index != -1)
                    {
                        serverList[index].busy = false;
                        long.TryParse(commands[2], out serverList[index].generation);
                        int.TryParse(commands[3], out serverList[index].firedCount);
                    }
                    break;

                case "Neuron":
                    Neuron n = new Neuron();
                    int.TryParse(commands[1], out n.id);
                    int.TryParse(commands[2], out int tempModel);
                    n.model = (Neuron.modelType)tempModel;
                    float.TryParse(commands[3], out n.lastCharge);
                    float.TryParse(commands[4], out n.leakRate);
                    int.TryParse(commands[5], out n.axonDelay);
                    bool.TryParse(commands[6], out n.inUse);
                    tempNeuron = n;
                    break;

                case "Neurons":
                    int.TryParse(commands[1], out int count);
                    for (int i = 2; i < commands.Length; i += 6)
                    {
                        n = new Neuron();
                        int.TryParse(commands[i], out n.id);
                        int.TryParse(commands[i+1], out tempModel);
                        n.model = (Neuron.modelType)tempModel;
                        float.TryParse(commands[i+2], out n.lastCharge);
                        float.TryParse(commands[i+3], out n.leakRate);
                        int.TryParse(commands[i+4], out n.axonDelay);
                        bool.TryParse(commands[i+5], out n.inUse);
                        tempNeurons.Add(n);
                    }
                    break;

                case "Synapses":
                    int.TryParse(commands[1], out int neuronID);
                    List<Synapse> synapses = new List<Synapse>();
                    for (int i = 2; i < commands.Length; i += 3)
                    {
                        Synapse s = new Synapse();
                        int.TryParse(commands[i], out s.targetNeuron);
                        float.TryParse(commands[i + 1], out s.weight);
                        int.TryParse(commands[i + 2], out int modelInt);
                        s.model = (Synapse.modelType)modelInt;
                        synapses.Add(s);
                    }
                    tempSynapses = synapses;
                    break;

                case "SynapsesFrom":
                    int.TryParse(commands[1], out neuronID);
                    synapses = new List<Synapse>();
                    for (int i = 2; i < commands.Length; i += 3)
                    {
                        Synapse s = new Synapse();
                        int.TryParse(commands[i], out s.targetNeuron);
                        float.TryParse(commands[i + 1], out s.weight);
                        int.TryParse(commands[i + 2], out int modelInt);
                        s.model = (Synapse.modelType)modelInt;
                        synapses.Add(s);
                    }
                    tempSynapses = synapses;
                    break;

            }
        }
        //TODO: neuron labels cannot contain '...' or '_'
        public static void ReceiveFromServer()
        {
            while (true)
            {
                string incomingMessage = "";
                var from = new IPEndPoint(IPAddress.Any, serverClientPort);
                var recvBuffer = serverClient.Receive(ref from);
                incomingMessage += Encoding.UTF8.GetString(recvBuffer);
                while (incomingMessage.EndsWith("..."))
                {
                    recvBuffer = serverClient.Receive(ref from);
                    string nextPart = Encoding.UTF8.GetString(recvBuffer);
                    if (nextPart.IndexOf("...") == -1)
                        ProcessIncomingMessages(nextPart);
                    else
                    {
                        int posOfSpace = nextPart.IndexOf(' ');
                        nextPart = nextPart.Substring(posOfSpace + 1);
                        incomingMessage += nextPart;
                    }
                }
                //Debug.WriteLine("Receive from server: "+incomingMessage);
                incomingMessage = incomingMessage.Replace("...", "");
                ProcessIncomingMessages(incomingMessage);
            }
        }
        public static void Broadcast(string message)
        {
            //Debug.WriteLine("Broadcast: " + message);
            byte[] datagram = Encoding.UTF8.GetBytes(message);
            IPEndPoint ipEnd = new IPEndPoint(broadCastAddress, clientServerPort);
            clientServer.SendAsync(datagram, datagram.Length, ipEnd);
        }
        public static void SendToServer(IPAddress serverIp, string message)
        {
            //Debug.WriteLine("Send to server: " + ip + ": " + message);
            byte[] datagram = Encoding.UTF8.GetBytes(message);
            IPEndPoint ipEnd = new IPEndPoint(serverIp, clientServerPort);
            clientServer.Send(datagram, datagram.Length, ipEnd);
        }
    }
}
