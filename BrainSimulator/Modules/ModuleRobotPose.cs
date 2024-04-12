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

namespace BrainSimulator.Modules
{
    public class ModuleRobotPose : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleRobotPose()
        {
            minHeight = 4;
            maxHeight = 500;
            minWidth = 3;
            maxWidth = 500;
        }


        public class Pose
        {
            public string name = "";
            public List<string> actuators = new List<string>();
            public List<float> positions = new List<float>();
            public float timing = 0; //in ms
            public bool isRelative = false;
        }

        public List<Pose> poses = new List<Pose>();

        [XmlIgnore]
        public string PosesString
        {
            get
            {
                string retVal = "";
                foreach (Pose p in poses)
                {
                    if (!p.isRelative) 
                        retVal += "NAME: " + p.name + "\n";
                    else
                        retVal += "NAME: " + p.name + ":RELATIVE \n";
                    retVal += "TIMING: " + p.timing + "\n";
                    for (int i = 0; i < p.actuators.Count; i++)
                    {
                        retVal += "ACTUATOR: " + p.actuators[i] + " :POSITION: " + p.positions[i] + "\n";
                    }
                    retVal += "\n";
                }
                return retVal;
            }
            set
            {
                poses.Clear();
                for (int i = 2; i < mv.NeuronCount; i++) mv.GetNeuronAt(i).Label = "";
                string[] theLines = value.Split(new char[] { '\n' });
                Pose theNewPose = new Pose();
                foreach (string line in theLines)
                {
                    string[] fields = line.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (fields.Length > 1)
                    {
                        switch (fields[0].Trim())
                        {
                            case "NAME": 
                                theNewPose.name = fields[1].Trim();
                                if (fields.Length > 2 && fields[2].Trim() == "RELATIVE")
                                    theNewPose.isRelative = true;
                                break;
                            case "TIMING":
                                if (fields.Length > 0 && float.TryParse(fields[1].Trim(), out float result))
                                    theNewPose.timing = result;
                                break;
                            case "ACTUATOR":
                                if (fields.Length > 2 && float.TryParse(fields[3].Trim(), out result))
                                {
                                    theNewPose.actuators.Add(fields[1].Trim());
                                    theNewPose.positions.Add(result);
                                }
                                break;
                        }
                    }
                    else
                    {
                        if (theNewPose.name != "")
                        {
                            poses.Add(theNewPose);
                            mv.GetNeuronAt(poses.Count+1).Label = theNewPose.name;
                        }
                        theNewPose = new Pose();
                    }
                }
            }
        }


        //fill this method in with code which will execute
        //once for each cycle of the engine

        float[] previousPositionValues = null;
        public override void Fire()
        {
            Init();  //be sure to leave this here
            ModuleView mv = MainWindow.theNeuronArray.FindModuleByLabel("Robot");
            if (mv == null) return;

            //a little bit of initializeaion
            if (previousPositionValues == null)
            {
                previousPositionValues = new float[mv.Height];
                for (int i = 0; i < mv.Height; i++)
                    previousPositionValues[i] = mv.GetNeuronAt(0, i).LastCharge;
            }

            //see if a pose request has been fired
            for (int i = 2; i < base.mv.NeuronCount; i++)
            {
                Neuron n = base.mv.GetNeuronAt(i);
                if (n == null || n.Label == "") break;
                //update internal labels in case they are changed
                poses[i - 2].name = base.mv.GetNeuronAt(i).Label;
                if (n.Fired())
                {
                    Pose thePose = poses[i - 2];
                    mv.GetNeuronAt("Timing").SetValue(thePose.timing);
                    for (int j = 0; j < thePose.actuators.Count; j++)
                    {
                        Neuron n1 = mv.GetNeuronAt(thePose.actuators[j]);
                        if (!thePose.isRelative)
                            n1.SetValue(thePose.positions[j]);
                        else
                        {
                            n1.SetValue(thePose.positions[j] + n1.LastCharge);
                        }
                    }
                }
            }

            //save a new pose
            if (GetNeuron("Save").Fired() || GetNeuron("Move").Fired())
            {
                Neuron n = null;
                foreach (Neuron n1 in base.mv.Neurons)
                {
                    if (n1.Label == "")
                    {
                        n = n1;
                        break;
                    }
                }

                if (n == null) return; //TODO: add something here to expand the array?

                Pose theNewPose = new Pose()
                {
                    timing = mv.GetNeuronAt(0, 0).LastCharge,
                    isRelative = GetNeuron("Move").Fired(),
                };

                n.Label = theNewPose.isRelative ? "M" + poses.Count : "P" + poses.Count;

                for (int i = 1; i < previousPositionValues.Length; i++)
                {
                    if (previousPositionValues[i] != mv.GetNeuronAt(0, i).LastCharge)
                    {
                        theNewPose.actuators.Add(mv.GetNeuronAt(0, i).Label);
                        if (!theNewPose.isRelative)
                            theNewPose.positions.Add(mv.GetNeuronAt(0, i).LastCharge);
                        else
                            theNewPose.positions.Add(mv.GetNeuronAt(0, i).LastCharge-previousPositionValues[i]);
                    }
                }
                poses.Add(theNewPose);
                MainWindow.Update();
            }
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            Init();
            previousPositionValues = null;
            mv.GetNeuronAt(0, 0).Label = "Save";
            mv.GetNeuronAt(0, 1).Label = "Move";
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
            for (int i = 0; i < poses.Count; i++)
            {
                mv.GetNeuronAt(i + 2).Label = poses[i].name;
            }
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
