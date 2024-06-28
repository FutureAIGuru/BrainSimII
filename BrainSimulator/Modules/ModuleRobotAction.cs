//
// Copyright (c) Charles Simon. All rights reserved.
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

namespace BrainSimulator.Modules
{
    public class ModuleRobotAction : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleRobotAction()
        {
            minHeight = 4;
            maxHeight = 500;
            minWidth = 3;
            maxWidth = 500;
        }

        bool recording = false;

        public class Action
        {
            //future.. timing
            public string name = "";
            public List<string> motions = new List<string>();
            public int playPosition = 0;
        }
        public List<Action> actions = new List<Action>();

        Action currentAction = null;

        [XmlIgnore]
        public string ActionsString
        {
            get
            {
                string retVal = "";
                foreach (Action a in actions)
                {
                    retVal += "ACTION: " + a.name + "\n";
                    for (int i = 0; i < a.motions.Count; i++)
                    {
                        retVal += "MOTION: " + a.motions[i]  + "\n";
                    }
                    retVal += "\n";
                }
                return retVal;
            }
            set
            {
                actions.Clear();
                for (int i = 1; i < mv.NeuronCount; i++) mv.GetNeuronAt(i).Label = "";
                string[] theLines = value.Split(new char[] { '\n' });
                Action theNewAction = new Action();
                foreach (string line in theLines)
                {
                    string[] fields = line.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (fields.Length > 1)
                    {
                        switch (fields[0].Trim())
                        {
                            case "ACTION":
                                theNewAction.name = fields[1].Trim();
                                break;
                            case "MOTION":
                                theNewAction.motions.Add(fields[1].Trim());
                                break;
                        }
                    }
                    else
                    {
                        if (theNewAction.name != "")
                        {
                            actions.Add(theNewAction);
                            mv.GetNeuronAt(actions.Count).Label = theNewAction.name;
                        }
                        theNewAction = new Action();
                    }
                }
                MainWindow.Update();
            }
        }


        public override void Fire()
        {
            Init();  //be sure to leave this here
            ModuleView moduleRobotPose = MainWindow.theNeuronArray.FindModuleByLabel("RobotPose");
            if (moduleRobotPose == null) return;
            ModuleView moduleRobot = MainWindow.theNeuronArray.FindModuleByLabel("Robot");
            if (moduleRobot == null) return;

            Neuron nRecStop = mv.GetNeuronAt(0, 0);
            if (nRecStop.Fired())
            {
                if (nRecStop.Label == "Record")
                {
                    //save the recording and link to neuron
                    nRecStop.Label = "Stop";
                    recording = true;
                    currentAction = new Action();
                }
                else
                {
                    //initialize a recording
                    nRecStop.Label = "Record";
                    recording = false;
                    actions.Add(currentAction);
                    currentAction = null;
                    mv.GetNeuronAt(actions.Count).Label = "A" + actions.Count;
                    MainWindow.Update();
                }
            }

            if (recording)
            {
                for (int i = 1; i < moduleRobotPose.NeuronCount; i++)
                {
                    Neuron n = moduleRobotPose.GetNeuronAt(i);
                    if (n == null || n.Label == "") break;
                    if (n.Fired())
                    {
                        currentAction.motions.Add(n.Label);
                    }
                }
            }
            else
            { //playback
                if (currentAction == null)
                {
                    for (int i = 1; i < mv.NeuronCount && i < actions.Count+1; i++)
                    {
                        Neuron n = mv.GetNeuronAt(i);
                        if (n == null || n.Label == "") break;
                        actions[i - 1].name = n.label;
                        if (n.Fired())
                        {
                            currentAction = actions[i - 1];
                            currentAction.playPosition = 0;
                        }
                    }
                }
                else //already playing an action
                {
                    if (moduleRobot.GetNeuronAt("Busy")?.LastCharge != 0)
                        return;
                    string currentActionStep = currentAction.motions[currentAction.playPosition++];
                    if (currentAction.playPosition >= currentAction.motions.Count)
                        currentAction = null;
                    Neuron n = moduleRobotPose.GetNeuronAt(currentActionStep);
                    if (n == null)
                    {
                        MessageBox.Show("Missing Pose/Motion: " + currentActionStep);
                        currentAction = null;
                    }
                    else
                    {
                        n.SetValue(1);
                        Debug.WriteLine("Taking Action: " + currentActionStep);
                    }
                }
            }
        }

        public override void Initialize()
        {
            Init();
            mv.GetNeuronAt(0, 0).Label = "Record";
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

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }
    }
}
