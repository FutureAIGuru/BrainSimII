//
// Copyright (c) Charles Simon. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

namespace BrainSimulator.Modules
{
    public class ModuleMotor : ModuleBase
    {
        public ModuleMotor()
        {
            minHeight = 6;
            minWidth = 2;
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here
        }

        public override void Initialize()
        {
            Neuron nEnable = mv.GetNeuronAt(0);
            nEnable.Label = "Enable";
            Neuron nDisable = mv.GetNeuronAt(1);
            nDisable.Label = "Disable";
            nEnable.AddSynapse(nDisable.Id, -1);
            nDisable.AddSynapse(nDisable.Id, 1);
            //nDisable.CurrentCharge = 1;
            for (int i = 2; i < mv.NeuronCount; i++)
            {
                Neuron n = mv.GetNeuronAt(i);
                nDisable.AddSynapse(n.Id, -1);
            }

            Neuron nStop = mv.GetNeuronAt(2);
            nStop.Label = "Stop";
            Neuron nGo = mv.GetNeuronAt(3);
            nGo.Label = "Go";
            Neuron nLeft = mv.GetNeuronAt(4);
            nLeft.Label = "Left";
            Neuron nRight = mv.GetNeuronAt(5);
            nRight.Label = "Right";
            nStop.AddSynapse(nGo.Id, -1);
            nGo.AddSynapse(nGo.Id, 1);
            Neuron nFwd = GetNeuron("ModuleMove", "^");
            if (nFwd != null)
                nGo.AddSynapse(nFwd.Id, 1);
            Neuron nKBGo = GetNeuron("KBOut", "Go");
            if (nKBGo != null)
                nKBGo.AddSynapse(nGo.Id, 1);
            Neuron nKBStop = GetNeuron("KBOut", "Stop");
            if (nKBStop != null)
                nKBStop.AddSynapse(nStop.Id, 1);
            Neuron nL = GetNeuron("ModuleTurn", "<");
            Neuron nR = GetNeuron("ModuleTurn", ">");
            if (nL != null)
                nLeft.AddSynapse(nL.Id, 1);
            if (nR != null)
                nRight.AddSynapse(nR.Id, 1);
            Neuron nLTurn = GetNeuron("KBOut", "LTurn");
            if (nLTurn != null)
                nLTurn.AddSynapse(nLeft.Id, 1);
            Neuron nRTurn = GetNeuron("KBOut", "RTurn");
            if (nRTurn != null)
                nRTurn.AddSynapse(nRight.Id, 1);
            nStop.AddSynapse(nLeft.Id, -1);
            nStop.AddSynapse(nRight.Id, -1);
            nLeft.AddSynapse(nLeft.Id, 1);
            nRight.AddSynapse(nRight.Id, 1);

        }
    }
}
