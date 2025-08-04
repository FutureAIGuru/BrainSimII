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
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleCorticalColumn : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleCorticalColumn()
        {
            minHeight = 15;
            maxHeight = 20;
            minWidth = 1;
            maxWidth = 1;
        }


        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            // UpdateDialog();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            AddSynapses();
        }


        private void AddSynapses()
        {
            //rows with hard-coded functionality
            int inputRow = 6;
            int outputRow = 8;
            int thisCol = 9;
            int recur = 10;
            int isa = 11;
            int has_inst = 12;
            int has_a = 13;
            int part_of = 14;

            Init();
            //ClearNeurons(false);

            mv.Color = Utils.ColorToInt(Colors.DarkGray);
            int colNum = 0;
            foreach (var module in theNeuronArray.modules)
            {
                if (module.Label=="CorticalColumn" && module.FirstNeuron < mv.FirstNeuron)
                    colNum++;
            }

            mv.GetNeuronAt(0, 0).Label = "Col" + colNum;
            Neuron n = theNeuronArray.GetNeuron("Request");
            if (n != null)
                n.AddSynapse(mv.GetNeuronAt(0, 0).id, 1);

            mv.GetNeuronAt(0, 0).AddSynapse(mv.GetNeuronAt(0, 1).id, .2f, Synapse.modelType.Hebbian3);
            mv.GetNeuronAt(0, 0).AddSynapse(mv.GetNeuronAt(0, 2).id, 1);
            mv.GetNeuronAt(0, 1).Model = Neuron.modelType.LIF;
            mv.GetNeuronAt(0, 1).LeakRate = 0.3f;
            mv.GetNeuronAt(0, 1).AddSynapse(mv.GetNeuronAt(0, 2).id, -1);
            mv.GetNeuronAt(0, 1).AddSynapse(mv.GetNeuronAt(0, 3).id, -1);
            mv.GetNeuronAt(0, 2).AddSynapse(mv.GetNeuronAt(0, 3).id, 1);
            mv.GetNeuronAt(0, 3).AddSynapse(mv.GetNeuronAt(0, 4).id, 1);

            foreach (var module in theNeuronArray.modules)
            {
                if (module.ModuleTypeStr.Contains("CorticalColumn") && module.FirstNeuron > mv.FirstNeuron)
                {
                    mv.GetNeuronAt(0, 3).AddSynapse(module.FirstNeuron + 4, -1);
                }
            }

            mv.GetNeuronAt(0, 4).Label = "Act" + colNum;
            mv.GetNeuronAt(0, 4).Model = Neuron.modelType.Burst;
            mv.GetNeuronAt(0, 4).AxonDelay = 7;
            mv.GetNeuronAt(0, 4).LeakRate = 4;

            mv.GetNeuronAt(0, 4).AddSynapse(mv.GetNeuronAt(0, 0).id, 1);
            mv.GetNeuronAt(0, 4).AddSynapse(mv.GetNeuronAt(0, 1).id, 1);
            mv.GetNeuronAt(0, 4).AddSynapse(mv.GetNeuronAt(0, 6).id, 1, Synapse.modelType.Learn);
            mv.GetNeuronAt(0, 4).AddSynapse(mv.GetNeuronAt(0, 7).id, 1, Synapse.modelType.Learn);

            mv.GetNeuronAt(5).Label = "De" + colNum;
            mv.GetNeuronAt(5).Model = Neuron.modelType.Burst;
            mv.GetNeuronAt(5).AxonDelay = 5;
            mv.GetNeuronAt(5).LeakRate = 4;
            mv.GetNeuronAt(5).AddSynapse(mv.GetNeuronAt(1).id, -1, Synapse.modelType.Learn);
            mv.GetNeuronAt(5).AddSynapse(mv.GetNeuronAt(6).id, -1, Synapse.modelType.Learn);
            mv.GetNeuronAt(5).AddSynapse(mv.GetNeuronAt(7).id, -1, Synapse.modelType.Learn);
            mv.GetNeuronAt(5).AddSynapse(mv.GetNeuronAt(8).id, -1, Synapse.modelType.Learn);

            mv.GetNeuronAt(6).Label = "In" + colNum;
            mv.GetNeuronAt(6).Model = Neuron.modelType.LIF;
            mv.GetNeuronAt(6).LeakRate = .3f;
            n = theNeuronArray.GetNeuron("in-fired");
            if (n != null)
                mv.GetNeuronAt(6).AddSynapse(n.id, 1);
            for (int i = thisCol; i < part_of; i++)
                if (i != recur)
                    mv.GetNeuronAt(inputRow).AddSynapse(mv.GetNeuronAt(i).id, 1);
            mv.GetNeuronAt(7).AddSynapse(mv.GetNeuronAt(recur).id, 1);
            mv.GetNeuronAt(8).AddSynapse(mv.GetNeuronAt(recur).id, 1);

            mv.GetNeuronAt(7).Label = "In" + colNum+"*";
            mv.GetNeuronAt(7).Model = Neuron.modelType.LIF;
            mv.GetNeuronAt(7).LeakRate = .3f;
            mv.GetNeuronAt(7).AddSynapse(mv.GetNeuronAt(8).id, 1, Synapse.modelType.Learn);

            mv.GetNeuronAt(8).Label = "Out" + colNum;
            mv.GetNeuronAt(8).Model = Neuron.modelType.LIF;
            mv.GetNeuronAt(8).LeakRate = .3f;
            n = theNeuronArray.GetNeuron("out-fired");
            if (n != null)
                mv.GetNeuronAt(8).AddSynapse(n.id, 1);

            //find all neurons in theNeuronArray with labels starting with "
            string labelPrefix = "\""; // Replace with desired prefix

            for (int i = 0; i < theNeuronArray.arraySize; i++)
            {
                Neuron neuron = theNeuronArray.GetNeuron(i);
                if (neuron != null && neuron.Label != null && neuron.Label.StartsWith(labelPrefix))
                {
                    neuron.AddSynapse(mv.GetNeuronAt(6).id, .2f, Synapse.modelType.Hebbian3);
                    Neuron neuron2 = theNeuronArray.GetNeuron(i + 2*theNeuronArray.rows);
                    neuron2.AddSynapse(mv.GetNeuronAt(7).id, .2f, Synapse.modelType.Hebbian3);
                    Neuron neuron3 = theNeuronArray.GetNeuron(i + 4 * theNeuronArray.rows);
                    mv.GetNeuronAt(8).AddSynapse(neuron3.id, .2f, Synapse.modelType.Hebbian3);
                }
            }


            //this relationship...transfer input to output
            Neuron nThis = theNeuronArray.GetNeuron("this");
            if (nThis == null) return;
            nThis.AddSynapse(mv.GetNeuronAt(thisCol).id, 1, Synapse.modelType.Gate);
            mv.GetNeuronAt(thisCol).AddSynapse(mv.GetNeuronAt(8).id, 1);

            //recur relationship...transfer output to input
            Neuron nRecur = theNeuronArray.GetNeuron("recur");
            if (nRecur == null) return;
            nRecur.AddSynapse(mv.GetNeuronAt(recur).id, 1, Synapse.modelType.Gate);
            mv.GetNeuronAt(recur).AddSynapse(mv.GetNeuronAt(6).id, 1);

            //Isa Relationships
            bool flowControl = AddFixedRelationshipRow(outputRow, has_inst, "has-inst");
            if (!flowControl)
                return;
            flowControl = AddFixedRelationshipRow(outputRow, has_a, "has-a");
            if (!flowControl)
                return;
             flowControl = AddFixedRelationshipRow(outputRow, isa, "is-a");
            if (!flowControl)
                return;

            //handle part-of relationships
            Neuron nSource = theNeuronArray.GetNeuron("part-of");
            if (nSource is null) return;
            nSource.AddSynapse(mv.GetNeuronAt(part_of).id, 1, Synapse.modelType.Gate);
            mv.GetNeuronAt(part_of).AddSynapse(mv.GetNeuronAt(outputRow).id, 1);
                
            foreach (var module in theNeuronArray.modules)
            {
                if (module.Label== "CorticalColumn" && module.FirstNeuron != mv.FirstNeuron)
                {
                    Neuron nTest = module.GetNeuronAt(inputRow);
                    nTest.AddSynapse(mv.GetNeuronAt(part_of).id,.2f, Synapse.modelType.Hebbian2);
                }
            }
        }

        private bool AddFixedRelationshipRow(int thisOut, int row, string sourceLabel)
        {
            Neuron nSource = theNeuronArray.GetNeuron(sourceLabel);
            if (nSource is null) return false;
            nSource.AddSynapse(mv.GetNeuronAt(row).id, 1, Synapse.modelType.Gate);

            foreach (var module in theNeuronArray.modules)
            {
                if (module.Label =="CorticalColumn"  && module.FirstNeuron != mv.FirstNeuron)
                {
                    mv.GetNeuronAt(row).AddSynapse(module.GetNeuronAt(thisOut).id, .2f, Synapse.modelType.Hebbian3);
                }
            }

            return true;
        }



        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        public override MenuItem CustomContextMenuItems()
        {
            Button clearButton = new Button { Content = "Clear Neurons", };
            clearButton.Click += ClrButton_Click; 
            MenuItem mi = new MenuItem { Header = clearButton };
            return mi;
        }

        private void ClrButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (Neuron n in mv.Neurons)
            {
                foreach (Synapse s in n.synapses)
                    n.DeleteSynapse(s.targetNeuron);
            }
            MainWindow.arrayView.Update();
        }


        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return;
            AddSynapses();
        }
    }
}
