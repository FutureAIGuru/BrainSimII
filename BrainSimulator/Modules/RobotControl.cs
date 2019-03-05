using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;

namespace BrainSimulator
{
    public partial class NeuronArray
    {
        int pos = 750;
        public void RobotControl(NeuronArea na)
        {
            //string input = na.GetParam("-i");
            //NeuronArea naIn = FindAreaByLabel(input);
            //if (naIn == null) return;

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 4; j++) 
                {
                    Neuron n = na.GetNeuronAt(j,i);
                    if (n != null && n.LastCharge > .9)
                    {
                        MoveRobot(i, (j + 2) * 400, 1500);
                        break;
                    }
                }
            }

        }
        void MoveRobot(int port, int position, int speed)
        {
            SerialPort sp = new SerialPort("COM4");
            string commandString = "#"+port+" P" + position + " S750\r\n";
            sp.Open();
            sp.Write(commandString);
            sp.Close();

        }
    }
}