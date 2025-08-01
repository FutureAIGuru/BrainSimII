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
    public class ModuleCorticalColumnInput : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleCorticalColumnInput()
        {
            minHeight = 1;
            maxHeight = 1;
            minWidth = 7;
            maxWidth = 7;
        }


        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            string newLabel = mv.GetNeuronAt(0, 0).Label;
            newLabel = newLabel.Replace("\"", "") + "*";
            if (mv.GetNeuronAt(3,0).Label != newLabel)
                mv.GetNeuronAt(3, 0).Label = newLabel;
            newLabel = newLabel.Replace("*", "") + "-out";
            if (mv.GetNeuronAt(4, 0).Label != newLabel)
                mv.GetNeuronAt(4, 0).Label = newLabel;
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
            Init();
            //ClearNeurons(false);

            mv.Color = Utils.ColorToInt(Colors.DarkGray);
            mv.Width = 7;

            //connections to the cortical columns are added by the columns  (change?)
            if (mv.GetNeuronAt(0, 0).Label == "")
                mv.GetNeuronAt(0, 0).Label = "\"Input\"";
            mv.GetNeuronAt(0, 0).Model = Neuron.modelType.Burst;
            mv.GetNeuronAt(0, 0).LeakRate= 4;
            mv.GetNeuronAt(0, 0).AxonDelay = 10;

            Neuron nFireOut = theNeuronArray.GetNeuron("Fire-Out");
            Neuron nNewCol = theNeuronArray.GetNeuron("New-Col");
            Neuron nGetCol = theNeuronArray.GetNeuron("Get-Col");
            if (nFireOut == null) return;
            if (nNewCol == null) return;
            if (nGetCol == null) return;

            mv.GetNeuronAt(0, 0).AddSynapse(mv.GetNeuronAt(1,0).id, 1);
            mv.GetNeuronAt(0, 0).AddSynapse(nGetCol.id, 1);

            mv.GetNeuronAt(1, 0).AddSynapse(mv.GetNeuronAt(2, 0).id, 1);
            mv.GetNeuronAt(1, 0).AddSynapse(nFireOut.id, 1);
            nNewCol.AddSynapse(mv.GetNeuronIndexAt(1, 0), 1, Synapse.modelType.Gate);
            mv.GetNeuronAt(1, 0).AddSynapse(mv.GetNeuronAt(4, 0).id, 1, Synapse.modelType.Learn);


            nFireOut.AddSynapse(mv.GetNeuronIndexAt(2, 0), 1, Synapse.modelType.Gate);

            mv.GetNeuronAt(3, 0).Label = "";
            mv.GetNeuronAt(3, 0).Model = Neuron.modelType.Burst;
            mv.GetNeuronAt(3, 0).LeakRate = 4;
            mv.GetNeuronAt(3, 0).AxonDelay = 8;
            mv.GetNeuronAt(3, 0).AddSynapse(nFireOut.id, 1);
            mv.GetNeuronAt(3, 0).AddSynapse(mv.GetNeuronIndexAt(2,0), 1);

            mv.GetNeuronAt(4, 0).Label = "";
            mv.GetNeuronAt(4, 0).Model = Neuron.modelType.LIF;
            mv.GetNeuronAt(4, 0).LeakRate = 0.3f;

            mv.GetNeuronAt(0, 0).AddSynapse(mv.GetNeuronAt(5, 0).id, 1, Synapse.modelType.Gate);
            mv.GetNeuronAt(3, 0).AddSynapse(mv.GetNeuronAt(6, 0).id, 1, Synapse.modelType.Gate);
            mv.GetNeuronAt(5, 0).AddSynapse(mv.GetNeuronAt(0, 0).id, -1);
            mv.GetNeuronAt(6, 0).AddSynapse(mv.GetNeuronAt(3, 0).id, -1);
            mv.GetNeuronAt(5, 0).AddSynapse(mv.GetNeuronAt(3, 0).id, 1);
            mv.GetNeuronAt(6, 0).AddSynapse(mv.GetNeuronAt(0, 0).id, 1);

            theNeuronArray.GetNeuron("Inverse").AddSynapse(mv.GetNeuronAt(5, 0).id, 1);
            theNeuronArray.GetNeuron("Inverse").AddSynapse(mv.GetNeuronAt(6, 0).id, 1);
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
            //AddSynapses();
        }
    }
}
