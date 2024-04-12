//
// Copyright (c) Charles Simon. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

namespace BrainSimulator.Modules
{
    public class ModuleChain : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore]
        //public theStatus = 1;

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleChain()
        {
            minHeight = 1;
            maxHeight = 100;
            minWidth = 1;
            maxWidth = 100;
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
            Init();
            ClearNeurons();
            mv.GetNeuronAt(0, 0).Label = "In";

            for (int i = 0; i < mv.Height; i++)
            {
                for (int j = 0; j < mv.Width - 1; j++)
                {
                    mv.GetNeuronAt(j, i).AddSynapse(mv.GetNeuronAt(j + 1, i).id, 1);
                }
                if (i < mv.Height - 1)
                {
                    mv.GetNeuronAt(mv.Width - 1, i).AddSynapse(mv.GetNeuronAt(0, i + 1).id, 1);
                }
            }
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv != null)
            {
                AddSynapses();
            }
        }
    }
}
