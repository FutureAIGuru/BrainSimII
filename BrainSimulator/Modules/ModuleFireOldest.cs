//
// Copyright (c) Charles Simon. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

namespace BrainSimulator.Modules
{
    public class ModuleFireOldest : ModuleBase
    {
        //any public variable you create here will automatically be stored with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore]
        //public theStatus = 1;

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            Neuron n = mv.GetNeuronAt(0);
            if (n.Fired())
            {
                long oldestTime = n.LastFired;
                int oldestNeuron = n.id;
                foreach (Neuron n1 in mv.Neurons)
                {
                    if (n1 == n) continue;
                    if (n1.lastFired < oldestTime)
                    {
                        oldestNeuron = mv.GetNeuronOffset(n1.id);
                        oldestTime = n1.lastFired;
                    }
                }
                mv.GetNeuronAt(oldestNeuron).SetValue(1);
            }

            //if you want the dlg to update, use the following code
            //because the thread you are in is not the UI thread
            //if (dlg != null)
            //     Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            minHeight = 1;
            minWidth = 1;
            AddLabel("Fire");
        }
    }
}
