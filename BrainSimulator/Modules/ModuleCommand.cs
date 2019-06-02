using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BrainSimulator
{
    public class ModuleCommand : ModuleBase
    {
        string textFile = "";
        string[] commands;
        int line = 0;
        string currentModule = "";
        public override void Fire()
        {
            Init();  //be sure to leave this here to enable use of the na variable
            if (commands == null) return;
            if (line >= commands.Length) return;
            string[] commandLine = commands[line].Split(' ');
            if (commandLine.Length > 0 && commandLine[0].IndexOf("//") != 0)
                for (int i = 0; i < commandLine.Length; i++)
                {
                    string currentCommand = commandLine[i];
                    if (currentCommand == "stop")
                    {
                        line = commands.Length;
                        return;
                    }
                    int colonLoc = currentCommand.IndexOf(':');
                    if (colonLoc != -1)
                    {
                        currentModule = currentCommand.Substring(0, colonLoc);
                        continue;
                    }
                    NeuronArea na = theNeuronArray.FindAreaByLabel(currentModule);
                    if (na != null && currentCommand != "")
                    {
                        Neuron n = na.GetNeuronAt(currentCommand);
                        if (n != null)
                        {
                            n.CurrentCharge = 1;
                        }
                    }
                }
            line++;

        }
        public override void Initialize()
        {
            textFile = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            textFile += @"\BrainSim\commands.txt";
            if (File.Exists(textFile))
            {
                commands = File.ReadAllLines(textFile);
            }
            line = 0;
        }
    }


}
